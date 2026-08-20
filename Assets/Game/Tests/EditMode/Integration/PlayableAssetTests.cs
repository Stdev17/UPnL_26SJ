using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UPnL.SignalRush.Player;
using UPnL.SignalRush.Run;
using UPnL.SignalRush.World;
using SignalRushPlayerInput = UPnL.SignalRush.Player.PlayerInput;

namespace UPnL.SignalRush.Tests.Integration
{
    public sealed class PlayableAssetTests
    {
        private const string ScenePath = "Assets/Scenes/SCN_SignalRush_Playable.unity";

        [Test]
        public void RuntimeInputContainsExactlyThreeApprovedActions()
        {
            var input = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/InputSystem_Actions.inputactions");

            Assert.That(input, Is.Not.Null);
            Assert.That(input.actionMaps.Count, Is.EqualTo(1));
            Assert.That(input.actionMaps[0].actions.Select(action => action.name),
                Is.EquivalentTo(new[] { "Move", "Jump", "Attack" }));
            Assert.That(input.FindAction("Restart"), Is.Null);
        }

        [Test]
        public void PlayableSceneHasCompleteRuntimeWiring()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), Is.Not.Null);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var player = roots.Single(root => root.name == "Player");
            var bridge = player.GetComponent<SignalRushPlayable>();
            var input = player.GetComponent<SignalRushPlayerInput>();
            var spawner = roots.Single(root => root.name == "ChunkSpawner").GetComponent<ChunkSpawner>();

            Assert.That(player.GetComponent<Rigidbody2D>(), Is.Not.Null);
            Assert.That(bridge, Is.Not.Null);
            Assert.That(input, Is.Not.Null);
            AssertObjectReferencesAssigned(bridge, "_runController", "_goalTrigger", "_playerStatus", "_playerMotor",
                "_playerCombat", "_comboCounter", "_chunkSpawner", "_player");
            AssertObjectReferencesAssigned(input, "_move", "_jump", "_attack", "_motor", "_combat", "_runController");

            var serializedSpawner = new SerializedObject(spawner);
            AssertObjectReferencesAssigned(spawner, "_tuning", "_origin", "_player");
            Assert.That(serializedSpawner.FindProperty("_gameplayFrontPrefabs").arraySize, Is.EqualTo(2));
            Assert.That(serializedSpawner.FindProperty("_decorFrontPrefabs").arraySize, Is.EqualTo(2));
            Assert.That(serializedSpawner.FindProperty("_sniperRearPrefabs").arraySize, Is.EqualTo(2));
            Assert.That(roots.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount), Is.Zero);
        }

        [Test]
        public void GameplayLayersAndBuildSceneAreRegistered()
        {
            Assert.That(LayerMask.NameToLayer("Player"), Is.EqualTo(8));
            Assert.That(LayerMask.NameToLayer("PlayerAttack"), Is.EqualTo(9));
            Assert.That(LayerMask.NameToLayer("Obstacle"), Is.EqualTo(10));
            Assert.That(LayerMask.NameToLayer("Projectile"), Is.EqualTo(11));
            Assert.That(LayerMask.NameToLayer("World"), Is.EqualTo(12));
            Assert.That(LayerMask.NameToLayer("Goal"), Is.EqualTo(13));
            Assert.That(EditorBuildSettings.scenes.Single().path, Is.EqualTo(ScenePath));
            Assert.That(EditorBuildSettings.scenes.Single().enabled, Is.True);
        }

        private static void AssertObjectReferencesAssigned(Object target, params string[] fields)
        {
            var serialized = new SerializedObject(target);
            foreach (var field in fields)
                Assert.That(serialized.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
        }
    }
}
