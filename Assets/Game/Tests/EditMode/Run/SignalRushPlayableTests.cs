using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPnL.SignalRush.Combat;
using UPnL.SignalRush.Combo;
using UPnL.SignalRush.Player;
using UPnL.SignalRush.Run;
using UPnL.SignalRush.Tuning;
using UPnL.SignalRush.World;

namespace UPnL.SignalRush.Tests.Run
{
    public sealed class SignalRushPlayableTests
    {
        [Test]
        public void GoalEventReportsGoalToRunController()
        {
            var fixture = CreatePlayable();

            fixture.goal.TryReach();
            fixture.run.ResolveFixedStep();

            Assert.That(fixture.run.Result, Is.EqualTo(RunResult.GoalReached));
            Destroy(fixture);
        }

        [Test]
        public void ComboChangesDriveMotorSpeedMultiplier()
        {
            var fixture = CreatePlayable();

            fixture.combo.RecordBreak();
            fixture.motor.Simulate(true, 0.02f);

            Assert.That(
                fixture.body.linearVelocity.x,
                Is.EqualTo(fixture.tuning.BaseRunSpeed * fixture.combo.SpeedMultiplier).Within(0.0001f));
            Destroy(fixture);
        }

        [Test]
        public void RespawnStateMovesToSafePositionAndPausesRunUntilActive()
        {
            var fixture = CreatePlayable();
            fixture.body.position = new Vector2(8f, 2f);
            fixture.motor.Simulate(true, 0.02f);
            fixture.body.position = new Vector2(20f, -10f);

            fixture.status.RequestRespawn();

            Assert.That(fixture.run.Phase, Is.EqualTo(RunPhase.Respawning));
            Assert.That(fixture.motor.Position, Is.EqualTo(new Vector2(8f, 2f)));

            fixture.status.Tick(fixture.tuning.RespawnLockSeconds);

            Assert.That(fixture.run.Phase, Is.EqualTo(RunPhase.Running));
            Destroy(fixture);
        }

        [Test]
        public void ContactSeamsRequestTypedDamageAndResolveProjectileOnlyOnce()
        {
            var fixture = CreatePlayable();
            var causes = new List<DamageCause>();
            fixture.status.Hit += causes.Add;
            var projectileObject = new GameObject("Projectile");
            var projectile = projectileObject.AddComponent<Projectile>();

            fixture.bridge.HandleProjectile(projectile);
            fixture.bridge.HandleProjectile(projectile);
            fixture.status.ResetStatus();
            var obstacleObject = new GameObject("Obstacle");
            var obstacle = obstacleObject.AddComponent<BreakableObstacle>();
            fixture.bridge.HandleObstacle(obstacle);

            Assert.That(causes, Is.EqualTo(new[] { DamageCause.Projectile, DamageCause.Obstacle }));
            Object.DestroyImmediate(projectileObject);
            Object.DestroyImmediate(obstacleObject);
            Destroy(fixture);
        }

        [Test]
        public void ProjectileTriggerRoutesThroughPublicContactSeam()
        {
            var fixture = CreatePlayable();
            DamageCause? cause = null;
            fixture.status.Hit += value => cause = value;
            var projectileObject = new GameObject("Projectile");
            var collider = projectileObject.AddComponent<BoxCollider2D>();
            projectileObject.AddComponent<Projectile>();

            Invoke(fixture.bridge, "OnTriggerEnter2D", collider);

            Assert.That(cause, Is.EqualTo(DamageCause.Projectile));
            Object.DestroyImmediate(projectileObject);
            Destroy(fixture);
        }

        [Test]
        public void BrokenObstacleIsDeactivatedByCombatEvent()
        {
            var fixture = CreatePlayable();
            var obstacleObject = new GameObject("Obstacle");
            obstacleObject.transform.position = fixture.player.position;
            obstacleObject.AddComponent<BoxCollider2D>();
            obstacleObject.AddComponent<BreakableObstacle>();
            Physics2D.SyncTransforms();

            fixture.combat.RequestAttack();

            Assert.That(obstacleObject.activeSelf, Is.False);
            Object.DestroyImmediate(obstacleObject);
            Destroy(fixture);
        }

