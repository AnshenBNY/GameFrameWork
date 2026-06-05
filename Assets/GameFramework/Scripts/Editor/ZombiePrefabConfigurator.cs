using GameFramework.AI;
using GameFramework.Combat;
using GameFramework.Stats;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor
{
    /// <summary>
    /// 为 Zombie 预制体补齐敌人运行必需组件，便于调试与模板复用。
    /// </summary>
    public static class ZombiePrefabConfigurator
    {
        private const string ZombiePath = "Assets/GameFramework/Generated/Zombie1.prefab";

        [MenuItem("Tools/GameFramework/Configure Zombie Enemy Prefab")]
        public static void ConfigureZombieEnemyPrefab()
        {
            CombatLayerSetup.EnsureProjectLayers();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZombiePath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("GameFramework", $"未找到 Zombie 预制体：{ZombiePath}", "OK");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(ZombiePath);
            if (root == null)
            {
                EditorUtility.DisplayDialog("GameFramework", $"加载 Zombie 预制体失败：{ZombiePath}", "OK");
                return;
            }

            try
            {
                EnsureRequiredComponents(root);
                if (CombatLayers.Enemy >= 0)
                {
                    CombatLayerSetup.SetLayerRecursively(root, CombatLayers.Enemy);
                }

                PrefabUtility.SaveAsPrefabAsset(root, ZombiePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            EditorUtility.DisplayDialog("GameFramework", "Zombie1 已完成敌人组件配置。", "OK");
        }

        private static void EnsureRequiredComponents(GameObject root)
        {
            ActorStatsComponent stats = GetOrAdd<ActorStatsComponent>(root);
            SerializedObject statsSo = new SerializedObject(stats);
            statsSo.FindProperty("baseAttributes.baseHealth").floatValue = 140f;
            statsSo.FindProperty("baseAttributes.moveSpeed").floatValue = 2.35f;
            statsSo.FindProperty("baseAttributes.armor").floatValue = 6f;
            statsSo.ApplyModifiedPropertiesWithoutUndo();

            FactionComponent faction = GetOrAdd<FactionComponent>(root);
            faction.SetFaction(FactionType.Enemy);

            GetOrAdd<EnemySimpleHealthBar>(root);
            BasicEnemyController ai = GetOrAdd<BasicEnemyController>(root);

            Animator animator = root.GetComponent<Animator>();
            if (animator == null)
            {
                animator = root.GetComponentInChildren<Animator>();
            }

            if (animator != null && ai != null)
            {
                SerializedObject aiSo = new SerializedObject(ai);
                aiSo.FindProperty("animator").objectReferenceValue = animator;
                aiSo.FindProperty("firePoint").objectReferenceValue = root.transform;
                aiSo.FindProperty("detectRange").floatValue = 16f;
                aiSo.FindProperty("attackRange").floatValue = 1.85f;
                aiSo.FindProperty("stoppingDistance").floatValue = 1.15f;
                aiSo.FindProperty("moveSpeed").floatValue = 2.35f;
                aiSo.FindProperty("turnSpeed").floatValue = 6.5f;
                aiSo.FindProperty("attackSpeed").floatValue = 1.05f;
                aiSo.FindProperty("useStatusMoveSpeed").boolValue = true;
                aiSo.FindProperty("useStatusAttackSpeed").boolValue = true;
                aiSo.FindProperty("attackDamage").floatValue = 14f;
                aiSo.FindProperty("attackEventFallbackDuration").floatValue = 1.15f;
                aiSo.FindProperty("targetRefreshInterval").floatValue = 0.35f;
                aiSo.FindProperty("destroyDelayAfterDeath").floatValue = 2.8f;
                aiSo.FindProperty("disableDamageCollidersOnDeath").boolValue = true;
                aiSo.FindProperty("disableRigidbodyOnDeath").boolValue = true;
                aiSo.FindProperty("useAnimationRootMotionForMovement").boolValue = true;

                // 默认适配你当前 Zombie1 动画机（参数驱动）。
                aiSo.FindProperty("speedParamName").stringValue = "Speed";
                aiSo.FindProperty("moveSpeedParamName").stringValue = "MoveSpeed";
                aiSo.FindProperty("attackSpeedParamName").stringValue = "AttackSpeed";
                aiSo.FindProperty("walkBoolParamName").stringValue = "Walk";
                aiSo.FindProperty("detectTriggerParamName").stringValue = "Detect";
                aiSo.FindProperty("attackTriggerAParamName").stringValue = "Attack";
                aiSo.FindProperty("attackTriggerBParamName").stringValue = "Attack_2";
                aiSo.FindProperty("hitTriggerParamName").stringValue = "Hit";
                aiSo.FindProperty("deadBoolParamName").stringValue = "Dead";
                aiSo.FindProperty("deadTriggerParamName").stringValue = "Dead";
                aiSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T existing = go.GetComponent<T>();
            if (existing != null)
            {
                return existing;
            }

            return go.AddComponent<T>();
        }
    }
}
