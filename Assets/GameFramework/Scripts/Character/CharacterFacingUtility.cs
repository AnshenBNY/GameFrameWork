using UnityEngine;

namespace GameFramework.Character
{
    /// <summary>
    /// 角色朝向工具：
    /// 统一约定 Root 的 +Z 轴（Vector3.forward）为“正面”，与 Unity Transform.forward 一致。
    /// </summary>
    public static class CharacterFacingUtility
    {
        /// <summary>
        /// 让 transform 的 +Z 轴（forward）对齐到世界水平方向。
        /// </summary>
        public static Quaternion GetFacingRotation(Vector3 worldDirection)
        {
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude <= 0.0001f)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(worldDirection.normalized, Vector3.up);
        }

        /// <summary>
        /// 平滑旋转 transform，使其 +Z 轴对齐目标水平方向。
        /// </summary>
        public static void RotateToward(Transform target, Vector3 worldDirection, float speed)
        {
            if (target == null)
            {
                return;
            }

            Quaternion targetRot = GetFacingRotation(worldDirection);
            target.rotation = Quaternion.Slerp(target.rotation, targetRot, Time.deltaTime * speed);
        }

        /// <summary>
        /// 平滑旋转 transform，使其 +Z 轴对齐目标方向（含俯仰，用于 Muzzle 等）。
        /// </summary>
        public static void RotateToward3D(Transform target, Vector3 worldDirection, float speed)
        {
            if (target == null || worldDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRot = Quaternion.LookRotation(worldDirection.normalized, Vector3.up);
            target.rotation = Quaternion.Slerp(target.rotation, targetRot, Time.deltaTime * speed);
        }
    }
}
