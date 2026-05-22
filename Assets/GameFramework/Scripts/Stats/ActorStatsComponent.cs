using System.Collections.Generic;
using GameFramework.Combat;
using GameFramework.Core;
using UnityEngine;

namespace GameFramework.Stats
{
    /// <summary>
    /// 角色/怪物通用属性组件。
    /// 负责：
    /// 1. 管理当前生命值。
    /// 2. 承担伤害结算入口。
    /// 3. 管理增益/减益并实时刷新属性乘区。
    /// </summary>
    public class ActorStatsComponent : MonoBehaviour
    {
        [Header("基础属性")]
        [SerializeField] private AttributeSet baseAttributes = new AttributeSet();

        [Header("运行时状态")]
        [SerializeField] private float currentHealth;

        private readonly List<RuntimeStatusEffect> _activeEffects = new List<RuntimeStatusEffect>();

        /// <summary>
        /// 当前属性快照（会被状态效果实时影响）。
        /// </summary>
        public AttributeSet CurrentAttributes { get; private set; } = new AttributeSet();

        public float CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0f;

        private void Awake()
        {
            RecalculateAttributes();
            currentHealth = CurrentAttributes.MaxHealth;
        }

        private void Update()
        {
            TickEffects(Time.deltaTime);
        }

        /// <summary>
        /// 应用伤害并返回最终伤害值。
        /// </summary>
        public float ApplyDamage(DamageContext context)
        {
            if (IsDead)
            {
                return 0f;
            }

            // 先吃护甲减伤，再做最小伤害保护。
            float reduction = CurrentAttributes.GetDamageReductionRatio();
            float finalDamage = Mathf.Max(1f, context.RawDamage * (1f - reduction));
            currentHealth = Mathf.Max(0f, currentHealth - finalDamage);

            if (IsDead)
            {
                GameEventBus.RaiseActorDied(gameObject);
            }

            return finalDamage;
        }

        /// <summary>
        /// 治疗接口，通常用于道具和治疗技能。
        /// </summary>
        public void Heal(float amount)
        {
            if (IsDead)
            {
                return;
            }

            currentHealth = Mathf.Min(CurrentAttributes.MaxHealth, currentHealth + Mathf.Max(0f, amount));
        }

        /// <summary>
        /// 添加一个状态效果（Buff 或 Debuff）。
        /// </summary>
        public void AddStatusEffect(StatusEffectData data)
        {
            if (data == null)
            {
                return;
            }

            _activeEffects.Add(new RuntimeStatusEffect(data));
            RecalculateAttributes();
        }

        private void TickEffects(float deltaTime)
        {
            bool changed = false;
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                _activeEffects[i].RemainingTime -= deltaTime;
                if (_activeEffects[i].RemainingTime <= 0f)
                {
                    _activeEffects.RemoveAt(i);
                    changed = true;
                }
            }

            if (changed)
            {
                RecalculateAttributes();
            }
        }

        /// <summary>
        /// 重新聚合基础属性与状态效果，得到最终属性。
        /// </summary>
        private void RecalculateAttributes()
        {
            float previousMaxHealth = CurrentAttributes != null ? Mathf.Max(1f, CurrentAttributes.MaxHealth) : Mathf.Max(1f, baseAttributes.MaxHealth);
            float previousHealthRatio = previousMaxHealth > 0f ? Mathf.Clamp01(currentHealth / previousMaxHealth) : 1f;

            CurrentAttributes = CloneAttributes(baseAttributes);

            float healthMul = 1f;
            float moveMul = 1f;
            float attackMul = 1f;

            foreach (RuntimeStatusEffect effect in _activeEffects)
            {
                healthMul *= effect.Data.healthMultiplier;
                moveMul *= effect.Data.moveSpeedMultiplier;
                attackMul *= effect.Data.attackSpeedMultiplier;
            }

            CurrentAttributes.baseHealth *= healthMul;
            CurrentAttributes.moveSpeed *= moveMul;
            CurrentAttributes.baseAttackSpeed *= attackMul;
            CurrentAttributes.buffPowerMultiplier = Mathf.Max(0.1f, healthMul * moveMul * attackMul);
            CurrentAttributes.debuffPowerMultiplier = 1f / CurrentAttributes.buffPowerMultiplier;

            // 当最大生命变化时，保持当前血量比例，避免出现“免费回血/掉血突变”。
            currentHealth = Mathf.Clamp(previousHealthRatio * CurrentAttributes.MaxHealth, 0f, CurrentAttributes.MaxHealth);
        }

        private static AttributeSet CloneAttributes(AttributeSet source)
        {
            return new AttributeSet
            {
                baseHealth = source.baseHealth,
                extraHealth = source.extraHealth,
                armor = source.armor,
                moveSpeed = source.moveSpeed,
                baseAttackSpeed = source.baseAttackSpeed,
                jumpHeight = source.jumpHeight,
                jumpCount = source.jumpCount,
                level = source.level,
                experience = source.experience,
                buffPowerMultiplier = source.buffPowerMultiplier,
                debuffPowerMultiplier = source.debuffPowerMultiplier
            };
        }

        private sealed class RuntimeStatusEffect
        {
            public RuntimeStatusEffect(StatusEffectData data)
            {
                Data = data;
                RemainingTime = data.duration;
            }

            public StatusEffectData Data { get; }
            public float RemainingTime { get; set; }
        }
    }
}
