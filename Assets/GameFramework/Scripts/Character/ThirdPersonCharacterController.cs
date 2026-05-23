using GameFramework.Skill;
using GameFramework.Stats;
using GameFramework.Weapon;
using UnityEngine;

namespace GameFramework.Character
{
    /// <summary>
    /// 第三人称角色控制器（玩家）：
    /// - Player 根节点：只负责位移与碰撞，不参与朝向
    /// - Root 节点：负责模型/武器朝向（约定 Root 的 +Z 为正面）
    /// - 移动方向始终相对相机，不受 Root 旋转影响
    /// - 射击时 Root 朝向屏幕准心目标；非射击移动时 Root 朝向移动方向
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(ActorStatsComponent))]
    public class ThirdPersonCharacterController : MonoBehaviour
    {
        [Header("层级引用")]
        [Tooltip("专门用于模型旋转的 Root 节点（Player 预制体中的 Root）。")]
        [SerializeField] private Transform characterRoot;

        [SerializeField] private ThirdPersonCameraController cameraController;

        [Header("移动与朝向")]
        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private float aimRotationSpeed = 18f;

        [Header("战斗组件")]
        [SerializeField] private WeaponRuntime weaponRuntime;
        [SerializeField] private SkillCaster skillCaster;
        [SerializeField] private ItemDefinition quickUseItem;

        private CharacterController _characterController;
        private ActorStatsComponent _stats;
        private Vector3 _velocity;
        private int _remainingJumps;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _stats = GetComponent<ActorStatsComponent>();

            if (characterRoot == null)
            {
                Transform root = transform.Find("Root");
                characterRoot = root != null ? root : transform;
            }

            if (cameraController == null)
            {
                cameraController = FindObjectOfType<ThirdPersonCameraController>();
            }

            ResetJumpState();
        }

        private void Update()
        {
            if (_stats.IsDead)
            {
                return;
            }

            HandleMovement();
            HandleFire();
            HandleSkillCast();
            HandleItemUse();
        }

        public void SetCameraController(ThirdPersonCameraController controller)
        {
            cameraController = controller;
        }

        public void SetCharacterRoot(Transform root)
        {
            characterRoot = root;
        }

        private void HandleMovement()
        {
            AttributeSet attributes = _stats.CurrentAttributes;

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 input = new Vector3(horizontal, 0f, vertical);
            input = Vector3.ClampMagnitude(input, 1f);

            // 位移基准：相机水平方向。与 Root 当前朝向无关。
            Vector3 forward = cameraController != null ? cameraController.MovementForward : transform.forward;
            Vector3 right = cameraController != null ? cameraController.MovementRight : transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDir = forward * input.z + right * input.x;
            if (moveDir.sqrMagnitude > 0.0001f)
            {
                moveDir.Normalize();
            }

            Vector3 horizontalMove = moveDir * attributes.moveSpeed;
            HandleRootRotation(moveDir);

            if (_characterController.isGrounded)
            {
                _velocity.y = -2f;
                ResetJumpState();
            }

            if (Input.GetButtonDown("Jump") && _remainingJumps > 0)
            {
                _velocity.y = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * Mathf.Max(0.1f, attributes.jumpHeight));
                _remainingJumps--;
            }

            _velocity.y += Physics.gravity.y * Time.deltaTime;
            Vector3 finalMove = horizontalMove + new Vector3(0f, _velocity.y, 0f);
            _characterController.Move(finalMove * Time.deltaTime);
        }

        /// <summary>
        /// Root 朝向规则：
        /// 1. 按住射击 -> 朝向准心射线落点（可边移动边瞄准）
        /// 2. 否则，有移动输入 -> 朝向移动方向
        /// </summary>
        private void HandleRootRotation(Vector3 moveDir)
        {
            Transform rotTarget = characterRoot != null ? characterRoot : transform;
            Vector3 rotOrigin = rotTarget.position;

            if (Input.GetButton("Fire1") && cameraController != null)
            {
                if (cameraController.TryGetAimDirection(rotOrigin, transform, out Vector3 aimDir))
                {
                    RotateTransform(rotTarget, aimDir, aimRotationSpeed);
                    return;
                }
            }

            if (moveDir.sqrMagnitude > 0.0001f)
            {
                RotateTransform(rotTarget, moveDir, rotationSpeed);
            }
        }

        /// <summary>
        /// 旋转 Root，使局部 +Z 轴（forward）对齐目标方向。
        /// </summary>
        private static void RotateTransform(Transform target, Vector3 direction, float speed)
        {
            CharacterFacingUtility.RotateToward(target, direction, speed);
        }

        private void HandleFire()
        {
            if (weaponRuntime == null)
            {
                return;
            }

            if (Input.GetButton("Fire1"))
            {
                weaponRuntime.TryFire(gameObject);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                weaponRuntime.StartReload();
            }
        }

        private void HandleSkillCast()
        {
            if (skillCaster == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                skillCaster.TryCast(SkillType.Active1);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                skillCaster.TryCast(SkillType.Active2);
            }
            if (Input.GetKeyDown(KeyCode.Q))
            {
                skillCaster.TryCast(SkillType.Ultimate);
            }
        }

        private void HandleItemUse()
        {
            if (quickUseItem == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                _stats.Heal(quickUseItem.healAmount);
            }
        }

        private void ResetJumpState()
        {
            _remainingJumps = Mathf.Max(1, _stats.CurrentAttributes.jumpCount);
        }
    }
}
