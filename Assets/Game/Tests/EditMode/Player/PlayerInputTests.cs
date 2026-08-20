using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UPnL.SignalRush.Player;
using UPnL.SignalRush.Run;
using UPnL.SignalRush.Tuning;
using SignalRushPlayerInput = UPnL.SignalRush.Player.PlayerInput;

namespace UPnL.SignalRush.Tests.Player
{
    public sealed class PlayerInputTests : InputTestFixture
    {
        [Test]
        public void AttackDuringRunningRequestsCombat()
        {
            var gameObject = new GameObject();
            var combat = gameObject.AddComponent<PlayerCombat>();
            var run = gameObject.AddComponent<RunController>();
            var input = gameObject.AddComponent<SignalRushPlayerInput>();
            Assign(input, "_combat", combat);
            Assign(input, "_runController", run);

            input.HandleAttack();

            Assert.That(combat.IsAttacking, Is.True);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Running));

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void AttackAfterFinishRestartsWithoutRequestingCombat()
        {
            var gameObject = new GameObject();
            var combat = gameObject.AddComponent<PlayerCombat>();
            var run = gameObject.AddComponent<RunController>();
            var input = gameObject.AddComponent<SignalRushPlayerInput>();
            Assign(input, "_combat", combat);
            Assign(input, "_runController", run);
            run.Tick(3f);
            run.ReportPlayerDead();
            run.ResolveFixedStep();

            input.HandleAttack();

            Assert.That(run.Phase, Is.EqualTo(RunPhase.Running));
            Assert.That(run.ElapsedSeconds, Is.Zero);
            Assert.That(combat.IsAttacking, Is.False);

            Object.DestroyImmediate(gameObject);
        }

        [UnityTest]
        public IEnumerator MovePerformedAndCanceledForwardTheirValues()
        {
            yield return new EnterPlayMode(false);
            var fixture = CreateMoveFixture();

            try
            {
                Assert.That(fixture.reference.action.enabled, Is.True);
                SetMove(fixture, 1f);
                yield return null;
                fixture.motor.Simulate(true, 0.02f);
                Assert.That(
                    fixture.body.linearVelocity.x,
                    Is.EqualTo(fixture.tuning.BaseRunSpeed + fixture.tuning.HorizontalCorrectionSpeed));

                SetMove(fixture, 0f);
                yield return null;
                fixture.motor.Simulate(true, 0.02f);
                Assert.That(fixture.body.linearVelocity.x, Is.EqualTo(fixture.tuning.BaseRunSpeed));
            }
            finally
            {
                Destroy(fixture);
            }

            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator DisableAndReenableDoesNotRetainReleasedMove()
        {
            yield return new EnterPlayMode(false);
            var fixture = CreateMoveFixture();

            try
            {
                SetMove(fixture, 1f);
                yield return null;
                fixture.motor.Simulate(true, 0.02f);
                Assert.That(
                    fixture.body.linearVelocity.x,
                    Is.EqualTo(fixture.tuning.BaseRunSpeed + fixture.tuning.HorizontalCorrectionSpeed));

                fixture.input.enabled = false;
                SetMove(fixture, 0f);
                yield return null;
                fixture.input.enabled = true;
                yield return null;
                fixture.motor.Simulate(true, 0.02f);

                Assert.That(fixture.body.linearVelocity.x, Is.EqualTo(fixture.tuning.BaseRunSpeed));
            }
            finally
            {
                Destroy(fixture);
            }

            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator MoveCanceledWhileLockedDoesNotReturnAfterUnlock()
        {
            yield return new EnterPlayMode(false);
            var fixture = CreateMoveFixture();

            try
            {
                SetMove(fixture, 1f);
                yield return null;
                fixture.status.RequestRespawn();
                SetMove(fixture, 0f);
                yield return null;
                fixture.status.Tick(fixture.tuning.RespawnLockSeconds);
                fixture.motor.Simulate(true, 0.02f);

                Assert.That(fixture.body.linearVelocity.x, Is.EqualTo(fixture.tuning.BaseRunSpeed));
            }
            finally
            {
                Destroy(fixture);
            }

            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator ReturnToEditMode()
        {
            if (Application.isPlaying)
            {
                yield return new ExitPlayMode();
            }
        }

        private static (
            GameObject gameObject,
            SignalRushPlayerInput input,
            PlayerMotor2D motor,
            Rigidbody2D body,
            PlayerStatus status,
            SignalRushTuning tuning,
            InputActionAsset asset,
            InputActionReference reference,
            Gamepad gamepad) CreateMoveFixture()
        {
            var gameObject = new GameObject("PlayerInputTests");
            gameObject.SetActive(false);
            var body = gameObject.AddComponent<Rigidbody2D>();
            var status = gameObject.AddComponent<PlayerStatus>();
            var motor = gameObject.AddComponent<PlayerMotor2D>();
            var input = gameObject.AddComponent<SignalRushPlayerInput>();
            var tuning = ScriptableObject.CreateInstance<SignalRushTuning>();
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var action = asset.AddActionMap("Gameplay").AddAction("Move", InputActionType.Value, "<Gamepad>/leftTrigger");
            var reference = InputActionReference.Create(action);
            var gamepad = InputSystem.AddDevice<Gamepad>();
            Assign(status, "_tuning", tuning);
            Assign(motor, "_body", body);
            Assign(motor, "_tuning", tuning);
            Assign(motor, "_status", status);
            Assign(input, "_move", reference);
            Assign(input, "_motor", motor);
            gameObject.SetActive(true);
            return (gameObject, input, motor, body, status, tuning, asset, reference, gamepad);
        }

        private void SetMove(
            (GameObject gameObject, SignalRushPlayerInput input, PlayerMotor2D motor, Rigidbody2D body,
                PlayerStatus status, SignalRushTuning tuning, InputActionAsset asset, InputActionReference reference,
                Gamepad gamepad) fixture,
            float value)
        {
            Set(fixture.gamepad.leftTrigger, value);
        }

        private static void Destroy(
            (GameObject gameObject, SignalRushPlayerInput input, PlayerMotor2D motor, Rigidbody2D body,
                PlayerStatus status, SignalRushTuning tuning, InputActionAsset asset, InputActionReference reference,
                Gamepad gamepad) fixture)
        {
            fixture.gameObject.SetActive(false);
            Object.DestroyImmediate(fixture.gameObject);
            Object.DestroyImmediate(fixture.tuning);
            Object.DestroyImmediate(fixture.reference);
            Object.DestroyImmediate(fixture.asset);
            InputSystem.RemoveDevice(fixture.gamepad);
        }

        private static void Assign(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
