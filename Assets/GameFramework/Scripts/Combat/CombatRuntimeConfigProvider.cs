using UnityEngine;

namespace GameFramework.Combat
{
    /// <summary>
    /// 运行时注入器：将 ScriptableObject 配置注入 CombatDamageManager。
    /// </summary>
    [DisallowMultipleComponent]
    public class CombatRuntimeConfigProvider : MonoBehaviour
    {
        [SerializeField] private CombatDamageConfig damageConfig;

        private void Awake()
        {
            if (damageConfig != null)
            {
                CombatDamageManager.SetConfig(damageConfig);
            }
        }
    }
}
