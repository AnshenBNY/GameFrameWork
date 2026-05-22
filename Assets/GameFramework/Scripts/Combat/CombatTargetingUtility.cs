using GameFramework.Stats;
using UnityEngine;

namespace GameFramework.Combat
{
    /// <summary>
    /// 战斗目标筛选工具：
    /// 将“是否可作用”规则集中在这里，避免各系统重复判断。
    /// </summary>
    public static class CombatTargetingUtility
    {
        public static bool CanAffect(GameObject caster, GameObject target, bool allowSelf, bool allowFriendly, bool allowHostile)
        {
            if (caster == null || target == null)
            {
                return false;
            }

            ActorStatsComponent targetStats = target.GetComponentInParent<ActorStatsComponent>();
            if (targetStats == null || targetStats.IsDead)
            {
                return false;
            }

            GameObject targetRoot = targetStats.gameObject;
            if (targetRoot == caster)
            {
                return allowSelf;
            }

            FactionComponent casterFaction = caster.GetComponentInParent<FactionComponent>();
            FactionComponent targetFaction = targetRoot.GetComponentInParent<FactionComponent>();

            // 没有阵营信息时，默认按可作用处理，避免过度阻断原型联调。
            if (casterFaction == null || targetFaction == null)
            {
                return true;
            }

            bool hostile = casterFaction.IsHostileTo(targetFaction);
            if (hostile)
            {
                return allowHostile;
            }

            return allowFriendly;
        }
    }
}
