using UnityEngine;

namespace GameFramework.Stats
{
    /// <summary>
    /// 属性容器：覆盖玩家与怪物的共同属性。
    /// 拆分为“基础值 + 运行时值”时，本结构用于承载基础配置和实时刷新结果。
    /// </summary>
    [System.Serializable]
    public class AttributeSet
    {
        [Header("生命与防御")]
        public float baseHealth = 100f;
        public float extraHealth = 0f;
        public float armor = 0f;

        [Header("移动与动作")]
        public float moveSpeed = 6f;
        public float baseAttackSpeed = 1f;
        public float jumpHeight = 1.5f;
        public int jumpCount = 1;

        [Header("成长")]
        public int level = 1;
        public float experience = 0f;

        [Header("增益与减益（聚合值，最终由状态系统计算）")]
        public float buffPowerMultiplier = 1f;
        public float debuffPowerMultiplier = 1f;

        /// <summary>
        /// 最大生命值（基础生命 + 额外生命）。
        /// </summary>
        public float MaxHealth => Mathf.Max(1f, baseHealth + extraHealth);

        /// <summary>
        /// 简化减伤公式：护甲越高，减伤越多但有上限。
        /// 你后续可替换为更复杂的曲线公式。
        /// </summary>
        public float GetDamageReductionRatio()
        {
            // 这里用常见近似公式：armor / (armor + 100)
            return armor <= 0f ? 0f : armor / (armor + 100f);
        }
    }
}
