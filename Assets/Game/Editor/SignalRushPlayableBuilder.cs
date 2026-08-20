using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UPnL.SignalRush.Combat;
using UPnL.SignalRush.Combo;
using UPnL.SignalRush.Player;
using UPnL.SignalRush.Run;
using UPnL.SignalRush.Tuning;
using UPnL.SignalRush.UI;
using UPnL.SignalRush.World;
using SignalRushPlayerInput = UPnL.SignalRush.Player.PlayerInput;

namespace UPnL.SignalRush.Editor
{
    public static class SignalRushPlayableBuilder
    {
        private const string Root = "Assets/Game";
        private const string ScenePath = "Assets/Scenes/SCN_SignalRush_Playable.unity";
        private const string InputPath = "Assets/Settings/InputSystem_Actions.inputactions";

        [MenuItem("Signal Rush/Rebuild Playable Scene")]
        public static void Build()
        {
            EnsureFolders();
            ConfigureLayers();

            var tuning = LoadOrCreateTuning();
            var inputReferences = RebuildInputActions();
            var whiteSprite = LoadOrCreateWhiteSprite();
            var projectile = CreateProjectilePrefab(whiteSprite);
            var obstacleA = CreateObstaclePrefab("A", whiteSprite, new Color(0.9f, 0.2f, 0.2f));
            var obstacleB = CreateObstaclePrefab("B", whiteSprite, new Color(1f, 0.45f, 0.1f));
            var sniper = CreateSniperPrefab(tuning, projectile, whiteSprite);
            var gameplay = new[]
            {
                CreateGameplayChunkPrefab("A", whiteSprite, obstacleA, -1.5f),
                CreateGameplayChunkPrefab("B", whiteSprite, obstacleB, 1.5f),
            };
            var decor = new[]
            {
                CreateDecorChunkPrefab("A", whiteSprite, new Color(0.12f, 0.18f, 0.28f)),
                CreateDecorChunkPrefab("B", whiteSprite, new Color(0.18f, 0.12f, 0.28f)),
            };
            var sniperChunks = new[]
            {
                CreateSniperChunkPrefab("A", whiteSprite, sniper, -2f),
                CreateSniperChunkPrefab("B", whiteSprite, sniper, 2f),
            };
            var playerPrefab = CreatePlayerPrefab(tuning, inputReferences, whiteSprite);
            var goalPrefab = CreateGoalPrefab(whiteSprite);

            AssetDatabase.SaveAssets();
            BuildScene(playerPrefab, goalPrefab, obstacleA, obstacleB, gameplay, decor, sniperChunks, whiteSprite);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Built playable scene: {ScenePath}");
        }

        private static void EnsureFolders()
        {
            EnsureFolder(Root, "Art");
            EnsureFolder($"{Root}/Art", "Graybox");
            EnsureFolder(Root, "Data");
            EnsureFolder(Root, "Input");
            EnsureFolder(Root, "Prefabs");
            EnsureFolder("Assets", "Scenes");
        }

