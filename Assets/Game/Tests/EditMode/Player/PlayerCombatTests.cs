using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPnL.SignalRush.Combo;
using UPnL.SignalRush.Combat;
using UPnL.SignalRush.Player;
using UPnL.SignalRush.Tuning;

namespace UPnL.SignalRush.Tests.Player
{
    public sealed class PlayerCombatTests
    {
        [Test]
        public void AttackResolvesAllOverlappingTargetsAndRecordsEachSuccess()
        {
            var fixture = CreateCombat();
            var obstacle = CreateTarget<BreakableObstacle>("Obstacle", Vector2.left * 0.5f);
            var projectile = CreateTarget<Projectile>("Projectile", Vector2.right * 0.5f);
            var broken = 0;
            var parried = 0;
            fixture.combat.ObstacleBroken += target =>
            {
                Assert.That(target, Is.SameAs(obstacle.component));
                broken++;
            };
            fixture.combat.ProjectileParried += target =>
            {
                Assert.That(target, Is.SameAs(projectile.component));
                parried++;
            };

            fixture.combat.RequestAttack();

            Assert.That(fixture.combat.IsAttacking, Is.True);
            Assert.That(obstacle.component.IsBroken, Is.True);
            Assert.That(projectile.component.IsResolved, Is.True);
            Assert.That(fixture.combo.Current, Is.EqualTo(2));
            Assert.That(broken, Is.EqualTo(1));
            Assert.That(parried, Is.EqualTo(1));

            Destroy(fixture, obstacle.gameObject, projectile.gameObject);
        }

        [Test]
        public void AttackBuffersOnlyOneNextWindowAndResolvesItsCurrentOverlaps()
        {
            var fixture = CreateCombat();

            fixture.combat.RequestAttack();
            fixture.combat.RequestAttack();
            fixture.combat.RequestAttack();
            var nextWindowObstacle = CreateTarget<BreakableObstacle>("NextWindowObstacle", Vector2.zero);

            fixture.combat.Tick(fixture.tuning.AttackWindowSeconds);

            Assert.That(fixture.combat.IsAttacking, Is.True);
            Assert.That(nextWindowObstacle.component.IsBroken, Is.True);
            Assert.That(fixture.combo.Current, Is.EqualTo(1));

            fixture.combat.Tick(fixture.tuning.AttackWindowSeconds);

            Assert.That(fixture.combat.IsAttacking, Is.False);

            Destroy(fixture, nextWindowObstacle.gameObject);
        }

        [TestCase(PlayerState.Hit)]
        [TestCase(PlayerState.Respawning)]
        [TestCase(PlayerState.Dead)]
        public void StatusInterruptionClosesAttackAndDiscardsBufferedWindow(PlayerState state)
        {
            var fixture = CreateCombat();

            fixture.combat.RequestAttack();
            fixture.combat.RequestAttack();
            EnterState(fixture.status, state);

            Assert.That(fixture.combat.IsAttacking, Is.False);

            fixture.combat.Tick(fixture.tuning.AttackWindowSeconds);

            Assert.That(fixture.combat.IsAttacking, Is.False);

            Destroy(fixture);
        }

        [Test]
        public void MissingSerializedDependenciesDoNotThrow()
        {
            var gameObject = new GameObject("PlayerCombatWithoutDependencies");
            var combat = gameObject.AddComponent<PlayerCombat>();

            Assert.DoesNotThrow(() =>
            {
                combat.RequestAttack();
                combat.Tick(0.1f);
                combat.Interrupt();
            });

            Object.DestroyImmediate(gameObject);
        }

        private static (PlayerCombat combat, PlayerStatus status, ComboCounter combo, SignalRushTuning tuning, GameObject gameObject) CreateCombat()
        {
            var gameObject = new GameObject("PlayerCombatTests");
            var hitboxObject = new GameObject("AttackHitbox");
            hitboxObject.transform.SetParent(gameObject.transform);
            var hitbox = hitboxObject.AddComponent<BoxCollider2D>();
            hitbox.isTrigger = true;
            hitbox.size = Vector2.one * 2f;

            var combat = gameObject.AddComponent<PlayerCombat>();
            var status = gameObject.AddComponent<PlayerStatus>();
            var combo = gameObject.AddComponent<ComboCounter>();
            var tuning = ScriptableObject.CreateInstance<SignalRushTuning>();
            SetReference(combat, "_attackHitbox", hitbox);
            SetReference(combat, "_tuning", tuning);
            SetReference(combat, "_combo", combo);
            SetReference(combat, "_status", status);
            SetReference(status, "_tuning", tuning);
            SetReference(combo, "_tuning", tuning);
            Physics2D.SyncTransforms();

            return (combat, status, combo, tuning, gameObject);
        }

        private static (T component, GameObject gameObject) CreateTarget<T>(string name, Vector2 position) where T : Component
        {
            var gameObject = new GameObject(name);
            gameObject.transform.position = position;
            gameObject.AddComponent<BoxCollider2D>();
            var component = gameObject.AddComponent<T>();
            Physics2D.SyncTransforms();
            return (component, gameObject);
        }

        private static void EnterState(PlayerStatus status, PlayerState state)
        {
            switch (state)
            {
                case PlayerState.Hit:
                    status.RequestDamage(DamageCause.Projectile);
                    break;
                case PlayerState.Respawning:
                    status.RequestRespawn();
                    break;
                case PlayerState.Dead:
                    status.MarkDead();
                    break;
            }
        }

        private static void SetReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Destroy((PlayerCombat combat, PlayerStatus status, ComboCounter combo, SignalRushTuning tuning, GameObject gameObject) fixture, params GameObject[] targets)
        {
            foreach (var target in targets)
            {
                Object.DestroyImmediate(target);
            }

            Object.DestroyImmediate(fixture.gameObject);
            Object.DestroyImmediate(fixture.tuning);
        }
    }
}
