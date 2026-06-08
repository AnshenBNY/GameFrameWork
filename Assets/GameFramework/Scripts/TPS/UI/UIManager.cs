using GameFramework.Core;
using UnityEngine;

namespace GameFramework.TPS.UI
{
    /// <summary>
    /// 最小 UI 管理骨架：
    /// - 统一提供 GameplayHintUI / WeaponUIManager 访问入口
    /// - 通过 RuntimeContext 注入，避免运行时 Find 查找
    /// </summary>
    [DisallowMultipleComponent]
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private GameplayHintUI gameplayHintUI;
        [SerializeField] private WeaponUIManager weaponUIManager;

        private static UIManager _instance;

        public static UIManager Instance => _instance ?? RuntimeContext.Instance?.UIManager;

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
            ResolveReferencesFromHierarchy();
            RuntimeContext.RegisterUiManager(this);
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
            UIManager manager = Instance;
            manager?.ResolveReferencesFromHierarchy();
            return manager != null ? manager.gameplayHintUI : null;
        }

        public static WeaponUIManager GetWeaponUIManager()
        {
            UIManager manager = Instance;
            manager?.ResolveReferencesFromHierarchy();
            return manager != null ? manager.weaponUIManager : null;
        }

        public static void RegisterGameplayHintUI(GameplayHintUI ui)
        {
            if (ui == null)
            {
                return;
            }

            UIManager manager = Instance;
            if (manager != null)
            {
                manager.gameplayHintUI = ui;
            }
        }

        public static void UnregisterGameplayHintUI(GameplayHintUI ui)
        {
            if (ui == null)
            {
                return;
            }

            UIManager manager = Instance;
            if (manager != null && manager.gameplayHintUI == ui)
            {
                manager.gameplayHintUI = null;
            }
        }

        public static void RegisterWeaponUIManager(WeaponUIManager ui)
        {
            if (ui == null)
            {
                return;
            }

            UIManager manager = Instance;
            if (manager != null)
            {
                manager.weaponUIManager = ui;
            }
        }

        public static void UnregisterWeaponUIManager(WeaponUIManager ui)
        {
            if (ui == null)
            {
                return;
            }

            UIManager manager = Instance;
            if (manager != null && manager.weaponUIManager == ui)
            {
                manager.weaponUIManager = null;
            }
        }

        public void ResolveReferencesFromHierarchy()
        {
            if (gameplayHintUI == null)
            {
                gameplayHintUI = GetComponentInChildren<GameplayHintUI>(true);
            }

            if (weaponUIManager == null)
            {
                weaponUIManager = GetComponentInChildren<WeaponUIManager>(true);
            }
        }
    }
}