        private static void EnsureFolder(string parent, string name)
        {
            var path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static void ConfigureLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            var names = new[] { "Player", "PlayerAttack", "Obstacle", "Projectile", "World", "Goal" };
            for (var i = 0; i < names.Length; i++)
                layers.GetArrayElementAtIndex(8 + i).stringValue = names[i];
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SignalRushTuning LoadOrCreateTuning()
        {
            const string path = Root + "/Data/SO_SignalRushTuning.asset";
            var tuning = AssetDatabase.LoadAssetAtPath<SignalRushTuning>(path);
            if (tuning != null)
                return tuning;

            tuning = ScriptableObject.CreateInstance<SignalRushTuning>();
            AssetDatabase.CreateAsset(tuning, path);
            return tuning;
        }

        private static InputActionReference[] RebuildInputActions()
        {
            var input = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = input.AddActionMap("Gameplay");
            var move = map.AddAction("Move", InputActionType.Value);
            move.expectedControlType = "Axis";
            move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick/x");
            var jump = map.AddAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");
            var attack = map.AddAction("Attack", InputActionType.Button);
            attack.AddBinding("<Keyboard>/x");
            attack.AddBinding("<Gamepad>/buttonWest");

            File.WriteAllText(InputPath, input.ToJson());
            Object.DestroyImmediate(input);
            AssetDatabase.ImportAsset(InputPath, ImportAssetOptions.ForceUpdate);
            var imported = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            return new[]
            {
                SaveActionReference(imported.FindAction("Gameplay/Move"), "Move"),
                SaveActionReference(imported.FindAction("Gameplay/Jump"), "Jump"),
                SaveActionReference(imported.FindAction("Gameplay/Attack"), "Attack"),
            };
        }

        private static InputActionReference SaveActionReference(InputAction action, string name)
        {
            var path = $"{Root}/Input/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            var reference = InputActionReference.Create(action);
            AssetDatabase.CreateAsset(reference, path);
            return reference;
        }

        private static Sprite LoadOrCreateWhiteSprite()
        {
            const string path = Root + "/Art/Graybox/SPR_White.asset";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;

            var texture = new Texture2D(1, 1) { name = "TEX_White" };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            AssetDatabase.CreateAsset(texture, path);
            sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            sprite.name = "SPR_White";
            AssetDatabase.AddObjectToAsset(sprite, texture);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject CreateProjectilePrefab(Sprite sprite)
        {
            var root = new GameObject("PF_Projectile");
            root.layer = LayerMask.NameToLayer("Projectile");
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.yellow;
            root.transform.localScale = Vector3.one * 0.3f;
            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            root.AddComponent<CircleCollider2D>().isTrigger = true;
            root.AddComponent<Projectile>();
            return SavePrefab(root, "PF_Projectile");
        }

        private static GameObject CreateObstaclePrefab(string suffix, Sprite sprite, Color color)
        {
            var root = new GameObject($"PF_Obstacle_{suffix}");
            root.layer = LayerMask.NameToLayer("Obstacle");
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            root.transform.localScale = new Vector3(0.8f, 1.6f, 1f);
            root.AddComponent<BoxCollider2D>().isTrigger = true;
            root.AddComponent<BreakableObstacle>();
            return SavePrefab(root, $"PF_Obstacle_{suffix}");
        }

        private static GameObject CreateSniperPrefab(SignalRushTuning tuning, GameObject projectilePrefab, Sprite sprite)
        {
            var root = new GameObject("PF_Sniper");
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0.85f, 0.2f, 0.85f);
            root.transform.localScale = Vector3.one * 0.7f;
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(root.transform, false);
            muzzle.localPosition = Vector3.right;
            var sniper = root.AddComponent<Sniper>();
            SetObject(sniper, "_tuning", tuning);
            SetObject(sniper, "_muzzle", muzzle);
            SetObject(sniper, "_projectilePrefab", projectilePrefab.GetComponent<Projectile>());
            return SavePrefab(root, "PF_Sniper");
        }

        private static GameObject CreateGameplayChunkPrefab(string suffix, Sprite sprite, GameObject obstaclePrefab, float obstacleX)
        {
            var root = new GameObject($"PF_Chunk_Gameplay_{suffix}");
            root.layer = LayerMask.NameToLayer("World");
            root.AddComponent<Chunk>();
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0.25f, 0.65f, 0.45f);
            root.transform.localScale = new Vector3(6f, 0.5f, 1f);
            root.AddComponent<BoxCollider2D>();
            var obstacle = (GameObject)PrefabUtility.InstantiatePrefab(obstaclePrefab);
            obstacle.name = "Obstacle";
            obstacle.transform.SetParent(root.transform, false);
            obstacle.transform.localPosition = new Vector3(obstacleX / 6f, 1.5f, 0f);
            obstacle.transform.localScale = new Vector3(0.13f, 3.2f, 1f);
            return SavePrefab(root, $"PF_Chunk_Gameplay_{suffix}");
        }

        private static GameObject CreateDecorChunkPrefab(string suffix, Sprite sprite, Color color)
        {
            var root = new GameObject($"PF_Chunk_Decor_{suffix}");
            root.AddComponent<Chunk>();
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = -10;
            root.transform.localScale = new Vector3(8f, 5f, 1f);
            return SavePrefab(root, $"PF_Chunk_Decor_{suffix}");
        }

        private static GameObject CreateSniperChunkPrefab(string suffix, Sprite sprite, GameObject sniperPrefab, float sniperX)
        {
            var root = new GameObject($"PF_Chunk_Sniper_{suffix}");
            var chunk = root.AddComponent<Chunk>();
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0.16f, 0.16f, 0.2f);
            renderer.sortingOrder = -5;
            root.transform.localScale = new Vector3(6f, 3f, 1f);
            var sniperObject = (GameObject)PrefabUtility.InstantiatePrefab(sniperPrefab);
            sniperObject.name = "Sniper";
            sniperObject.transform.SetParent(root.transform, false);
            sniperObject.transform.localPosition = new Vector3(sniperX / 6f, 0f, 0f);
            sniperObject.transform.localScale = new Vector3(0.12f, 0.23f, 1f);
            SetObject(chunk, "_sniper", sniperObject.GetComponent<Sniper>());
            return SavePrefab(root, $"PF_Chunk_Sniper_{suffix}");
        }

