using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UPnL.SignalRush.Combat;
using UPnL.SignalRush.Tuning;
using UPnL.SignalRush.World;

namespace UPnL.SignalRush.Tests.World
{
    public sealed class WorldRulesTests
    {
        [Test]
        public void PlaceAppliesSlotRoleAndPosition()
        {
            var gameObject = new GameObject("Chunk");
            var chunk = gameObject.AddComponent<Chunk>();

            chunk.Place(new ChunkSlot(ChunkRole.DecorFront, new Vector2(3f, -2f)));

            Assert.That(chunk.Role, Is.EqualTo(ChunkRole.DecorFront));
            Assert.That((Vector2)chunk.transform.position, Is.EqualTo(new Vector2(3f, -2f)));
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void SniperFiresOnceTowardLatestPlayerPosition()
        {
            var fixture = CreateSniper();
            Projectile spawned = null;
            fixture.sniper.ProjectileSpawned += projectile => spawned = projectile;

            Assert.That(fixture.sniper.TryActivate(), Is.True);
            Assert.That(fixture.sniper.TryActivate(), Is.False);
            fixture.player.position = new Vector2(0f, 4f);
            fixture.sniper.Tick(fixture.tuning.SniperWarningSeconds);

            Assert.That(fixture.sniper.IsTargetting, Is.False);
            Assert.That(spawned, Is.Not.Null);
            Assert.That(spawned.GetComponent<Rigidbody2D>().linearVelocity, Is.EqualTo(Vector2.up * 18f));
            Assert.That(fixture.sniper.TryActivate(), Is.False);

            spawned.TryParry();
            Assert.That(fixture.sniper.TryActivate(), Is.True);
            Destroy(fixture, spawned);
        }

        [Test]
        public void ChunkCannotDespawnDuringWarningOrUnresolvedProjectile()
        {
            var fixture = CreateSniper();
            var chunkObject = new GameObject("Chunk");
            var chunk = chunkObject.AddComponent<Chunk>();
            SetObjectReference(chunk, "_sniper", fixture.sniper);
            Projectile spawned = null;
            fixture.sniper.ProjectileSpawned += projectile => spawned = projectile;

            Assert.That(chunk.CanDespawn, Is.True);
            fixture.sniper.TryActivate();
            Assert.That(chunk.CanDespawn, Is.False);
            fixture.sniper.Tick(fixture.tuning.SniperWarningSeconds);
            Assert.That(chunk.CanDespawn, Is.False);
            spawned.TryHitPlayer();
            Assert.That(chunk.CanDespawn, Is.True);

            Object.DestroyImmediate(chunkObject);
            Destroy(fixture, spawned);
        }

        [Test]
        public void MissedProjectileImmediatelyClearsSniperOwnership()
        {
            var fixture = CreateSniper();
            Projectile spawned = null;
            fixture.sniper.ProjectileSpawned += projectile => spawned = projectile;
            fixture.sniper.TryActivate();
            fixture.sniper.Tick(fixture.tuning.SniperWarningSeconds);

            spawned.TryResolveMissed();

            var activeProjectile = typeof(Sniper).GetField("_activeProjectile", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(activeProjectile.GetValue(fixture.sniper), Is.Null);
            Destroy(fixture, spawned);
        }

        [Test]
        public void DestroyingSniperDestroysItsUnresolvedOwnedProjectile()
        {
            var fixture = CreateSniper();
            Projectile spawned = null;
            fixture.sniper.ProjectileSpawned += projectile => spawned = projectile;
            fixture.sniper.TryActivate();
            fixture.sniper.Tick(fixture.tuning.SniperWarningSeconds);

            typeof(Sniper).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(fixture.sniper, null);

            var destroyed = spawned == null;
            if (!destroyed)
                Object.DestroyImmediate(spawned.gameObject);
            Object.DestroyImmediate(fixture.root);
            Object.DestroyImmediate(fixture.tuning);
            Assert.That(destroyed, Is.True);
        }

        [Test]
        public void BeginRejectsMissingRolePrefabsAndLeavesSpawnerStopped()
        {
            var spawnerObject = new GameObject("Spawner");
            var spawner = spawnerObject.AddComponent<ChunkSpawner>();
            LogAssert.Expect(LogType.Error, "ChunkSpawner requires tuning, origin, player, and non-empty role prefab lists.");

            spawner.Begin();

            Assert.That(spawner.SpawnNext(), Is.Null);
            LogAssert.NoUnexpectedReceived();
            Object.DestroyImmediate(spawnerObject);
        }

        [Test]
        public void SpawnNextRoundRobinsRolesAndBoundsGameplayPlacementByReachability()
        {
            var previousGravity = Physics2D.gravity;
            Physics2D.gravity = new Vector2(0f, -10f);
            var fixture = CreateSpawner();

            try
            {
                fixture.spawner.Begin();
                var gameplayOne = fixture.spawner.SpawnNext();
                var decor = fixture.spawner.SpawnNext();
                var sniper = fixture.spawner.SpawnNext();
                var gameplayTwo = fixture.spawner.SpawnNext();

                Assert.That(gameplayOne.Role, Is.EqualTo(ChunkRole.GameplayFront));
                Assert.That(decor.Role, Is.EqualTo(ChunkRole.DecorFront));
                Assert.That(sniper.Role, Is.EqualTo(ChunkRole.SniperRear));
                Assert.That(gameplayTwo.Role, Is.EqualTo(ChunkRole.GameplayFront));
                Assert.That(gameplayOne.transform.position.x, Is.EqualTo(11.024264f).Within(0.0001f));
                Assert.That(gameplayOne.transform.position.y, Is.EqualTo(2f).Within(0.0001f));
                Assert.That(gameplayTwo.name, Does.StartWith("GameplayB"));
            }
            finally
            {
                Physics2D.gravity = previousGravity;
                Destroy(fixture);
            }
        }

        [Test]
        public void GameplayHeightDoesNotDriftAwayFromOrigin()
        {
            var fixture = CreateSpawner();
            var tuning = new SerializedObject(fixture.tuning);
            tuning.FindProperty("_jumpVelocity").floatValue = 8f;
            tuning.ApplyModifiedPropertiesWithoutUndo();
            fixture.spawner.Begin();

            Chunk thirdGameplay = null;
            for (var i = 0; i < 7; i++)
            {
                var spawned = fixture.spawner.SpawnNext();
                if (spawned.Role == ChunkRole.GameplayFront)
                    thirdGameplay = spawned;
            }

            Assert.That(thirdGameplay.transform.position.y, Is.LessThanOrEqualTo(3.5f));
            Destroy(fixture);
        }

        [Test]
        public void SpawningSniperRearInjectsScenePlayerBeforeActivation()
        {
            var fixture = CreateSpawner();
            fixture.spawner.Begin();
            fixture.spawner.SpawnNext();
            fixture.spawner.SpawnNext();

            var rear = fixture.spawner.SpawnNext();

            Assert.That(rear.GetComponent<Sniper>().IsTargetting, Is.True);
            Destroy(fixture);
        }

        [Test]
        public void TickCleansBehindChunksOnlyAfterTheirSniperLifetimeEnds()
        {
            var fixture = CreateSpawner();
            fixture.spawner.Begin();
            var gameplay = fixture.spawner.SpawnNext();
            var decor = fixture.spawner.SpawnNext();
            var rear = fixture.spawner.SpawnNext();
            var sniper = rear.GetComponent<Sniper>();
            Projectile projectile = null;
            sniper.ProjectileSpawned += spawned => projectile = spawned;
            fixture.player.position = new Vector2(100f, 0f);

            fixture.spawner.Tick();

            Assert.That(gameplay == null, Is.True);
            Assert.That(decor == null, Is.True);
            Assert.That(rear == null, Is.False);

            sniper.Tick(fixture.tuning.SniperWarningSeconds);
            projectile.TryResolveMissed();
            fixture.spawner.Tick();

            Assert.That(rear == null, Is.True);
            if (projectile != null)
                Object.DestroyImmediate(projectile.gameObject);
            Destroy(fixture);
        }

        [Test]
        public void StopDestroysTrackedChunks()
        {
            var fixture = CreateSpawner();
            fixture.spawner.Begin();
            var spawned = fixture.spawner.SpawnNext();

            fixture.spawner.Stop();

            Assert.That(spawned == null, Is.True);
            Destroy(fixture);
        }

        [Test]
        public void BeginDestroysPriorChunksAndResetsRoundRobin()
        {
            var fixture = CreateSpawner();
            fixture.spawner.Begin();
            var prior = fixture.spawner.SpawnNext();
            fixture.spawner.SpawnNext();

            fixture.spawner.Begin();
            var next = fixture.spawner.SpawnNext();

            Assert.That(prior == null, Is.True);
            Assert.That(next.Role, Is.EqualTo(ChunkRole.GameplayFront));
            Destroy(fixture);
        }

        private static (Sniper sniper, Transform player, SignalRushTuning tuning, Projectile projectilePrefab, GameObject root) CreateSniper()
        {
            var root = new GameObject("SniperFixture");
            var sniper = root.AddComponent<Sniper>();
            var player = new GameObject("Player").transform;
            player.SetParent(root.transform);
            player.position = new Vector2(3f, 0f);
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(root.transform);
            var projectileObject = new GameObject("ProjectilePrefab");
            projectileObject.transform.SetParent(root.transform);
            projectileObject.AddComponent<Rigidbody2D>();
            var projectilePrefab = projectileObject.AddComponent<Projectile>();
            var tuning = ScriptableObject.CreateInstance<SignalRushTuning>();
            SetObjectReference(sniper, "_tuning", tuning);
            SetObjectReference(sniper, "_playerTarget", player);
            SetObjectReference(sniper, "_muzzle", muzzle);
            SetObjectReference(sniper, "_projectilePrefab", projectilePrefab);
            return (sniper, player, tuning, projectilePrefab, root);
        }

        private static (ChunkSpawner spawner, SignalRushTuning tuning, Transform player, GameObject root, GameObject[] prefabs) CreateSpawner()
        {
            var root = new GameObject("SpawnerFixture");
            var spawner = root.AddComponent<ChunkSpawner>();
            var origin = new GameObject("Origin").transform;
            origin.SetParent(root.transform);
            origin.position = new Vector2(10f, 2f);
            var player = new GameObject("Player").transform;
            player.SetParent(root.transform);
            var tuning = ScriptableObject.CreateInstance<SignalRushTuning>();
            var tuningData = new SerializedObject(tuning);
            tuningData.FindProperty("_jumpVelocity").floatValue = 1f;
            tuningData.ApplyModifiedPropertiesWithoutUndo();
            var prefabs = new[]
            {
                CreateChunkPrefab("GameplayA", root.transform),
                CreateChunkPrefab("GameplayB", root.transform),
                CreateChunkPrefab("Decor", root.transform),
                CreateChunkPrefab("Sniper", root.transform)
            };
            ConfigureSniperChunk(prefabs[3], tuning, root.transform);
            var serialized = new SerializedObject(spawner);
            serialized.FindProperty("_tuning").objectReferenceValue = tuning;
            serialized.FindProperty("_origin").objectReferenceValue = origin;
            serialized.FindProperty("_player").objectReferenceValue = player;
            SetArray(serialized.FindProperty("_gameplayFrontPrefabs"), prefabs[0].GetComponent<Chunk>(), prefabs[1].GetComponent<Chunk>());
            SetArray(serialized.FindProperty("_decorFrontPrefabs"), prefabs[2].GetComponent<Chunk>());
            SetArray(serialized.FindProperty("_sniperRearPrefabs"), prefabs[3].GetComponent<Chunk>());
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return (spawner, tuning, player, root, prefabs);
        }

        private static void ConfigureSniperChunk(GameObject chunkObject, SignalRushTuning tuning, Transform fixtureRoot)
        {
            var sniper = chunkObject.AddComponent<Sniper>();
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(chunkObject.transform);
            var projectileObject = new GameObject("ProjectilePrefab");
            projectileObject.transform.SetParent(fixtureRoot);
            projectileObject.AddComponent<Rigidbody2D>();
            var projectile = projectileObject.AddComponent<Projectile>();
            SetObjectReference(sniper, "_tuning", tuning);
            SetObjectReference(sniper, "_muzzle", muzzle);
            SetObjectReference(sniper, "_projectilePrefab", projectile);
            SetObjectReference(chunkObject.GetComponent<Chunk>(), "_sniper", sniper);
        }

        private static GameObject CreateChunkPrefab(string name, Transform parent)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            gameObject.AddComponent<Chunk>();
            return gameObject;
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(SerializedProperty property, params Chunk[] chunks)
        {
            property.arraySize = chunks.Length;
            for (var i = 0; i < chunks.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = chunks[i];
        }

        private static void Destroy((Sniper sniper, Transform player, SignalRushTuning tuning, Projectile projectilePrefab, GameObject root) fixture, Projectile spawned)
        {
            if (spawned != null)
                Object.DestroyImmediate(spawned.gameObject);
            Object.DestroyImmediate(fixture.root);
            Object.DestroyImmediate(fixture.tuning);
        }

        private static void Destroy((ChunkSpawner spawner, SignalRushTuning tuning, Transform player, GameObject root, GameObject[] prefabs) fixture)
        {
            for (var i = fixture.root.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(fixture.root.transform.GetChild(i).gameObject);
            Object.DestroyImmediate(fixture.root);
            Object.DestroyImmediate(fixture.tuning);
        }
    }
}
