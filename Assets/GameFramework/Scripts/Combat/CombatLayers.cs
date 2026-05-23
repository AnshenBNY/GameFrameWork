using UnityEngine;

namespace GameFramework.Combat
{
    /// <summary>
    /// 战斗相关 Layer 常量与 LayerMask 构建工具。
    /// 约定：
    /// - Player：玩家碰撞体（可被敌人命中，不参与玩家自身瞄准/射击判定）
    /// - Enemy：敌人碰撞体
    /// - Environment：场景静态碰撞（地面、墙体等）
    /// </summary>
    public static class CombatLayers
    {
        public const string PlayerLayerName = "Player";
        public const string EnemyLayerName = "Enemy";
        public const string EnvironmentLayerName = "Environment";

        public static int Player => LayerMask.NameToLayer(PlayerLayerName);
        public static int Enemy => LayerMask.NameToLayer(EnemyLayerName);
        public static int Environment => LayerMask.NameToLayer(EnvironmentLayerName);

        /// <summary>
        /// 玩家瞄准/射击：命中 Default + Environment + Enemy，排除 Player。
        /// </summary>
        public static LayerMask PlayerCombatMask => BuildMask(Player, Default, Environment, Enemy);

        /// <summary>
        /// 敌人射击：命中 Default + Environment + Player，排除 Enemy。
        /// </summary>
        public static LayerMask EnemyCombatMask => BuildMask(Enemy, Default, Environment, Player);

        /// <summary>
        /// 根据阵营返回默认射击 LayerMask。
        /// </summary>
        public static LayerMask GetCombatMaskForFaction(FactionType faction)
        {
            switch (faction)
            {
                case FactionType.Player:
                    return PlayerCombatMask;
                case FactionType.Enemy:
                    return EnemyCombatMask;
                default:
                    return BuildMask(-1, Default, Environment, Player, Enemy);
            }
        }

        private static int Default => 0;

        /// <param name="excludeLayer">要排除的 Layer，传 -1 表示不排除。</param>
        /// <param name="optionalFourthLayer">可选第四层，传 -1 表示忽略。</param>
        private static LayerMask BuildMask(int excludeLayer, int layerA, int layerB, int layerC, int optionalFourthLayer = -1)
        {
            int mask = 0;
            AddLayer(ref mask, layerA);
            AddLayer(ref mask, layerB);
            AddLayer(ref mask, layerC);
            AddLayer(ref mask, optionalFourthLayer);

            if (excludeLayer >= 0)
            {
                mask &= ~(1 << excludeLayer);
            }

            return mask;
        }

        private static void AddLayer(ref int mask, int layer)
        {
            if (layer >= 0 && layer < 32)
            {
                mask |= 1 << layer;
            }
        }
    }
}
