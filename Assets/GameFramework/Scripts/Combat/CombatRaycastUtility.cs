using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Combat
{
    /// <summary>
    /// 战斗射线检测工具：
    /// - 按距离排序
    /// - 剔除 owner 自身层级（含子节点 Collider）
    /// - 剔除指定 Layer（如 Player 层不参与玩家瞄准）
    /// </summary>
    public static class CombatRaycastUtility
    {
        private static readonly List<RaycastHit> HitBuffer = new List<RaycastHit>(16);

        /// <summary>
        /// 获取射线上第一个有效命中。若无命中返回 false，hit 为 default。
        /// </summary>
        public static bool TryFirstHit(
            Ray ray,
            float maxDistance,
            LayerMask layerMask,
            Transform ignoreRoot,
            out RaycastHit hit,
            params int[] ignoredLayers)
        {
            HitBuffer.Clear();
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
            HitBuffer.AddRange(hits);
            HitBuffer.Sort((a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < HitBuffer.Count; i++)
            {
                RaycastHit candidate = HitBuffer[i];
                if (ShouldIgnore(candidate.collider, ignoreRoot, ignoredLayers))
                {
                    continue;
                }

                hit = candidate;
                return true;
            }

            hit = default;
            return false;
        }

        private static bool ShouldIgnore(Collider collider, Transform ignoreRoot, int[] ignoredLayers)
        {
            if (collider == null)
            {
                return true;
            }

            if (ignoreRoot != null && collider.transform.IsChildOf(ignoreRoot))
            {
                return true;
            }

            if (ignoredLayers != null)
            {
                int colliderLayer = collider.gameObject.layer;
                for (int i = 0; i < ignoredLayers.Length; i++)
                {
                    if (ignoredLayers[i] >= 0 && colliderLayer == ignoredLayers[i])
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
