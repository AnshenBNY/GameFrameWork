using UnityEngine;

namespace GameFramework.Weapon
{
    /// <summary>
    /// 武器定义（ScriptableObject）：
    /// 用于配置武器的静态参数，便于不同武器复用同一套运行时逻辑。
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Weapon/Weapon Definition", fileName = "WeaponDefinition")]
    public class WeaponDefinition : ScriptableObject
    {
        [Header("基础信息")]
        public string weaponId = "weapon_rifle_01";
        public string displayName = "Rifle";

        [Header("伤害参数")]
        [Tooltip("单发弹丸伤害。若一次射击会发射多个弹丸，则每颗都按此值计算。")]
        public float pelletDamage = 8f;

        [Tooltip("每次射击发射的弹丸数量。霰弹枪可配置为 8~12。")]
        public int pelletsPerShot = 1;

        [Tooltip("基础扩散角（度）。")]
        public float spreadAngle = 1.5f;

        [Tooltip("最大射程（米）。")]
        public float range = 80f;

        [Header("射击节奏")]
        [Tooltip("射击间隔（秒）。例如 1 秒一次。")]
        public float fireInterval = 0.15f;

        [Header("弹药")]
        [Tooltip("弹匣容量。")]
        public int clipSize = 30;

        [Tooltip("初始后备弹药。")]
        public int initialReserveAmmo = 120;

        [Tooltip("单次换弹耗时（秒）。")]
        public float reloadDuration = 1.8f;

        [Tooltip("是否启用无限后备弹药（用于原型测试）。")]
        public bool infiniteReserveAmmo = false;

        [Tooltip("是否自动射击。")]
        public bool automatic = true;

        [Header("调试")]
        [Tooltip("若未设置特效，本地调试时可绘制弹道。")]
        public bool debugDrawRay = true;
    }
}
