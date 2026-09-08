using GameFramework.Combat;
using GameFramework.Skill;
using GameFramework.Stats;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor
{
    /// <summary>
    /// 为 Player 预制体补齐框架层运行组件，打通“玩家 -> 属性/阵营/技能”闭环。
    /// 对齐 <see cref="ZombiePrefabConfigurator"/> 的做法，统一走 Editor 一键配置，
    /// 避免手改 prefab YAML。
    /// </summary>
    public static class PlayerPrefabConfigurator
    {
        private const string PlayerPath = "Assets/GameFramework/Generated/Player.prefab";
        private const string BlastSkillPath = "Assets/GameFramework/Generated/skill_player_blast.asset";
        private const string HealSkillPath = "Assets/GameFramework/Generated/skill_player_heal.asset";
        private const string UltSkillPath = "Assets/GameFramework/Generated/skill_player_ult.asset";

        [MenuItem("Tools/GameFramework/Configure Player Prefab")]
        public static void ConfigurePlayerPrefab()
        {
            CombatLayerSetup.EnsureProjectLayers();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("GameFramework", $"未找到玩家预制体：{PlayerPath}", "OK");
                return;
            }

            AlignSkillTypes();

            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPath);
            if (root == null)
            {
                EditorUtility.DisplayDialog("GameFramework", $"加载玩家预制体失败：{PlayerPath}", "OK");
                return;
            }

            try
            {
                EnsureRequiredComponents(root);
                if (CombatLayers.Player >= 0)
                {
                    CombatLayerSetup.SetLayerRecursively(root, CombatLayers.Player);
                }

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            EditorUtility.DisplayDialog("GameFramework", "Player 已完成属性/阵营/技能组件配置。", "OK");
        }

        private static void EnsureRequiredComponents(GameObject root)
        {
            // 1) 属性组件：挂在根节点，敌人攻击命中 hit.transform.root 时才能扣血。
            ActorStatsComponent stats = GetOrAdd<ActorStatsComponent>(root);
            SerializedObject statsSo = new SerializedObject(stats);
            statsSo.FindProperty("baseAttributes.baseHealth").floatValue = 200f;
            statsSo.FindProperty("baseAttributes.armor").floatValue = 20f;
            statsSo.FindProperty("baseAttributes.moveSpeed").floatValue = 5f;
            statsSo.FindProperty("baseAttributes.baseAttackSpeed").floatValue = 1f;
            statsSo.ApplyModifiedPropertiesWithoutUndo();

            // 2) 阵营：玩家阵营，用于技能/射击的敌我判定。
            FactionComponent faction = GetOrAdd<FactionComponent>(root);
            faction.SetFaction(FactionType.Player);

            // 3) 技能施放器：绑定 3 个技能槽。
            SkillCaster caster = GetOrAdd<SkillCaster>(root);
            SerializedObject casterSo = new SerializedObject(caster);
            AssignSkill(casterSo, "loadout.active1", BlastSkillPath);
            AssignSkill(casterSo, "loadout.active2", HealSkillPath);
            AssignSkill(casterSo, "loadout.ultimate", UltSkillPath);
            // affectMask 保持默认 ~0（全部层）：技能敌我目标由 CanAffect 的阵营判定负责，
            // 若在此排除 Player 层会导致治疗/增益无法作用到玩家与友军。
            casterSo.ApplyModifiedPropertiesWithoutUndo();

            // 4) 输入层：把 1/2/Q 映射到技能施放。
            GetOrAdd<PlayerSkillInput>(root);
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

        /// <summary>
        /// 修正技能资产的 skillType 字段，使其与所处槽位语义一致（原资产均误配为 Active1）。
        /// TryCast 按槽位路由，本步骤主要为语义正确与被动技能判定服务。
        /// </summary>
        private static void AlignSkillTypes()
        {
            SetSkillType(HealSkillPath, SkillType.Active2);
            SetSkillType(UltSkillPath, SkillType.Ultimate);
        }

        private static void SetSkillType(string assetPath, SkillType type)
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