        private static GameObject CreatePlayerPrefab(SignalRushTuning tuning, InputActionReference[] actions, Sprite sprite)
        {
            var root = new GameObject("PF_Player");
            root.SetActive(false);
            root.layer = LayerMask.NameToLayer("Player");
            var body = root.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            root.AddComponent<CapsuleCollider2D>().size = new Vector2(0.8f, 1.6f);
            var status = root.AddComponent<PlayerStatus>();
            var combo = root.AddComponent<ComboCounter>();
            var motor = root.AddComponent<PlayerMotor2D>();
            var combat = root.AddComponent<PlayerCombat>();
            var input = root.AddComponent<SignalRushPlayerInput>();

            var visual = new GameObject("Visual");
            visual.layer = root.layer;
            visual.transform.SetParent(root.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0.15f, 0.8f, 1f);
            visual.transform.localScale = new Vector3(0.8f, 1.6f, 1f);

            var groundProbe = new GameObject("GroundProbe").transform;
            groundProbe.SetParent(root.transform, false);
            groundProbe.localPosition = new Vector3(0f, -0.85f, 0f);
            var attackHitbox = new GameObject("AttackHitbox");
            attackHitbox.layer = LayerMask.NameToLayer("PlayerAttack");
            attackHitbox.transform.SetParent(root.transform, false);
            attackHitbox.transform.localPosition = new Vector3(0.9f, 0f, 0f);
            var attackCollider = attackHitbox.AddComponent<BoxCollider2D>();
            attackCollider.isTrigger = true;
            attackCollider.size = new Vector2(1.5f, 1.3f);

            SetObject(status, "_tuning", tuning);
            SetObject(combo, "_tuning", tuning);
            SetObject(motor, "_body", body);
            SetObject(motor, "_tuning", tuning);
            SetObject(motor, "_status", status);
            SetObject(motor, "_groundProbe", groundProbe);
            SetInt(motor, "_groundLayers", 1 << LayerMask.NameToLayer("World"));
            SetObject(combat, "_attackHitbox", attackCollider);
            SetObject(combat, "_tuning", tuning);
            SetObject(combat, "_combo", combo);
            SetObject(combat, "_status", status);
            SetObject(input, "_move", actions[0]);
            SetObject(input, "_jump", actions[1]);
            SetObject(input, "_attack", actions[2]);
            SetObject(input, "_motor", motor);
            SetObject(input, "_combat", combat);
            root.SetActive(true);
            return SavePrefab(root, "PF_Player");
        }

