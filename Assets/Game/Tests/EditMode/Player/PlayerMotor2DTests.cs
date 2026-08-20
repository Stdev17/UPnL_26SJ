using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPnL.SignalRush.Player;
using UPnL.SignalRush.Tuning;

namespace UPnL.SignalRush.Tests.Player
{
    public sealed class PlayerMotor2DTests
    {
        [Test]
        public void GroundedJumpSetsVerticalVelocityOnce()
        {
            var fixture = CreateMotor();
            var groundedChanges = 0;
            fixture.motor.GroundedChanged += _ => groundedChanges++;

            fixture.motor.Simulate(true, 0.02f);
            fixture.motor.RequestJump();
            fixture.motor.Simulate(true, 0.02f);
            fixture.motor.RequestJump();

            Assert.That(fixture.motor.IsGrounded, Is.False);
            Assert.That(fixture.body.linearVelocity.y, Is.EqualTo(fixture.tuning.JumpVelocity));
            Assert.That(groundedChanges, Is.EqualTo(2));

            Destroy(fixture);
        }

        [Test]
        public void LockedControlIgnoresCorrectionAndJumpRequests()
        {
            var fixture = CreateMotor();
            fixture.motor.Simulate(true, 0.02f);
            fixture.status.RequestRespawn();

            fixture.motor.SetMoveInput(1f);
            fixture.motor.RequestJump();
            fixture.status.Tick(fixture.tuning.RespawnLockSeconds);
            fixture.motor.Simulate(true, 0.02f);

            Assert.That(fixture.body.linearVelocity.x, Is.EqualTo(fixture.tuning.BaseRunSpeed));
            Assert.That(fixture.body.linearVelocity.y, Is.Zero);

            Destroy(fixture);
        }

        [Test]
        public void MovementClampsCorrectionAndSpeedMultiplierToTheirMinimums()
        {
            var fixture = CreateMotor();

            fixture.motor.SetSpeedMultiplier(0f);
            fixture.motor.SetMoveInput(2f);
            fixture.motor.Simulate(true, 0.02f);

            Assert.That(fixture.body.linearVelocity.x, Is.EqualTo(fixture.tuning.BaseRunSpeed + fixture.tuning.HorizontalCorrectionSpeed));

            Destroy(fixture);
        }

        [Test]
        public void RespawnMovesBodyAndClearsVelocity()
        {
            var fixture = CreateMotor();
            fixture.body.linearVelocity = new Vector2(4f, -3f);

            fixture.motor.Respawn(new Vector2(7f, 2f));

            Assert.That(fixture.motor.Position, Is.EqualTo(new Vector2(7f, 2f)));
            Assert.That(fixture.body.linearVelocity, Is.EqualTo(Vector2.zero));

            Destroy(fixture);
        }

        [Test]
        public void FallingAddsTheConfiguredExtraGravity()
        {
            var fixture = CreateMotor();
            var previousGravity = Physics2D.gravity;
            Physics2D.gravity = new Vector2(0f, -10f);
            fixture.body.linearVelocity = new Vector2(0f, -1f);

            try
            {
                fixture.motor.Simulate(false, 0.1f);

                Assert.That(fixture.body.linearVelocity.y, Is.EqualTo(-2f));
            }
            finally
            {
                Physics2D.gravity = previousGravity;
                Destroy(fixture);
            }
        }

        private static (PlayerMotor2D motor, Rigidbody2D body, PlayerStatus status, SignalRushTuning tuning, GameObject gameObject) CreateMotor()
        {
            var gameObject = new GameObject("PlayerMotor2DTests");
            var body = gameObject.AddComponent<Rigidbody2D>();
            var status = gameObject.AddComponent<PlayerStatus>();
            var motor = gameObject.AddComponent<PlayerMotor2D>();
            var tuning = ScriptableObject.CreateInstance<SignalRushTuning>();
            var serializedStatus = new SerializedObject(status);
            serializedStatus.FindProperty("_tuning").objectReferenceValue = tuning;
            serializedStatus.ApplyModifiedPropertiesWithoutUndo();
            var serializedMotor = new SerializedObject(motor);
            serializedMotor.FindProperty("_body").objectReferenceValue = body;
            serializedMotor.FindProperty("_tuning").objectReferenceValue = tuning;
            serializedMotor.FindProperty("_status").objectReferenceValue = status;
            serializedMotor.ApplyModifiedPropertiesWithoutUndo();
            return (motor, body, status, tuning, gameObject);
        }

        private static void Destroy((PlayerMotor2D motor, Rigidbody2D body, PlayerStatus status, SignalRushTuning tuning, GameObject gameObject) fixture)
        {
            Object.DestroyImmediate(fixture.gameObject);
            Object.DestroyImmediate(fixture.tuning);
        }
    }
}
