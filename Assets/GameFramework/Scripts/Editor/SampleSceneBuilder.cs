using System.IO;
using GameFramework.AI;
using GameFramework.Character;
using GameFramework.Combat;
using GameFramework.Core;
using GameFramework.Level;
using GameFramework.Skill;
using GameFramework.Stats;
using GameFramework.Weapon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.Editor
{
    /// <summary>
    /// 一键构建最小可玩样例场景：
    /// - 生成玩家、怪物、地面、相机、HUD、关卡管理器
    /// - 自动创建基础武器/技能/关卡配置资产
    /// 
    /// 使用方式：
    /// 1) 点击菜单 Tools/GameFramework/Build Minimal Playable Sample
    /// 2) 保存场景后直接 Play
    /// </summary>
    public static class SampleSceneBuilder
    {
        private const string AssetRoot = "Assets/GameFramework/Generated";

        [MenuItem("Tools/GameFramework/Build Minimal Playable Sample")]
        public static void BuildSample()
        {
            EnsureFolders();
            CombatLayerSetup.EnsureProjectLayers();
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 地面
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(5f, 1f, 5f);
            CombatLayerSetup.SetLayerRecursively(ground, CombatLayers.Environment);

            // 资产
            WeaponDefinition playerWeapon = CreatePlayerWeaponAsset();
            SkillDefinition playerSkill1 = CreatePlayerSkillAsset("skill_player_blast", "Blast", 20f, 0f, 4f, 4f, 3f, false, false, true, SkillTargetMode.SphereArea);
            SkillDefinition playerSkill2 = CreatePlayerSkillAsset("skill_player_heal", "Self Heal", 0f, 25f, 0f, 0f, 8f, true, true, false, SkillTargetMode.SelfOrSingle);
            SkillDefinition playerUlt = CreatePlayerSkillAsset("skill_player_ult", "Ultimate Burst", 45f, 0f, 6f, 5f, 12f, false, false, true, SkillTargetMode.ConeArea);

            WeaponDefinition enemyWeapon = CreateEnemyWeaponAsset();
            SkillDefinition enemySkill1 = CreatePlayerSkillAsset("skill_enemy_bite", "Bite", 12f, 0f, 3f, 2f, 4f, false, false, true, SkillTargetMode.SelfOrSingle);
            SkillDefinition enemyUlt = CreatePlayerSkillAsset("skill_enemy_roar", "Roar", 20f, 0f, 5f, 3f, 9f, false, false, true, SkillTargetMode.ConeArea);

            // 玩家
            GameObject player = CreatePlayer(playerWeapon, playerSkill1, playerSkill2, playerUlt);

            // 怪物预制体（作为场景对象再保存为 prefab）
            GameObject monsterTemplate = CreateMonsterTemplate(enemyWeapon, enemySkill1, enemyUlt, player.transform);
            string monsterPrefabPath = $"{AssetRoot}/Monster.prefab";
            GameObject monsterPrefab = PrefabUtility.SaveAsPrefabAsset(monsterTemplate, monsterPrefabPath);
            Object.DestroyImmediate(monsterTemplate);

            // 关卡配置与管理器
            LevelDefinition level = CreateLevelAsset(monsterPrefab);
            LevelManager levelManager = CreateLevelManager(level);

            // 启动器
            GameObject bootstrap = new GameObject("GameBootstrap");
            GameBootstrap gameBootstrap = bootstrap.AddComponent<GameBootstrap>();
            SerializedObject bootstrapSo = new SerializedObject(gameBootstrap);
            bootstrapSo.FindProperty("levelManager").objectReferenceValue = levelManager;
            bootstrapSo.FindProperty("firstLevel").objectReferenceValue = level;
            bootstrapSo.ApplyModifiedPropertiesWithoutUndo();

            // 相机
            SetupCamera(player.transform);

            // HUD
            BuildHud(player);

            // 场景保存
            string scenePath = $"{AssetRoot}/SampleScene.unity";
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("GameFramework", "最小可玩样例已生成：Assets/GameFramework/Generated/SampleScene.unity", "OK");
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/GameFramework/Generated"))
            {
                AssetDatabase.CreateFolder("Assets/GameFramework", "Generated");
            }
        }

        private static WeaponDefinition CreatePlayerWeaponAsset()
        {
            WeaponDefinition asset = ScriptableObject.CreateInstance<WeaponDefinition>();
            asset.weaponId = "weapon_player_shotgun";
            asset.displayName = "Player Shotgun";
            asset.pelletDamage = 5f;
            asset.pelletsPerShot = 8;
            asset.spreadAngle = 4f;
            asset.fireInterval = 0.4f;
            asset.clipSize = 24;
            asset.initialReserveAmmo = 96;
            asset.reloadDuration = 1.6f;
            asset.infiniteReserveAmmo = false;
            asset.range = 30f;
            asset.automatic = true;
            return SaveAsset(asset, $"{AssetRoot}/PlayerWeapon.asset");
        }

        private static WeaponDefinition CreateEnemyWeaponAsset()
        {
            WeaponDefinition asset = ScriptableObject.CreateInstance<WeaponDefinition>();
            asset.weaponId = "weapon_enemy_rifle";
            asset.displayName = "Enemy Rifle";
            asset.pelletDamage = 4f;
            asset.pelletsPerShot = 1;
            asset.spreadAngle = 1.5f;
            asset.fireInterval = 0.8f;
            asset.clipSize = 20;
            asset.initialReserveAmmo = 9999;
            asset.reloadDuration = 1.2f;
            asset.infiniteReserveAmmo = true;
            asset.range = 25f;
            asset.automatic = true;
            return SaveAsset(asset, $"{AssetRoot}/EnemyWeapon.asset");
        }

        private static SkillDefinition CreatePlayerSkillAsset(
            string id,
            string displayName,
            float damage,
            float heal,
            float radius,
            float castDistance,
            float cooldown,
            bool allowSelf = false,
            bool allowFriendly = false,
            bool allowHostile = true,
            SkillTargetMode targetMode = SkillTargetMode.SphereArea)
        {
            SkillDefinition asset = ScriptableObject.CreateInstance<SkillDefinition>();
            asset.skillId = id;
            asset.displayName = displayName;
            asset.damage = damage;
            asset.heal = heal;
            asset.radius = radius;
            asset.castDistance = castDistance;
            asset.cooldown = cooldown;
            asset.allowSelf = allowSelf;
            asset.allowFriendly = allowFriendly;
            asset.allowHostile = allowHostile;
            asset.targetMode = targetMode;
            return SaveAsset(asset, $"{AssetRoot}/{id}.asset");
        }

        private static LevelDefinition CreateLevelAsset(GameObject monsterPrefab)
        {
            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.levelId = "level_sample_001";
            level.displayName = "Sample Arena";
            level.mapPrefab = null;

            level.monsterSpawnPoints.Add(new SpawnPointData
            {
                spawnId = "monster_1",
                position = new Vector3(0f, 1f, 14f),
                eulerAngles = Vector3.zero,
                prefab = monsterPrefab
            });
            level.monsterSpawnPoints.Add(new SpawnPointData
            {
                spawnId = "monster_2",
                position = new Vector3(5f, 1f, 18f),
                eulerAngles = Vector3.zero,
                prefab = monsterPrefab
            });
            level.monsterSpawnPoints.Add(new SpawnPointData
            {
                spawnId = "monster_3",
                position = new Vector3(-5f, 1f, 18f),
                eulerAngles = Vector3.zero,
                prefab = monsterPrefab
            });

            level.triggers.Add(new LevelTriggerData
            {
                triggerId = "start_zone",
                position = new Vector3(0f, 1f, 0f),
                size = new Vector3(6f, 2f, 6f)
            });

            // 线性阶段：进入出发区 -> 清两波怪 -> 完成关卡。
            level.phases.Add(new LevelPhaseData
            {
                phaseId = "phase_reach_start",
                phaseType = LevelPhaseType.ReachTrigger,
                requiredTriggerId = "start_zone"
            });
            level.phases.Add(new LevelPhaseData
            {
                phaseId = "phase_defend_waves",
                phaseType = LevelPhaseType.DefendWaves,
                requiredWaveCount = 2
            });
            level.phases.Add(new LevelPhaseData
            {
                phaseId = "phase_complete",
                phaseType = LevelPhaseType.Complete
            });

            return SaveAsset(level, $"{AssetRoot}/SampleLevel.asset");
        }

        private static LevelManager CreateLevelManager(LevelDefinition level)
        {
            GameObject managerGo = new GameObject("LevelManager");
            LevelManager manager = managerGo.AddComponent<LevelManager>();
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("currentLevel").objectReferenceValue = level;
            so.ApplyModifiedPropertiesWithoutUndo();
            return manager;
        }

        private static GameObject CreatePlayer(
            WeaponDefinition weaponDef,
            SkillDefinition skill1,
            SkillDefinition skill2,
            SkillDefinition ult)
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 1f, 0f);

            Object.DestroyImmediate(player.GetComponent<Collider>());
            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.4f;

            player.AddComponent<FactionComponent>();

            ActorStatsComponent stats = player.AddComponent<ActorStatsComponent>();
            SerializedObject statsSo = new SerializedObject(stats);
            statsSo.FindProperty("baseAttributes.baseHealth").floatValue = 140f;
            statsSo.FindProperty("baseAttributes.moveSpeed").floatValue = 6.5f;
            statsSo.FindProperty("baseAttributes.jumpCount").intValue = 2;
            statsSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(player.transform);
            muzzle.transform.localPosition = new Vector3(0f, 1.2f, 0.5f);

            WeaponRuntime weaponRuntime = player.AddComponent<WeaponRuntime>();
            player.AddComponent<WeaponShotVisualizer>();
            SerializedObject weaponSo = new SerializedObject(weaponRuntime);
            weaponSo.FindProperty("definition").objectReferenceValue = weaponDef;
            weaponSo.FindProperty("firePoint").objectReferenceValue = muzzle.transform;
            weaponSo.FindProperty("useCombatLayerDefaults").boolValue = true;
            weaponSo.ApplyModifiedPropertiesWithoutUndo();

            SkillCaster caster = player.AddComponent<SkillCaster>();
            SerializedObject casterSo = new SerializedObject(caster);
            casterSo.FindProperty("loadout.active1").objectReferenceValue = skill1;
            casterSo.FindProperty("loadout.active2").objectReferenceValue = skill2;
            casterSo.FindProperty("loadout.ultimate").objectReferenceValue = ult;
            casterSo.ApplyModifiedPropertiesWithoutUndo();

            ThirdPersonCharacterController controller = player.AddComponent<ThirdPersonCharacterController>();
            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("weaponRuntime").objectReferenceValue = weaponRuntime;
            controllerSo.FindProperty("skillCaster").objectReferenceValue = caster;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            SetFaction(player, FactionType.Player);
            CombatLayerSetup.SetLayerRecursively(player, CombatLayers.Player);
            return player;
        }

        private static GameObject CreateMonsterTemplate(
            WeaponDefinition weaponDef,
            SkillDefinition skill1,
            SkillDefinition ult,
            Transform player)
        {
            GameObject monster = GameObject.CreatePrimitive(PrimitiveType.Cube);
            monster.name = "MonsterTemplate";
            monster.transform.position = new Vector3(0f, 1f, 10f);
            monster.transform.localScale = new Vector3(1f, 2f, 1f);

            monster.AddComponent<FactionComponent>();

            ActorStatsComponent stats = monster.AddComponent<ActorStatsComponent>();
            SerializedObject statsSo = new SerializedObject(stats);
            statsSo.FindProperty("baseAttributes.baseHealth").floatValue = 80f;
            statsSo.FindProperty("baseAttributes.moveSpeed").floatValue = 3.5f;
            statsSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(monster.transform);
            muzzle.transform.localPosition = new Vector3(0f, 1f, 0.6f);

            WeaponRuntime weaponRuntime = monster.AddComponent<WeaponRuntime>();
            SerializedObject weaponSo = new SerializedObject(weaponRuntime);
            weaponSo.FindProperty("definition").objectReferenceValue = weaponDef;
            weaponSo.FindProperty("firePoint").objectReferenceValue = muzzle.transform;
            weaponSo.FindProperty("useCombatLayerDefaults").boolValue = true;
            weaponSo.ApplyModifiedPropertiesWithoutUndo();

            SkillCaster caster = monster.AddComponent<SkillCaster>();
            SerializedObject casterSo = new SerializedObject(caster);
            casterSo.FindProperty("loadout.active1").objectReferenceValue = skill1;
            casterSo.FindProperty("loadout.ultimate").objectReferenceValue = ult;
            casterSo.ApplyModifiedPropertiesWithoutUndo();

            MonsterBrain brain = monster.AddComponent<MonsterBrain>();
            SerializedObject brainSo = new SerializedObject(brain);
            brainSo.FindProperty("target").objectReferenceValue = player;
            brainSo.FindProperty("weaponRuntime").objectReferenceValue = weaponRuntime;
            brainSo.FindProperty("skillCaster").objectReferenceValue = caster;
            brainSo.ApplyModifiedPropertiesWithoutUndo();

            SetFaction(monster, FactionType.Enemy);
            CombatLayerSetup.SetLayerRecursively(monster, CombatLayers.Enemy);
            return monster;
        }

        private static void SetupCamera(Transform player)
        {
            // 肩部枢轴：相机围绕该点 orbiting，不影响角色自身旋转。
            GameObject pivotGo = new GameObject("CameraPivot");
            pivotGo.transform.SetParent(player, false);
            pivotGo.transform.localPosition = new Vector3(0f, 1.55f, 0f);

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraGo = new GameObject("Main Camera");
                cameraGo.tag = "MainCamera";
                mainCamera = cameraGo.AddComponent<Camera>();
                cameraGo.AddComponent<AudioListener>();
            }

            SimpleFollowCamera oldFollow = mainCamera.gameObject.GetComponent<SimpleFollowCamera>();
            if (oldFollow != null)
            {
                Object.DestroyImmediate(oldFollow);
            }

            ThirdPersonCameraController cameraController = mainCamera.gameObject.GetComponent<ThirdPersonCameraController>();
            if (cameraController == null)
            {
                cameraController = mainCamera.gameObject.AddComponent<ThirdPersonCameraController>();
            }

            SerializedObject cameraSo = new SerializedObject(cameraController);
            cameraSo.FindProperty("useCombatLayerDefaults").boolValue = true;
            cameraSo.ApplyModifiedPropertiesWithoutUndo();

            cameraController.SetTarget(player, pivotGo.transform);

            ThirdPersonCharacterController playerController = player.GetComponent<ThirdPersonCharacterController>();
            if (playerController != null)
            {
                SerializedObject controllerSo = new SerializedObject(playerController);
                controllerSo.FindProperty("cameraController").objectReferenceValue = cameraController;
                controllerSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void BuildHud(GameObject player)
        {
            GameObject canvasGo = new GameObject("HUDCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject healthTextGo = CreateHudText(canvas.transform, "HealthText", new Vector2(20f, -20f));
            GameObject ammoTextGo = CreateHudText(canvas.transform, "AmmoText", new Vector2(20f, -55f));
            GameObject skillTextGo = CreateHudText(canvas.transform, "SkillText", new Vector2(20f, -90f));

            PlayerCombatHud hud = canvasGo.AddComponent<PlayerCombatHud>();
            SerializedObject hudSo = new SerializedObject(hud);
            hudSo.FindProperty("playerStats").objectReferenceValue = player.GetComponent<ActorStatsComponent>();
            hudSo.FindProperty("weaponRuntime").objectReferenceValue = player.GetComponent<WeaponRuntime>();
            hudSo.FindProperty("skillCaster").objectReferenceValue = player.GetComponent<SkillCaster>();
            hudSo.FindProperty("healthText").objectReferenceValue = healthTextGo.GetComponent<Text>();
            hudSo.FindProperty("ammoText").objectReferenceValue = ammoTextGo.GetComponent<Text>();
            hudSo.FindProperty("skillText").objectReferenceValue = skillTextGo.GetComponent<Text>();
            hudSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateHudText(Transform parent, string name, Vector2 anchoredPos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(700f, 30f);

            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.color = Color.white;
            text.text = $"{name}...";

            return go;
        }

        private static T SaveAsset<T>(T asset, string path) where T : Object
        {
            string dir = Path.GetDirectoryName(path)?.Replace("\\", "/");
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                string[] parts = dir.Split('/');
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

            AssetDatabase.CreateAsset(asset, path);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static void SetFaction(GameObject go, FactionType faction)
        {
            FactionComponent component = go.GetComponent<FactionComponent>();
            if (component == null)
            {
                return;
            }

            SerializedObject so = new SerializedObject(component);
            so.FindProperty("faction").enumValueIndex = (int)faction;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
