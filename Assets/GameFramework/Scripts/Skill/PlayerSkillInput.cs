using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Skill
{
    /// <summary>
    /// 玩家技能输入层：把键盘按键映射到 <see cref="SkillCaster.TryCast"/>。
    /// 设计要点：
    /// 1. 输入与技能逻辑解耦——本组件只负责“何时施放”，具体效果全部交给 SkillCaster。
    /// 2. 绑定表数据化，可在 Inspector 增删按键或改键，无需改代码。
    /// 3. 施放结果（成功/冷却中）通过事件暴露，便于 HUD 做反馈。
    /// </summary>
    [RequireComponent(typeof(SkillCaster))]
    public class PlayerSkillInput : MonoBehaviour
    {
        [Serializable]
        public struct SkillKeyBinding
        {
            [Tooltip("触发该技能的按键。")]
            public KeyCode key;

            [Tooltip("对应的技能槽位。")]
            public SkillType skillType;
        }

        [SerializeField] private SkillCaster caster;

        [Tooltip("按键到技能槽位的映射，默认 1/2/Q 对应两个主动技能与终极技能。")]
        [SerializeField]
        private List<SkillKeyBinding> bindings = new List<SkillKeyBinding>
        {
            new SkillKeyBinding { key = KeyCode.Alpha1, skillType = SkillType.Active1 },
            new SkillKeyBinding { key = KeyCode.Alpha2, skillType = SkillType.Active2 },
            new SkillKeyBinding { key = KeyCode.Q, skillType = SkillType.Ultimate },
        };

        [Tooltip("仅在鼠标锁定（游戏进行中）时响应技能输入，避免在菜单/失焦状态误触发。")]
        [SerializeField] private bool requireCursorLocked = false;

        /// <summary>技能成功施放时触发（参数为技能槽位），HUD 可据此刷新冷却显示。</summary>
        public event Action<SkillType> OnSkillCast;

        /// <summary>技能因冷却或未装配而施放失败时触发。</summary>
        public event Action<SkillType> OnSkillRejected;

        private void Awake()
        {
            if (caster == null)
            {
                caster = GetComponent<SkillCaster>();
            }
        }

        private void Update()
        {
            if (caster == null)
            {
                return;
            }

            if (requireCursorLocked && Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            for (int i = 0; i < bindings.Count; i++)
            {
                SkillKeyBinding binding = bindings[i];
                if (!Input.GetKeyDown(binding.key))
                {
                    continue;
                }

                if (caster.TryCast(binding.skillType))
                {
                    OnSkillCast?.Invoke(binding.skillType);
                }
                else
                {
                    OnSkillRejected?.Invoke(binding.skillType);
                }
            }
        }
    }
}
