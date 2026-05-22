using UnityEngine;

namespace GameFramework.Stats
{
    /// <summary>
    /// 状态效果配置（可做成 ScriptableObject 资产）。
    /// 当前先以序列化类提供最小可用骨架。
    /// </summary>
    [System.Serializable]
    public class StatusEffectData
    {
        public string effectId;
        public string displayName;
        public StatusEffectType effectType = StatusEffectType.Buff;

        [Tooltip("效果持续时间（秒）")]
        public float duration = 5f;

        [Tooltip("生命乘区修正，1.1=+10%，0.9=-10%")]
        public float healthMultiplier = 1f;

        [Tooltip("移动速度乘区修正")]
        public float moveSpeedMultiplier = 1f;

        [Tooltip("攻击速度乘区修正")]
        public float attackSpeedMultiplier = 1f;
    }
}
