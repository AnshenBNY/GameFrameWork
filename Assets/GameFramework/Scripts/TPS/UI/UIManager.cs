using UnityEngine;

namespace GameFramework.TPS.UI
{
    /// <summary>
    /// 最小 UI 管理骨架：
    /// - 统一提供 GameplayHintUI / WeaponUIManager 访问入口
    /// - 允许业务侧通过静态方法查找，减少硬编码对象名依赖
    /// </summary>
    [DisallowMultipleComponent]
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private GameplayHintUI gameplayHintUI;
        [SerializeField] private WeaponUIManager weaponUIManager;

        private static UIManager _instance;

        public static UIManager Instance => _instance;

        public GameplayHintUI GameplayHintUI => gameplayHintUI;
        public WeaponUIManager WeaponUIManager => weaponUIManager;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("UIManager: multiple instances found, keeping the first one.", this);
                return;
            }

            _instance = this;
            ResolveReferences();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public static GameplayHintUI GetGameplayHintUI()
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIManager>();
            }

            if (_instance == null)
            {
                return FindObjectOfType<GameplayHintUI>();
            }

            _instance.ResolveReferences();
            return _instance.gameplayHintUI;
        }

        public static WeaponUIManager GetWeaponUIManager()
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIManager>();
            }

            if (_instance == null)
            {
                return FindObjectOfType<WeaponUIManager>();
            }

            _instance.ResolveReferences();
            return _instance.weaponUIManager;
        }

        public static void RegisterGameplayHintUI(GameplayHintUI ui)
        {
            if (ui == null)
            {
                return;
            }

            EnsureInstance();
            if (_instance != null)
            {
                _instance.gameplayHintUI = ui;
            }
        }

        public static void UnregisterGameplayHintUI(GameplayHintUI ui)
        {
            if (ui == null)
            {
                return;
            }

            EnsureInstance();
            if (_instance != null && _instance.gameplayHintUI == ui)
            {
                _instance.gameplayHintUI = null;
            }
        }

        public static void RegisterWeaponUIManager(WeaponUIManager ui)
        {
            if (ui == null)
            {
                return;
            }

            EnsureInstance();
            if (_instance != null)
            {
                _instance.weaponUIManager = ui;
            }
        }

        public static void UnregisterWeaponUIManager(WeaponUIManager ui)
        {
            if (ui == null)
            {
                return;
            }

            EnsureInstance();
            if (_instance != null && _instance.weaponUIManager == ui)
            {
                _instance.weaponUIManager = null;
            }
        }

        private static void EnsureInstance()
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIManager>();
            }
        }

        private void ResolveReferences()
        {
            if (gameplayHintUI == null)
            {
                gameplayHintUI = FindObjectOfType<GameplayHintUI>();
            }

            if (weaponUIManager == null)
            {
                weaponUIManager = FindObjectOfType<WeaponUIManager>();
            }
        }
    }
}
