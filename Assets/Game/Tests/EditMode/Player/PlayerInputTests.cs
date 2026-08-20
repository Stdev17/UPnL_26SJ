using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPnL.SignalRush.Player;
using UPnL.SignalRush.Run;

namespace UPnL.SignalRush.Tests.Player
{
    public sealed class PlayerInputTests
    {
        [Test]
        public void AttackDuringRunningRequestsCombat()
        {
            var gameObject = new GameObject();
            var combat = gameObject.AddComponent<PlayerCombat>();
            var run = gameObject.AddComponent<RunController>();
            var input = gameObject.AddComponent<PlayerInput>();
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
            var input = gameObject.AddComponent<PlayerInput>();
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

        private static void Assign(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
