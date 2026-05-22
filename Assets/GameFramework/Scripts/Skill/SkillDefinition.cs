using UnityEngine;

namespace GameFramework.Skill
{
    /// <summary>
    /// 技能配置资产。
    /// 通过同一份配置可以快速做出“位移技能 / 伤害技能 / 治疗技能”等不同技能。
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Skill/Skill Definition", fileName = "SkillDefinition")]
    public class SkillDefinition : ScriptableObject
    {
        [Header("基础信息")]
        public string skillId = "skill_active_01";
        public string displayName = "Burst";
        public SkillType skillType = SkillType.Active1;
        public SkillTargetMode targetMode = SkillTargetMode.SphereArea;

        [Header("冷却与消耗")]
        public float cooldown = 6f;
        public float energyCost = 0f;

        [Header("效果参数")]
        [Tooltip("瞬时伤害值（范围型伤害）。")]
        public float damage = 30f;

        [Tooltip("治疗值。")]
        public float heal = 0f;

        [Tooltip("范围半径（米）。0 表示单体（施法者正前方最近目标）。")]
        public float radius = 4f;

        [Tooltip("扇形角度（仅 ConeArea 生效）。")]
        [Range(1f, 180f)]
        public float coneAngle = 70f;

        [Tooltip("生效距离（以施法者为中心向前偏移）。")]
        public float castDistance = 4f;

        [Header("目标筛选")]
        [Tooltip("是否允许作用到自己。")]
        public bool allowSelf = false;

        [Tooltip("是否允许作用到友方。")]
        public bool allowFriendly = false;

        [Tooltip("是否允许作用到敌方。")]
        public bool allowHostile = true;
    }
}