        [Test]
        public void FallingBelowThresholdRequestsRespawn()
        {
            var fixture = CreatePlayable();
            fixture.player.position = new Vector2(0f, -6f);

            Invoke(fixture.bridge, "Update");

            Assert.That(fixture.status.State, Is.EqualTo(PlayerState.Respawning));
            Assert.That(fixture.run.Phase, Is.EqualTo(RunPhase.Respawning));
            Destroy(fixture);
        }

        [Test]
        public void FinishedToRunningRestartResetsPlayerComboAndSpawner()
        {
            var fixture = CreatePlayable(true);
            fixture.combo.RecordBreak();
            fixture.combo.RecordParry();
            fixture.status.MarkDead();
            fixture.run.ResolveFixedStep();
            fixture.body.position = new Vector2(30f, -4f);

            Assert.That(fixture.run.Phase, Is.EqualTo(RunPhase.Finished));
            Assert.That(fixture.spawner.SpawnNext(), Is.Null);

            fixture.run.Restart();

            Assert.That(fixture.status.State, Is.EqualTo(PlayerState.Active));
            Assert.That(fixture.combo.Current, Is.Zero);
            Assert.That(fixture.combo.Best, Is.Zero);
            Assert.That(fixture.combo.Interrupted, Is.Zero);
            Assert.That(fixture.motor.Position, Is.EqualTo(new Vector2(3f, 4f)));
            Assert.That(fixture.spawner.SpawnNext(), Is.Not.Null);
            Destroy(fixture);
        }

        [Test]
        public void RespawnToRunningDoesNotResetRunCombo()
        {
            var fixture = CreatePlayable();
            fixture.combo.RecordBreak();
            fixture.status.RequestRespawn();

            fixture.status.Tick(fixture.tuning.RespawnLockSeconds);

            Assert.That(fixture.combo.Current, Is.EqualTo(1));
            Assert.That(fixture.combo.Best, Is.EqualTo(1));
            Destroy(fixture);
        }

        [Test]
        public void DisableUnsubscribesEventsAndStopsSpawner()
        {
            var fixture = CreatePlayable(true);

            Invoke(fixture.bridge, "OnDisable");
            fixture.goal.TryReach();
            fixture.run.ResolveFixedStep();
            fixture.combo.RecordBreak();
            fixture.motor.Simulate(true, 0.02f);

            Assert.That(fixture.run.Result, Is.Null);
            Assert.That(fixture.body.linearVelocity.x, Is.EqualTo(fixture.tuning.BaseRunSpeed));
            Assert.That(fixture.spawner.SpawnNext(), Is.Null);
            Destroy(fixture);
        }

        private static PlayableFixture CreatePlayable(bool withSpawner = false)
        {
            var root = new GameObject("SignalRushPlayableTests");
            root.SetActive(false);
            root.transform.position = new Vector2(3f, 4f);
            var body = root.AddComponent<Rigidbody2D>();
            var run = root.AddComponent<RunController>();
            var goal = root.AddComponent<GoalTrigger>();
            var status = root.AddComponent<PlayerStatus>();
            var motor = root.AddComponent<PlayerMotor2D>();
            var combo = root.AddComponent<ComboCounter>();
            var combat = root.AddComponent<PlayerCombat>();
            var hitboxObject = new GameObject("AttackHitbox");
            hitboxObject.transform.SetParent(root.transform, false);
            var hitbox = hitboxObject.AddComponent<BoxCollider2D>();
            hitbox.isTrigger = true;
            hitbox.size = Vector2.one * 2f;
            var tuning = ScriptableObject.CreateInstance<SignalRushTuning>();
            var spawner = withSpawner ? CreateSpawner(root.transform, root.transform, tuning) : null;
            var bridge = root.AddComponent<SignalRushPlayable>();

            SetReference(run, "_goalTrigger", goal);
            SetReference(status, "_tuning", tuning);
            SetReference(combo, "_tuning", tuning);
            SetReference(motor, "_body", body);
            SetReference(motor, "_tuning", tuning);
            SetReference(motor, "_status", status);
            SetReference(combat, "_attackHitbox", hitbox);
            SetReference(combat, "_tuning", tuning);
            SetReference(combat, "_combo", combo);
            SetReference(combat, "_status", status);
            SetReference(bridge, "_runController", run);
            SetReference(bridge, "_goalTrigger", goal);
            SetReference(bridge, "_playerStatus", status);
            SetReference(bridge, "_playerMotor", motor);
            SetReference(bridge, "_playerCombat", combat);
            SetReference(bridge, "_comboCounter", combo);
            SetReference(bridge, "_chunkSpawner", spawner);
            SetReference(bridge, "_player", root.transform);
            SetFloat(bridge, "_fallY", -5f);
            root.SetActive(true);
            Invoke(bridge, "OnEnable");
            Physics2D.SyncTransforms();

            return new PlayableFixture(root, body, run, goal, status, motor, combo, combat, spawner, bridge, tuning);
        }

