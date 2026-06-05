using GameFramework.Core;
using GameFramework.Combat;
using GameFramework.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.Editor
{
    /// <summary>
    /// 一键创建可运行的最小关卡场景模板。
    /// </summary>
    public static class RuntimeSceneTemplateBuilder
    {
        private const string GeneratedRoot = "Assets/GameFramework/Generated";
        private const string ScenePath = GeneratedRoot + "/GF_RuntimeTemplate.unity";
        private const string LevelAssetPath = GeneratedRoot + "/GF_MinimalLevel.asset";
        private const string PlayerPrefabPath = GeneratedRoot + "/Player.prefab";
        private const string EnemyPrefabPath = GeneratedRoot + "/Zombie1.prefab";
        private const string GameControllerPrefabPath = GeneratedRoot + "/GameController.prefab";

        [MenuItem("Tools/GameFramework/Create Runtime Scene Template")]
        public static void CreateRuntimeSceneTemplate()
        {
            CombatLayerSetup.EnsureProjectLayers();
            ZombiePrefabConfigurator.ConfigureZombieEnemyPrefab();
            EnemyPrefabBuilder.CreateBasicEnemyPrefab();
            LevelTemplateBuilder.CreateMinimalLevelTemplate();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EnsureFolder(GeneratedRoot);

            BuildEnvironment();
            GameObject player = BuildPlayer();
            BuildCamera(player);
            BuildGameController();
            BuildFramework(player);

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("GameFramework", $"场景模板已生成：{ScenePath}", "OK");
        }

        private static void BuildEnvironment()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(6f, 1f, 6f);
            if (CombatLayers.Environment >= 0)
            {
                CombatLayerSetup.SetLayerRecursively(ground, CombatLayers.Environment);
            }

            for (int i = 0; i < 4; i++)
            {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"Wall_{i}";
                wall.transform.localScale = new Vector3(20f, 3f, 0.6f);
                switch (i)
                {
                    case 0: wall.transform.position = new Vector3(0f, 1.5f, 22f); break;
                    case 1: wall.transform.position = new Vector3(0f, 1.5f, -22f); break;
                    case 2:
                        wall.transform.position = new Vector3(22f, 1.5f, 0f);
                        wall.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                        break;
                    default:
                        wall.transform.position = new Vector3(-22f, 1.5f, 0f);
                        wall.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                        break;
                }

                if (CombatLayers.Environment >= 0)
                {
                    CombatLayerSetup.SetLayerRecursively(wall, CombatLayers.Environment);
                }
            }

            GameObject directional = new GameObject("Directional Light");
            Light light = directional.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            directional.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        }

        private static GameObject BuildPlayer()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                throw new System.InvalidOperationException($"未找到玩家预制体：{PlayerPrefabPath}");
            }

            GameObject player = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (player == null)
            {
                throw new System.InvalidOperationException("实例化玩家预制体失败。");
            }

            player.name = "Player";
            player.transform.position = new Vector3(0f, 0f, -12f);
            player.tag = "Player";
            if (CombatLayers.Player >= 0)
            {
                CombatLayerSetup.SetLayerRecursively(player, CombatLayers.Player);
            }

            ConfigurePlayerRuntimeDependencies(player);
            return player;
        }

        private static void BuildCamera(GameObject player)
        {
            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            Camera cam = cameraGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.01f;
            cameraGo.AddComponent<AudioListener>();

            var orbit = cameraGo.AddComponent<global::ThirdPersonOrbitCam>();
            orbit.player = player.transform;
            orbit.maxVerticalAngle = 60f;
            orbit.minVerticalAngle = -60f;

            var playerBasic = player.GetComponent<global::BasicBehaviour>();
            if (playerBasic != null)
            {
                playerBasic.playerCamera = cameraGo.transform;
                EditorUtility.SetDirty(playerBasic);
            }
        }

        private static void BuildGameController()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameControllerPrefabPath);
            if (prefab != null)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance != null)
                {
                    instance.name = "GameController";
                    instance.tag = "GameController";
                    return;
                }
            }

            GameObject fallback = new GameObject("GameController");
            fallback.tag = "GameController";

            GameObject pickupHud = new GameObject("PickupHUD");
            pickupHud.transform.SetParent(fallback.transform, false);
            GameObject pickupLabel = new GameObject("Label");
            pickupLabel.transform.SetParent(pickupHud.transform, false);
            Text pickupText = pickupLabel.AddComponent<Text>();
            pickupText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pickupText.text = "Pickup";
            pickupText.alignment = TextAnchor.MiddleCenter;
            pickupHud.SetActive(false);

            GameObject screenHud = new GameObject("ScreenHUD");
            screenHud.transform.SetParent(fallback.transform, false);
            BuildFallbackWeaponHud(screenHud);
            screenHud.AddComponent<global::WeaponUIManager>();
        }

        private static void BuildFramework(GameObject player)
        {
            LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelAssetPath);
            if (level == null)
            {
                throw new System.InvalidOperationException($"未找到关卡资产：{LevelAssetPath}");
            }

            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            if (enemyPrefab == null)
            {
                enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedRoot + "/GF_BasicEnemy.prefab");
            }
            if (enemyPrefab != null)
            {
                for (int i = 0; i < level.monsterSpawnPoints.Count; i++)
                {
                    if (level.monsterSpawnPoints[i].prefab == null)
                    {
                        level.monsterSpawnPoints[i].prefab = enemyPrefab;
                    }
                }
                EditorUtility.SetDirty(level);
            }

            GameObject root = new GameObject("GF_Root");
            LevelManager manager = root.AddComponent<LevelManager>();
            GameBootstrap bootstrap = root.AddComponent<GameBootstrap>();
            LevelResultListener resultListener = root.AddComponent<LevelResultListener>();
            root.AddComponent<LevelDebugHud>();

            SerializedObject managerSo = new SerializedObject(manager);
            managerSo.FindProperty("currentLevel").objectReferenceValue = level;
            managerSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject bootstrapSo = new SerializedObject(bootstrap);
            bootstrapSo.FindProperty("levelManager").objectReferenceValue = manager;
            bootstrapSo.FindProperty("firstLevel").objectReferenceValue = level;
            bootstrapSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject resultSo = new SerializedObject(resultListener);
            resultSo.FindProperty("levelManager").objectReferenceValue = manager;
            resultSo.FindProperty("player").objectReferenceValue = player;
            resultSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string fullPath)
        {
            if (AssetDatabase.IsValidFolder(fullPath))
            {
                return;
            }

            string[] parts = fullPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static void ConfigurePlayerRuntimeDependencies(GameObject player)
        {
            global::ShootBehaviour shoot = player.GetComponent<global::ShootBehaviour>();
            if (shoot == null)
            {
                return;
            }

            bool dirty = false;
            SerializedObject so = new SerializedObject(shoot);

            if (so.FindProperty("muzzleFlash").objectReferenceValue == null)
            {
                GameObject muzzleFlash = CreateEffectPlaceholder("FX_MuzzleFlash", player.transform);
                so.FindProperty("muzzleFlash").objectReferenceValue = muzzleFlash;
                dirty = true;
            }

            if (so.FindProperty("shot").objectReferenceValue == null)
            {
                GameObject shot = CreateEffectPlaceholder("FX_ShotTracer", player.transform);
                so.FindProperty("shot").objectReferenceValue = shot;
                dirty = true;
            }

            if (so.FindProperty("sparks").objectReferenceValue == null)
            {
                GameObject sparks = CreateEffectPlaceholder("FX_Sparks", player.transform);
                so.FindProperty("sparks").objectReferenceValue = sparks;
                dirty = true;
            }

            SerializedProperty shotMask = so.FindProperty("shotMask");
            if (shotMask != null && shotMask.intValue != CombatLayers.PlayerCombatMask.value)
            {
                shotMask.intValue = CombatLayers.PlayerCombatMask.value;
                dirty = true;
            }

            if (dirty)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static GameObject CreateEffectPlaceholder(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.SetActive(false);
            return go;
        }

        private static void BuildFallbackWeaponHud(GameObject screenHud)
        {
            GameObject weaponHud = new GameObject("WeaponHUD");
            weaponHud.transform.SetParent(screenHud.transform, false);

            GameObject weapon = new GameObject("Weapon");
            weapon.transform.SetParent(weaponHud.transform, false);
            Image weaponImage = weapon.AddComponent<Image>();
            weaponImage.raycastTarget = false;

            GameObject data = new GameObject("Data");
            data.transform.SetParent(weaponHud.transform, false);

            GameObject mag = new GameObject("Mag");
            mag.transform.SetParent(data.transform, false);

            for (int i = 0; i < 32; i++)
            {
                GameObject bullet = new GameObject($"Bullet_{i}");
                bullet.transform.SetParent(mag.transform, false);
                Image bulletImage = bullet.AddComponent<Image>();
                bulletImage.raycastTarget = false;
            }

            GameObject label = new GameObject("Label");
            label.transform.SetParent(data.transform, false);
            Text text = label.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "0/0";
            text.alignment = TextAnchor.MiddleLeft;
        }
    }
}
