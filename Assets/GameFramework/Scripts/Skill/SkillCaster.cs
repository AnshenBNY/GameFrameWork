using System.Collections.Generic;
using GameFramework.Combat;
using GameFramework.Stats;
using UnityEngine;

namespace GameFramework.Skill
{
    /// <summary>
    /// 技能施放器：
    /// 1. 管理技能冷却。
    /// 2. 根据配置执行伤害/治疗逻辑。
    /// 3. 提供统一的 TryCast 接口给角色控制或 AI。
    /// </summary>
    public class SkillCaster : MonoBehaviour
    {
        [SerializeField] private SkillLoadout loadout = new SkillLoadout();
        [SerializeField] private LayerMask affectMask = ~0;

        private readonly Dictionary<string, float> _cooldownEndTimes = new Dictionary<string, float>();

        public SkillLoadout Loadout => loadout;

        public bool TryCast(SkillType type)
        {
            SkillDefinition skill = loadout.GetByType(type);
            if (skill == null)
            {
                return false;
            }

            if (!IsSkillReady(skill))
            {
                return false;
            }

            ExecuteSkill(skill);
            _cooldownEndTimes[skill.skillId] = Time.time + Mathf.Max(0f, skill.cooldown);
            return true;
        }

        public bool IsSkillReady(SkillDefinition skill)
        {
            if (skill == null)
            {
                return false;
            }

            if (!_cooldownEndTimes.TryGetValue(skill.skillId, out float endTime))
            {
                return true;
            }

            return Time.time >= endTime;
        }

        private void ExecuteSkill(SkillDefinition skill)
        {
            Vector3 center = transform.position + transform.forward * Mathf.Max(0f, skill.castDistance);
            float radius = Mathf.Max(0f, skill.radius);

            // 被动技能默认只在自身生效（比如常驻回血、属性强化）。
            if (skill.skillType == SkillType.Passive)
            {
                ApplyToSingleTarget(skill, gameObject);
                return;
            }

            switch (skill.targetMode)
            {
                case SkillTargetMode.SelfOrSingle:
                    GameObject single = FindNearestTarget(center, Mathf.Max(1.5f, radius), skill);
                    if (single == null && skill.allowSelf)
                    {
                        single = gameObject;
                    }
                    if (single != null)
                    {
                        ApplyToSingleTarget(skill, single);
                    }
                    break;

                case SkillTargetMode.ConeArea:
                    ApplyConeArea(skill, center, Mathf.Max(1.5f, radius), skill.coneAngle);
                    break;

                case SkillTargetMode.SphereArea:
                default:
                    ApplySphereArea(skill, center, Mathf.Max(1.5f, radius));
                    break;
            }
        }

        private void ApplySphereArea(SkillDefinition skill, Vector3 center, float radius)
        {
            Collider[] targets = Physics.OverlapSphere(center, radius, affectMask, QueryTriggerInteraction.Ignore);
            foreach (Collider target in targets)
            {
                GameObject actor = target.attachedRigidbody != null ? target.attachedRigidbody.gameObject : target.gameObject;
                ApplyToSingleTarget(skill, actor);
            }
        }

        private void ApplyConeArea(SkillDefinition skill, Vector3 center, float radius, float coneAngle)
        {
            float half = Mathf.Clamp(coneAngle, 1f, 180f) * 0.5f;
            Collider[] targets = Physics.OverlapSphere(center, radius, affectMask, QueryTriggerInteraction.Ignore);
            foreach (Collider target in targets)
            {
                GameObject actor = target.attachedRigidbody != null ? target.attachedRigidbody.gameObject : target.gameObject;
                ActorStatsComponent stats = actor.GetComponentInParent<ActorStatsComponent>();
                if (stats == null)
                {
                    continue;
                }

                Vector3 toTarget = (stats.transform.position - transform.position);
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude <= 0.001f)
                {
                    ApplyToSingleTarget(skill, stats.gameObject);
                    continue;
                }

                float angle = Vector3.Angle(transform.forward, toTarget.normalized);
                if (angle <= half)
                {
                    ApplyToSingleTarget(skill, stats.gameObject);
                }
            }
        }

        private GameObject FindNearestTarget(Vector3 center, float radius, SkillDefinition skill)
        {
            Collider[] targets = Physics.OverlapSphere(center, radius, affectMask, QueryTriggerInteraction.Ignore);
            float bestDistance = float.MaxValue;
            GameObject best = null;

            foreach (Collider col in targets)
            {
                GameObject actor = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;
                ActorStatsComponent stats = actor.GetComponentInParent<ActorStatsComponent>();
                if (stats == null)
                {
                    continue;
                }

                if (!CombatTargetingUtility.CanAffect(
                        gameObject,
                        stats.gameObject,
                        skill.allowSelf,
                        skill.allowFriendly,
                        skill.allowHostile))
                {
                    continue;
                }

                float d = (stats.transform.position - transform.position).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = stats.gameObject;
                }
            }

            return best;
        }

        private void ApplyToSingleTarget(SkillDefinition skill, GameObject target)
        {
            ActorStatsComponent stats = target.GetComponentInParent<ActorStatsComponent>();
            if (stats == null)
            {
                return;
            }

            if (!CombatTargetingUtility.CanAffect(
                    gameObject,
                    stats.gameObject,
                    skill.allowSelf,
                    skill.allowFriendly,
                    skill.allowHostile))
            {
                return;
            }

            if (skill.damage > 0f)
            {
                DamageContext context = new DamageContext(gameObject, stats.gameObject, skill.damage, DamageSourceType.Skill);
                stats.ApplyDamage(context);
            }

            if (skill.heal > 0f)
            {
                stats.Heal(skill.heal);
            }
        }
    }
}