        private static ChunkSpawner CreateSpawner(Transform parent, Transform player, SignalRushTuning tuning)
        {
            var spawnerObject = new GameObject("ChunkSpawner");
            spawnerObject.transform.SetParent(parent, false);
            var spawner = spawnerObject.AddComponent<ChunkSpawner>();
            var origin = new GameObject("Origin").transform;
            origin.SetParent(spawnerObject.transform, false);
            var gameplay = CreateChunk("GameplayPrefab", parent);
            var decor = CreateChunk("DecorPrefab", parent);
            var sniper = CreateChunk("SniperPrefab", parent);
            var serialized = new SerializedObject(spawner);
            serialized.FindProperty("_tuning").objectReferenceValue = tuning;
            serialized.FindProperty("_origin").objectReferenceValue = origin;
            serialized.FindProperty("_player").objectReferenceValue = player;
            SetArray(serialized.FindProperty("_gameplayFrontPrefabs"), gameplay);
            SetArray(serialized.FindProperty("_decorFrontPrefabs"), decor);
            SetArray(serialized.FindProperty("_sniperRearPrefabs"), sniper);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return spawner;
        }

        private static Chunk CreateChunk(string name, Transform parent)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            return gameObject.AddComponent<Chunk>();
        }

        private static void SetReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(SerializedProperty property, Chunk chunk)
        {
            property.arraySize = 1;
            property.GetArrayElementAtIndex(0).objectReferenceValue = chunk;
        }

        private static void Invoke(Object target, string methodName, params object[] arguments)
        {
            target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);
        }

        private static void Destroy(PlayableFixture fixture)
        {
            Object.DestroyImmediate(fixture.root);
            Object.DestroyImmediate(fixture.tuning);
        }

        private readonly struct PlayableFixture
        {
            public readonly GameObject root;
            public readonly Transform player;
            public readonly Rigidbody2D body;
            public readonly RunController run;
            public readonly GoalTrigger goal;
            public readonly PlayerStatus status;
            public readonly PlayerMotor2D motor;
            public readonly ComboCounter combo;
            public readonly PlayerCombat combat;
            public readonly ChunkSpawner spawner;
            public readonly SignalRushPlayable bridge;
            public readonly SignalRushTuning tuning;

            public PlayableFixture(
                GameObject root,
                Rigidbody2D body,
                RunController run,
                GoalTrigger goal,
                PlayerStatus status,
                PlayerMotor2D motor,
                ComboCounter combo,
                PlayerCombat combat,
                ChunkSpawner spawner,
                SignalRushPlayable bridge,
                SignalRushTuning tuning)
            {
                this.root = root;
                player = root.transform;
                this.body = body;
                this.run = run;
                this.goal = goal;
                this.status = status;
                this.motor = motor;
                this.combo = combo;
                this.combat = combat;
                this.spawner = spawner;
                this.bridge = bridge;
                this.tuning = tuning;
            }
        }
    }
}
