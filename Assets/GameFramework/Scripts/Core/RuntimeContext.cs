using GameFramework.TPS.Player;
using GameFramework.TPS.UI;
using UnityEngine;

namespace GameFramework.Core
{
    /// <summary>
    /// 运行时依赖注入入口：
    /// 集中持有 Player / UI 等跨模块引用，避免业务脚本分散 Find/Tag 查找。
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public class RuntimeContext : MonoBehaviour
    {
        private static RuntimeContext _instance;
        private static UIManager _pendingUiManager;
        private static Transform _pendingPickupHud;

        [Header("场景引用（优先 Inspector 绑定）")]
        [SerializeField] private GameObject player;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private Transform pickupHud;

        [Header("兜底（仅未绑定时使用一次）")]
        [SerializeField] private string playerTag = "Player";

        public static RuntimeContext Instance => _instance;

        public GameObject Player => player;
        public UIManager UIManager => uiManager;
        public Transform PickupHud => pickupHud;
        public WeaponUIManager WeaponUIManager => uiManager != null ? uiManager.WeaponUIManager : null;
        public GameplayHintUI GameplayHintUI => uiManager != null ? uiManager.GameplayHintUI : null;

        public bool IsPlayerReady => player != null;
        public bool IsUiReady => uiManager != null && WeaponUIManager != null;
        public bool IsPickupHudReady => pickupHud != null;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("RuntimeContext: multiple instances found, keeping the first one.", this);
                return;
            }

            _instance = this;

            if (_pendingUiManager != null)
            {
                BindUiManager(_pendingUiManager);
                _pendingUiManager = null;
            }

            if (_pendingPickupHud != null)
            {
                BindPickupHud(_pendingPickupHud);
                _pendingPickupHud = null;
            }

            ResolveReferences();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public static void RegisterUiManager(UIManager manager)
        {
            if (manager == null)
            {
                return;
            }

            if (_instance != null)
            {
                _instance.BindUiManager(manager);
                return;
            }

            _pendingUiManager = manager;
        }

        public static void RegisterPickupHud(Transform pickupHudTransform)
        {
            if (pickupHudTransform == null)
            {
                return;
            }

            if (_instance != null)
            {
                _instance.BindPickupHud(pickupHudTransform);
                return;
            }

            _pendingPickupHud = pickupHudTransform;
        }

        public void BindPlayer(GameObject playerObject)
        {
            player = playerObject;
        }

        public void BindUiManager(UIManager manager)
        {
            uiManager = manager;
            manager?.ResolveReferencesFromHierarchy();
        }

        public void BindPickupHud(Transform pickupHudTransform)
        {
            pickupHud = pickupHudTransform;
        }

        /// <summary>
        /// 解析并缓存场景引用。仅在 Awake 或显式调用时执行，不在 Update 中重复查找。
        /// </summary>
        public void ResolveReferences()
        {
            if (player == null && !string.IsNullOrEmpty(playerTag))
            {
                GameObject tagged = GameObject.FindGameObjectWithTag(playerTag);
                if (tagged != null)
                {
                    player = tagged;
                }
            }

            if (uiManager == null && _pendingUiManager != null)
            {
                BindUiManager(_pendingUiManager);
            }

            if (pickupHud == null && _pendingPickupHud != null)
            {
                BindPickupHud(_pendingPickupHud);
            }
        }

        public bool TryGetPlayer(out GameObject playerObject, out ShootBehaviour shootBehaviour)
        {
            playerObject = player;
            shootBehaviour = playerObject != null ? playerObject.GetComponent<ShootBehaviour>() : null;
            return playerObject != null && shootBehaviour != null;
        }

        public bool TryGetPlayerBasic(out BasicBehaviour basicBehaviour)
        {
            basicBehaviour = player != null ? player.GetComponent<BasicBehaviour>() : null;
            return basicBehaviour != null;
        }
    }
}
