using UnityEngine;

namespace GameFramework.Combat
{
    [CreateAssetMenu(menuName = "GameFramework/Combat/Damage Config", fileName = "CombatDamageConfig")]
    public class CombatDamageConfig : ScriptableObject
    {
        [Header("基础规则")]
        [SerializeField] private float minDamage = 1f;
        [SerializeField] private bool clampRawDamageToNonNegative = true;
        [SerializeField] private bool useArmorReduction = true;

        public float MinDamage => Mathf.Max(0f, minDamage);
        public bool ClampRawDamageToNonNegative => clampRawDamageToNonNegative;
        public bool UseArmorReduction => useArmorReduction;
    }
}
