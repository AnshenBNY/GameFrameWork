using UnityEngine;

namespace GameFramework.Combat
{
    /// <summary>
    /// 阵营组件：挂在角色/怪物对象上，提供统一敌我判定接口。
    /// </summary>
    public class FactionComponent : MonoBehaviour
    {
        [SerializeField] private FactionType faction = FactionType.Neutral;

        public FactionType Faction => faction;

        public bool IsHostileTo(FactionComponent other)
        {
            if (other == null)
            {
                return false;
            }

            if (faction == FactionType.Neutral || other.faction == FactionType.Neutral)
            {
                return false;
            }

            return faction != other.faction;
        }
    }
}