        private static GameObject CreateGoalPrefab(Sprite sprite)
        {
            var root = new GameObject("PF_Goal");
            root.layer = LayerMask.NameToLayer("Goal");
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0.2f, 1f, 0.35f);
            root.transform.localScale = new Vector3(0.8f, 4f, 1f);
            root.AddComponent<BoxCollider2D>().isTrigger = true;
            root.AddComponent<GoalTrigger>();
            return SavePrefab(root, "PF_Goal");
        }

        private static GameObject SavePrefab(GameObject root, string name)
        {
            var path = $"{Root}/Prefabs/{name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildScene(
            GameObject playerPrefab,
            GameObject goalPrefab,
            GameObject obstacleA,
            GameObject obstacleB,
            GameObject[] gameplay,
            GameObject[] decor,
            GameObject[] sniperChunks,
            Sprite sprite)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var tuning = AssetDatabase.LoadAssetAtPath<SignalRushTuning>(Root + "/Data/SO_SignalRushTuning.asset");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 1.1f, 0f);
            var status = player.GetComponent<PlayerStatus>();
            var combo = player.GetComponent<ComboCounter>();
            var motor = player.GetComponent<PlayerMotor2D>();
            var combat = player.GetComponent<PlayerCombat>();
            var input = player.GetComponent<SignalRushPlayerInput>();

            CreateCamera(player.transform);
            CreateGround(sprite);
            CreateFixedObstacles(obstacleA, obstacleB);

            var goalObject = (GameObject)PrefabUtility.InstantiatePrefab(goalPrefab, scene);
            goalObject.name = "Goal";
            goalObject.transform.position = new Vector3(80f, 2f, 0f);
            var goal = goalObject.GetComponent<GoalTrigger>();

            var runObject = new GameObject("Run");
            var run = runObject.AddComponent<RunController>();
            SetObject(run, "_goalTrigger", goal);
            var spawnerObject = new GameObject("ChunkSpawner");
            var origin = new GameObject("SpawnOrigin").transform;
            origin.SetParent(spawnerObject.transform, false);
            origin.position = new Vector3(6f, 0f, 0f);
            var spawner = spawnerObject.AddComponent<ChunkSpawner>();
            SetObject(spawner, "_origin", origin);
            SetObject(spawner, "_player", player.transform);
            SetArray(spawner, "_gameplayFrontPrefabs", gameplay);
            SetArray(spawner, "_decorFrontPrefabs", decor);
            SetArray(spawner, "_sniperRearPrefabs", sniperChunks);
            SetObject(spawner, "_tuning", tuning);

            SetObject(input, "_runController", run);
            var bridge = player.AddComponent<SignalRushPlayable>();
            SetObject(bridge, "_runController", run);
            SetObject(bridge, "_goalTrigger", goal);
            SetObject(bridge, "_playerStatus", status);
            SetObject(bridge, "_playerMotor", motor);
            SetObject(bridge, "_playerCombat", combat);
            SetObject(bridge, "_comboCounter", combo);
            SetObject(bridge, "_chunkSpawner", spawner);
            SetObject(bridge, "_player", player.transform);
            SetFloat(bridge, "_fallY", -8f);

            BuildUi(combo, run);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static void CreateCamera(Transform player)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player, false);
            cameraObject.transform.localPosition = new Vector3(4f, 2f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.backgroundColor = new Color(0.03f, 0.05f, 0.09f);
            cameraObject.AddComponent<AudioListener>();
        }

        private static void CreateGround(Sprite sprite)
        {
            var ground = new GameObject("Ground");
            ground.layer = LayerMask.NameToLayer("World");
            ground.transform.position = new Vector3(40f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(100f, 1f, 1f);
            var renderer = ground.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0.18f, 0.22f, 0.25f);
            ground.AddComponent<BoxCollider2D>();
        }

        private static void CreateFixedObstacles(GameObject obstacleA, GameObject obstacleB)
        {
            var root = new GameObject("Obstacles").transform;
            var prefabs = new[] { obstacleA, obstacleB, obstacleA, obstacleB };
            for (var i = 0; i < prefabs.Length; i++)
            {
                var obstacle = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i]);
                obstacle.transform.SetParent(root, false);
                obstacle.transform.position = new Vector3(14f + i * 14f, 0.8f, 0f);
            }
        }

        private static void BuildUi(ComboCounter combo, RunController run)
        {
            var canvasObject = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var comboText = CreateText(canvasObject.transform, "ComboText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(130f, -30f), "Combo 0  Best 0");
            var timeText = CreateText(canvasObject.transform, "TimeText", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-100f, -30f), "Time 0.0");
            var hud = canvasObject.AddComponent<RunHud>();
            SetObject(hud, "_combo", combo);
            SetObject(hud, "_runController", run);
            SetObject(hud, "_comboText", comboText);
            SetObject(hud, "_elapsedText", timeText);

            var resultRoot = new GameObject("Result", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            resultRoot.transform.SetParent(canvasObject.transform, false);
            var rect = resultRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(360f, 140f);
            resultRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);
            var resultText = CreateText(resultRoot.transform, "ResultText", Vector2.zero, Vector2.one, Vector2.zero, "GoalReached\nPress X to restart");
            resultText.alignment = TextAnchor.MiddleCenter;
            resultText.rectTransform.offsetMin = Vector2.zero;
            resultText.rectTransform.offsetMax = Vector2.zero;
            var resultView = canvasObject.AddComponent<ResultView>();
            SetObject(resultView, "_runController", run);
            SetObject(resultView, "_resultText", resultText);
            SetObject(resultView, "_resultRoot", resultRoot);
            resultRoot.SetActive(false);
        }

        private static Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, string value)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.color = Color.white;
            text.text = value;
            var rect = text.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = new Vector2(260f, 50f);
            rect.anchoredPosition = position;
            return text;
        }

        private static void SetObject(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string field, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string field, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string field, GameObject[] prefabs)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            property.arraySize = prefabs.Length;
            for (var i = 0; i < prefabs.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i].GetComponent<Chunk>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
