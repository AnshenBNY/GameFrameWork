using GameFramework.Skill;
using GameFramework.Stats;
using GameFramework.Weapon;
using UnityEngine;

namespace GameFramework.Character
{
    /// <summary>
    /// 第三人称角色控制器（玩家）：
    /// - 基础移动/重力/跳跃（多段跳）
    /// - 武器射击
    /// - 道具使用
    /// - 技能释放（1/2/Q/F）
    /// 
    /// 输入约定（Unity 旧输入系统）：
    /// - Horizontal / Vertical
    /// - Mouse X / Mouse Y
    /// - Fire1
    /// - Jump
    /// - R（换弹）
    /// - Alpha1 (主动1) / Alpha2 (主动2) / Q (终极) / F (道具)
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(ActorStatsComponent))]
    public class ThirdPersonCharacterController : MonoBehaviour
    {
        [Header("视角与朝向")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float rotationSpeed = 12f;

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

        private void HandleMovement()
        {
            AttributeSet attributes = _stats.CurrentAttributes;

            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            Vector3 input = new Vector3(horizontal, 0f, vertical);
            input = Vector3.ClampMagnitude(input, 1f);

            Vector3 forward = cameraPivot != null ? cameraPivot.forward : transform.forward;
            Vector3 right = cameraPivot != null ? cameraPivot.right : transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDir = (forward * input.z + right * input.x).normalized;
            Vector3 horizontalMove = moveDir * attributes.moveSpeed;

            if (moveDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }

            if (_characterController.isGrounded)
            {
                _velocity.y = -2f;
                ResetJumpState();
            }

            if (Input.GetButtonDown("Jump") && _remainingJumps > 0)
            {
                // v = sqrt(2gh)
                _velocity.y = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * Mathf.Max(0.1f, attributes.jumpHeight));
                _remainingJumps--;
            }

            _velocity.y += Physics.gravity.y * Time.deltaTime;
            Vector3 finalMove = horizontalMove + new Vector3(0f, _velocity.y, 0f);
            _characterController.Move(finalMove * Time.deltaTime);
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
