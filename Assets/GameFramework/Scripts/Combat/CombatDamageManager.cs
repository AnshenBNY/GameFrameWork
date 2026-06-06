using System;
using GameFramework.Stats;
using UnityEngine;

namespace GameFramework.Combat
{
    /// <summary>
    /// 统一伤害管理器：
    /// - 统一处理伤害入口
    /// - 对外广播伤害结算结果
    /// </summary>
    public static class CombatDamageManager
    {
        private static readonly RuntimeConfig DefaultConfig = new RuntimeConfig(1f, true, true);
        private static RuntimeConfig _runtimeConfig = DefaultConfig;

        /// <summary>
        /// 参数：伤害上下文、最终伤害值、是否导致目标死亡。
        /// </summary>
        public static event Action<DamageContext, float, bool> OnDamageResolved;

        public static void SetConfig(CombatDamageConfig config)
        {
            if (config == null)
            {
                _runtimeConfig = DefaultConfig;
                return;
            }

            _runtimeConfig = new RuntimeConfig(
                config.MinDamage,
                config.ClampRawDamageToNonNegative,
                config.UseArmorReduction);
        }

        public static float ApplyDamage(DamageContext context)
        {
            if (context.Target == null)
            {
                return 0f;
            }

            ActorStatsComponent targetStats = context.Target.GetComponentInParent<ActorStatsComponent>();
            if (targetStats == null || targetStats.IsDead)
            {
                return 0f;
            }

            float finalDamage = CalculateFinalDamage(context, targetStats);
            float dealt = targetStats.ApplyFinalDamage(finalDamage);
            bool killed = targetStats.IsDead;
            OnDamageResolved?.Invoke(context, dealt, killed);
            return dealt;
        }

        public static float CalculateFinalDamage(DamageContext context, ActorStatsComponent targetStats)
        {
            if (targetStats == null || targetStats.IsDead)
            {
                return 0f;
            }

            float raw = context.RawDamage;
            if (_runtimeConfig.ClampRawDamageToNonNegative)
            {
                raw = Mathf.Max(0f, raw);
            }
            if (raw <= 0f)
            {
                return 0f;
            }

            float reduction = _runtimeConfig.UseArmorReduction
                ? targetStats.CurrentAttributes.GetDamageReductionRatio()
                : 0f;
            return Mathf.Max(_runtimeConfig.MinDamage, raw * (1f - reduction));
        }

        private readonly struct RuntimeConfig
        {
            public RuntimeConfig(float minDamage, bool clampRawDamageToNonNegative, bool useArmorReduction)
            {
                MinDamage = Mathf.Max(0f, minDamage);
                ClampRawDamageToNonNegative = clampRawDamageToNonNegative;
                UseArmorReduction = useArmorReduction;
            }

            public float MinDamage { get; }
            public bool ClampRawDamageToNonNegative { get; }
            public bool UseArmorReduction { get; }
        }
    }
}
