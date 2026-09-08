using GameFramework.AI;
using GameFramework.Combat;
using GameFramework.Skill;
using GameFramework.Stats;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace GameFramework.Editor
{
    /// <summary>
    /// 为 Zombie 预制体补齐敌人运行必需组件，便于调试与模板复用。
    /// </summary>
    public static class ZombiePrefabConfigurator
    {
        private const string ZombiePath = "Assets/GameFramework/Generated/Zombie1.prefab";
        private const string BiteSkillPath = "Assets/GameFramework/Generated/skill_enemy_bite.asset";
        private const string RoarSkillPath = "Assets/GameFramework/Generated/skill_enemy_roar.asset";

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

            // 技能施放器：bite -> Active1（主用），roar -> Ultimate（备用）。
            SkillCaster caster = GetOrAdd<SkillCaster>(root);
            SerializedObject casterSo = new SerializedObject(caster);
            AssignSkill(casterSo, "loadout.active1", BiteSkillPath);
            AssignSkill(casterSo, "loadout.ultimate", RoarSkillPath);
            casterSo.ApplyModifiedPropertiesWithoutUndo();
            AlignSkillType(RoarSkillPath, SkillType.Ultimate);

            // NavMesh 寻路代理：人形 pivot 在脚底，baseOffset 用 0。
            // 用公开 API 设置（比猜 SerializedProperty 名更稳），SaveAsPrefabAsset 会持久化。
            NavMeshAgent agent = GetOrAdd<NavMeshAgent>(root);
            agent.baseOffset = 0f;
            agent.radius = 0.35f;
            agent.height = 1.8f;
            agent.speed = 2.35f;
            agent.angularSpeed = 720f;
            agent.acceleration = 40f;
            agent.stoppingDistance = 1.15f;
            agent.autoBraking = true;

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
                // 保持根运动驱动位移（避免滑步）；NavMesh 仅提供寻路方向。
                aiSo.FindProperty("useAnimationRootMotionForMovement").boolValue = true;

                // AI 增强开关。
                aiSo.FindProperty("useNavMeshAgent").boolValue = true;
                aiSo.FindProperty("useSkills").boolValue = true;
                aiSo.FindProperty("enemySkillType").enumValueIndex = (int)SkillType.Active1;
                aiSo.FindProperty("skillRange").floatValue = 2.6f;
                aiSo.FindProperty("skillCastLockDuration").floatValue = 1.15f;
                aiSo.FindProperty("respondToGunshots").boolValue = true;
                aiSo.FindProperty("hearingRadius").floatValue = 28f;
                aiSo.FindProperty("investigateDuration").floatValue = 6f;

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

        private static void AssignSkill(SerializedObject so, string propertyPath, string assetPath)
        {
            SerializedProperty prop = so.FindProperty(propertyPath);
            if (prop == null)
            {
                return;
            }

            SkillDefinition skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(assetPath);
            if (skill != null)
            {
                prop.objectReferenceValue = skill;
            }
        }

        private static void AlignSkillType(string assetPath, SkillType type)
        {
            SkillDefinition skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(assetPath);
            if (skill == null)
            {
                return;
            }

            SerializedObject so = new SerializedObject(skill);
            SerializedProperty prop = so.FindProperty("skillType");
            if (prop != null && prop.enumValueIndex != (int)type)
            {
                prop.enumValueIndex = (int)type;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(skill);
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
