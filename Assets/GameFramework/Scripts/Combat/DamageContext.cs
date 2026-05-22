using UnityEngine;

namespace GameFramework.Combat
{
    /// <summary>
    /// 伤害来源类型：用于区分伤害统计、UI 提示和抗性逻辑。
    /// </summary>
    public enum DamageSourceType
    {
        Weapon,
        Skill,
        Environment
    }

    /// <summary>
    /// 伤害上下文：统一武器、技能、环境伤害的传参结构。
    /// </summary>
    public struct DamageContext
    {
        public GameObject Attacker;
        public GameObject Target;
        public float RawDamage;
        public DamageSourceType SourceType;
        public bool IsCritical;

        public DamageContext(GameObject attacker, GameObject target, float rawDamage, DamageSourceType sourceType, bool isCritical = false)
        {
            Attacker = attacker;
            Target = target;
            RawDamage = rawDamage;
            SourceType = sourceType;
            IsCritical = isCritical;
        }
    }
}
