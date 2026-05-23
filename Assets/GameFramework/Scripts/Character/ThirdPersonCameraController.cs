using GameFramework.Combat;
using UnityEngine;

namespace GameFramework.Character
{
    /// <summary>
    /// 越肩视角第三人称相机（挂在 PlayerCamera 上）：
    /// - 鼠标控制 Yaw / Pitch
    /// - 围绕 Player 的 CameraPivot orbiting
    /// - 提供移动基准方向与屏幕中心瞄准方向
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ThirdPersonCameraController : MonoBehaviour
    {
        [Header("目标")]
        [SerializeField] private Transform target;
        [SerializeField] private Transform pivot;

        [Header("鼠标灵敏度")]
        [SerializeField] private float mouseSensitivityX = 2.5f;
        [SerializeField] private float mouseSensitivityY = 2f;

        [Header("相机距离与偏移")]
        [SerializeField] private float distance = 3.8f;
        [SerializeField] private Vector3 shoulderOffset = new Vector3(0.45f, 0.15f, 0f);

        [Header("俯仰限制")]
        [SerializeField] private float minPitch = -30f;
        [SerializeField] private float maxPitch = 55f;
        [SerializeField] private float defaultPitch = 12f;

        [Header("瞄准射线")]
        [Tooltip("从屏幕中心发射的瞄准射线最大距离。")]
        [SerializeField] private float aimRayDistance = 200f;

        [SerializeField] private LayerMask aimMask = ~0;

        [Tooltip("启用后瞄准射线使用 PlayerCombatMask，并额外剔除 Player 层与 owner 自身。")]
        [SerializeField] private bool useCombatLayerDefaults = true;

        [Header("光标")]
        [SerializeField] private bool lockCursorOnStart = true;

        private Camera _camera;
        private float _yaw;
        private float _pitch;
        private int[] _ignoredAimLayers = System.Array.Empty<int>();

        public Vector3 MovementForward { get; private set; } = Vector3.forward;
        public Vector3 MovementRight { get; private set; } = Vector3.right;
        public Vector3 AimForward => MovementForward;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            ApplyDefaultAimMask();
        }

        private void ApplyDefaultAimMask()
        {
            if (!useCombatLayerDefaults)
            {
                return;
            }

            aimMask = CombatLayers.PlayerCombatMask;
            int playerLayer = CombatLayers.Player;
            _ignoredAimLayers = playerLayer >= 0 ? new[] { playerLayer } : System.Array.Empty<int>();
        }

        private void Start()
        {
            _pitch = defaultPitch;

            if (target != null)
            {
                _yaw = target.eulerAngles.y;
            }

            if (lockCursorOnStart)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            UpdateMovementBasis();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            HandleMouseLook();
            UpdateCameraTransform();
            UpdateMovementBasis();
        }

        public void SetTarget(Transform targetTransform, Transform pivotTransform = null)
        {
            target = targetTransform;
            pivot = pivotTransform;

            if (target != null)
            {
                _yaw = target.eulerAngles.y;
            }
        }

        /// <summary>
        /// 从屏幕中心射线获取水平瞄准方向（用于 Root 朝向准心目标点）。
        /// ignoreRoot：通常为 Player 根节点，用于剔除自身 Collider。
        /// </summary>
        public bool TryGetAimDirection(Vector3 origin, Transform ignoreRoot, out Vector3 horizontalDirection)
        {
            horizontalDirection = MovementForward;

            if (_camera == null)
            {
                return false;
            }

            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 aimPoint = ray.origin + ray.direction * aimRayDistance;

            if (CombatRaycastUtility.TryFirstHit(
                    ray,
                    aimRayDistance,
                    aimMask,
                    ignoreRoot,
                    out RaycastHit hit,
                    _ignoredAimLayers))
            {
                aimPoint = hit.point;
            }

            Vector3 toAim = aimPoint - origin;
            toAim.y = 0f;
            if (toAim.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            horizontalDirection = toAim.normalized;
            return true;
        }

        private void HandleMouseLook()
        {
            _yaw += Input.GetAxis("Mouse X") * mouseSensitivityX;
            _pitch -= Input.GetAxis("Mouse Y") * mouseSensitivityY;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        private void UpdateCameraTransform()
        {
            Vector3 pivotPosition = GetPivotPosition();
            Quaternion yawRotation = Quaternion.Euler(0f, _yaw, 0f);
            Quaternion pitchRotation = Quaternion.Euler(_pitch, 0f, 0f);

            Vector3 orbitDirection = yawRotation * pitchRotation * Vector3.back;
            Vector3 shoulder = yawRotation * shoulderOffset;
            transform.position = pivotPosition + shoulder + orbitDirection * distance;
            transform.rotation = Quaternion.LookRotation(pivotPosition - transform.position, Vector3.up);
        }

        private Vector3 GetPivotPosition()
        {
            if (pivot != null)
            {
                return pivot.position;
            }

            return target.position + Vector3.up * 1.6f;
        }

        private void UpdateMovementBasis()
        {
            Quaternion yawRotation = Quaternion.Euler(0f, _yaw, 0f);
            MovementForward = yawRotation * Vector3.forward;
            MovementRight = yawRotation * Vector3.right;
        }
    }
}
