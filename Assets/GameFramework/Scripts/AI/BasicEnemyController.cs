using GameFramework.Combat;
using GameFramework.Core;
using GameFramework.Skill;
using GameFramework.Stats;
using UnityEngine;
using UnityEngine.AI;

namespace GameFramework.AI
{
    /// <summary>
    /// 敌人控制器：
    /// - 检测玩家、追击至射程、射线/技能攻击（优先打 ActorStatsComponent）
    /// - 可选 NavMesh 寻路（无 NavMesh 时自动降级为直线移动）
    /// - 可选枪声警戒：听到远处枪声后前往侦查
    /// - 可选技能施放：在技能范围内用 SkillCaster 释放敌人技能
    /// </summary>
    [RequireComponent(typeof(ActorStatsComponent))]
    public class BasicEnemyController : MonoBehaviour
    {
        private enum AnimationDriveMode
        {
            Auto,
            StateName,
            Parameter
        }

        [Header("目标")]
        [SerializeField] private Transform target;
        [SerializeField] private string targetTag = "Player";
        [SerializeField] private float targetRefreshInterval = 0.5f;

        [Header("移动")]
        [SerializeField] private float detectRange = 18f;
        [SerializeField] private float attackRange = 10f;
        [SerializeField] private float stoppingDistance = 2f;
        [Tooltip("移动回退速度：当“使用Status移动速度”关闭时才生效。")]
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float turnSpeed = 8f;
        [Header("速度来源（推荐使用Status）")]
        [Tooltip("开启后，移动速度取 ActorStatsComponent.CurrentAttributes.moveSpeed。")]
        [SerializeField] private bool useStatusMoveSpeed = true;
        [Tooltip("开启后，攻击速度取 ActorStatsComponent.CurrentAttributes.baseAttackSpeed。")]
        [SerializeField] private bool useStatusAttackSpeed = true;
        [Header("位移控制")]
        [SerializeField] private bool useAnimationRootMotionForMovement = false;

        [Header("攻击")]
        [Tooltip("攻击回退速度：当“使用Status攻击速度”关闭时才生效。")]
        [SerializeField] private float attackSpeed = 1f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private Transform firePoint;
        [SerializeField] private LayerMask attackMask = ~0;
        [SerializeField] private bool useCombatLayerDefaults = true;
        [SerializeField] private float attackEventFallbackDuration = 1.1f;

        [Header("NavMesh 寻路")]
        [Tooltip("开启后使用 NavMeshAgent 寻路（需场景已烘焙 NavMesh）；未在 NavMesh 上时自动降级为直线移动。")]
        [SerializeField] private bool useNavMeshAgent = false;

        [Header("枪声警戒")]
        [Tooltip("开启后监听 GameEventBus.OnGunshotAlert，听到范围内枪声会前往侦查。")]
        [SerializeField] private bool respondToGunshots = true;
        [SerializeField] private float hearingRadius = 28f;
        [Tooltip("侦查持续时间：抵达枪声点或超时后回到待机。")]
        [SerializeField] private float investigateDuration = 6f;

        [Header("技能")]
        [Tooltip("开启后在技能范围内优先释放技能（需本对象挂 SkillCaster 并配置 loadout）。")]
        [SerializeField] private bool useSkills = false;
        [SerializeField] private SkillType enemySkillType = SkillType.Active1;
        [SerializeField] private float skillRange = 4f;
        [Tooltip("技能施放后的动作锁定时间（秒）。")]
        [SerializeField] private float skillCastLockDuration = 1.2f;

        [Header("动画（状态名驱动）")]
        [SerializeField] private Animator animator;
        [SerializeField] private AnimationDriveMode animationDriveMode = AnimationDriveMode.Auto;
        [SerializeField] private string idleStateName = "Idle";
        [SerializeField] private string moveStateName = "Walk";
        [SerializeField] private string attackStateName = "Attack_1";
        [SerializeField] private string hitStateName = "Damage";
        [SerializeField] private string deathStateName = "Death";
        [SerializeField] private float crossFadeTime = 0.12f;
        [SerializeField] private float hitReactCooldown = 0.25f;
        [Header("动画（参数驱动）")]
        [SerializeField] private string speedParamName = "Speed";
        [SerializeField] private string moveSpeedParamName = "MoveSpeed";
        [SerializeField] private string attackSpeedParamName = "AttackSpeed";
        [SerializeField] private string walkBoolParamName = "Walk";
        [SerializeField] private string detectTriggerParamName = "Detect";
        [SerializeField] private string attackTriggerAParamName = "Attack";
        [SerializeField] private string attackTriggerBParamName = "Attack_2";
        [SerializeField] private string hitTriggerParamName = "Hit";
        [SerializeField] private string deadBoolParamName = "Dead";
        [SerializeField] private string deadTriggerParamName = "Dead";
        [Header("死亡处理")]
        [SerializeField] private bool disableDamageCollidersOnDeath = true;
        [SerializeField] private float destroyDelayAfterDeath = 2.2f;
        [SerializeField] private bool disableRigidbodyOnDeath = true;

        private ActorStatsComponent _stats;
        private float _nextRefreshTime;
        private float _nextHitReactTime;
        private bool _isDeadAnimated;
        private bool _isDead;
        private bool _isAttackInProgress;
        private float _attackLockUntilTime;
        private int _idleHash;
        private int _moveHash;
        private int _attackHash;
        private int _hitHash;
        private int _deathHash;
        private int _currentStateHash;
        private bool _useParamDrive;
        private int _speedParamHash;
        private int _attackTriggerParamHash;
        private int _hitTriggerParamHash;
        private int _deadBoolParamHash;
        private bool _hasSpeedParam;
        private bool _hasMoveSpeedParam;
        private bool _hasAttackSpeedParam;
        private bool _hasWalkBoolParam;
        private bool _hasDetectTriggerParam;
        private bool _hasAttackTriggerAParam;
        private bool _hasAttackTriggerBParam;
        private bool _hasHitTriggerParam;
        private bool _hasDeadBoolParam;
        private bool _hasDeadTriggerParam;
        private int _moveSpeedParamHash;
        private int _attackSpeedParamHash;
        private int _walkBoolParamHash;
        private int _detectTriggerParamHash;
        private int _attackTriggerAParamHash;
        private int _attackTriggerBParamHash;
        private int _deadTriggerParamHash;
        private Collider[] _allColliders;
        private Rigidbody _rigidbody;
        private bool _hasDetectedTarget;
        private bool _hasWarnedMissingStatsForSpeedSource;
        private NavMeshAgent _agent;
        private SkillCaster _skillCaster;
        private Vector3 _investigatePosition;
        private float _investigateUntil;

        /// <summary>当前是否可通过 NavMeshAgent 移动（已启用且已挂到 NavMesh 上）。</summary>
        private bool UsingAgent => _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;

        private bool IsInvestigating => Time.time < _investigateUntil;

        private void Awake()
        {
            _stats = GetComponent<ActorStatsComponent>();
            WarnIfStatusSpeedSourceUnavailable();
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            if (firePoint == null)
            {
                firePoint = transform;
            }

            if (useCombatLayerDefaults)
            {
                attackMask = CombatLayers.EnemyCombatMask;
            }

            _idleHash = Animator.StringToHash(idleStateName);
            _moveHash = Animator.StringToHash(moveStateName);
            _attackHash = Animator.StringToHash(attackStateName);
            _hitHash = Animator.StringToHash(hitStateName);
            _deathHash = Animator.StringToHash(deathStateName);
            _allColliders = GetComponentsInChildren<Collider>(true);
            _rigidbody = GetComponent<Rigidbody>();
            SetupAnimationDrive();
            SyncRootMotionSetting();
            SetupSkillCaster();
            SetupAgent();

            if (_stats != null)
            {
                _stats.OnDamaged += HandleDamaged;
                _stats.OnDied += HandleDied;
            }

            PlayState(_idleHash, true);
        }

        private void OnEnable()
        {
            if (respondToGunshots)
            {
                GameEventBus.OnGunshotAlert += HandleGunshotAlert;
            }
        }

        private void OnDisable()
        {
            GameEventBus.OnGunshotAlert -= HandleGunshotAlert;
        }

        private void OnValidate()
        {
            SyncRootMotionSetting();
        }

        private void OnAnimatorMove()
        {
            if (!ShouldUseAnimationRootMotion() || _stats == null || _stats.IsDead)
            {
                return;
            }

            Vector3 delta = animator.deltaPosition;
            delta.y = 0f;
            transform.position += delta;

            // 根运动 + NavMesh：把代理内部位置同步到实际位置，保证寻路拐点持续更新。
            if (UsingAgent)
            {
                _agent.nextPosition = transform.position;
            }
        }

        private void OnDestroy()
        {
            if (_stats != null)
            {
                _stats.OnDamaged -= HandleDamaged;
                _stats.OnDied -= HandleDied;
            }
        }

        private void Update()
        {
            if (_stats == null || _isDead || _stats.IsDead)
            {
                return;
            }

            if (_isAttackInProgress && Time.time >= _attackLockUntilTime)
            {
                _isAttackInProgress = false;
            }

            if (target == null || Time.time >= _nextRefreshTime)
            {
                RefreshTarget();
            }

            if (target == null)
            {
                _hasDetectedTarget = false;
                return;
            }

            Vector3 toTarget = target.position - transform.position;
            float distance = toTarget.magnitude;
            if (distance > detectRange)
            {
                _hasDetectedTarget = false;
                // 未发现玩家：若正在侦查枪声，则前往侦查点，否则待机。
                if (IsInvestigating)
                {
                    MoveTowardPosition(_investigatePosition);
                    PlayMove();
                }
                else
                {
                    PlayIdle();
                }
                return;
            }

            if (!_hasDetectedTarget)
            {
                _hasDetectedTarget = true;
                // 发现玩家后停止侦查。
                _investigateUntil = 0f;
                TriggerDetect();
            }

            if (distance > attackRange)
            {
                MoveToward(toTarget, distance);
                // 移动时朝向路径拐点（NavMesh 绕障），无 NavMesh 时朝向玩家。
                FaceToTarget(GetSteerDirection(toTarget));
                PlayMove();
                return;
            }

            // 进入攻击范围：朝向玩家，优先尝试技能，其次普通攻击。
            FaceToTarget(toTarget);
            if (useSkills && TryCastSkill(distance))
            {
                return;
            }

            if (distance < stoppingDistance)
            {
                PlayIdle();
                return;
            }

            TryStartAttack();
        }

        private void RefreshTarget()
        {
            _nextRefreshTime = Time.time + Mathf.Max(0.1f, targetRefreshInterval);
            if (string.IsNullOrEmpty(targetTag))
            {
                return;
            }

            GameObject go = GameObject.FindGameObjectWithTag(targetTag);
            target = go != null ? go.transform : null;
        }

        private void FaceToTarget(Vector3 toTarget)
        {
            Vector3 flat = toTarget;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion targetRot = Quaternion.LookRotation(flat.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        private void MoveToward(Vector3 toTarget, float distance)
        {
            if (distance <= stoppingDistance)
            {
                return;
            }

            if (UsingAgent)
            {
                _agent.speed = GetEffectiveMoveSpeed();
                _agent.stoppingDistance = stoppingDistance;
                if (target != null)
                {
                    _agent.SetDestination(target.position);
                }
                return;
            }

            if (ShouldUseAnimationRootMotion())
            {
                return;
            }

            Vector3 moveDir = toTarget.normalized;
            moveDir.y = 0f;
            transform.position += moveDir * (GetEffectiveMoveSpeed() * Time.deltaTime);
        }

        /// <summary>
        /// 向指定世界坐标移动（用于枪声侦查）。支持 NavMesh 与直线两种方式。
        /// </summary>
        private void MoveTowardPosition(Vector3 worldPosition)
        {
            Vector3 to = worldPosition - transform.position;
            to.y = 0f;
            float dist = to.magnitude;

            FaceToTarget(GetSteerDirection(to));

            // 抵达侦查点：结束侦查。
            if (dist <= Mathf.Max(0.6f, stoppingDistance))
            {
                _investigateUntil = 0f;
                return;
            }

            if (UsingAgent)
            {
                _agent.speed = GetEffectiveMoveSpeed();
                _agent.stoppingDistance = stoppingDistance;
                _agent.SetDestination(worldPosition);
                return;
            }

            if (ShouldUseAnimationRootMotion())
            {
                return;
            }

            transform.position += to.normalized * (GetEffectiveMoveSpeed() * Time.deltaTime);
        }

        private void TryStartAttack()
        {
            if (_isDead || _stats == null || _stats.IsDead || _isAttackInProgress)
            {
                return;
            }

            _isAttackInProgress = true;
            _attackLockUntilTime = Time.time + Mathf.Max(0.15f, attackEventFallbackDuration / Mathf.Max(0.01f, GetEffectiveAttackSpeed()));
            PlayAttack();
        }

        private void ResolveAttackHit()
        {
            if (_isDead || _stats == null || _stats.IsDead || target == null)
            {
                return;
            }

            Vector3 toTarget = target.position - transform.position;
            float distance = toTarget.magnitude;
            if (distance <= 0.01f)
            {
                return;
            }

            Vector3 origin = firePoint != null ? firePoint.position + Vector3.up * 1.1f : transform.position + Vector3.up * 1.1f;
            Vector3 targetPoint = target.position + Vector3.up * 1.0f;
            Vector3 dir = (targetPoint - origin).normalized;
            if (!Physics.Raycast(origin, dir, out RaycastHit hit, distance + 0.7f, attackMask, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            GameObject hitRoot = hit.transform.root.gameObject;
            ActorStatsComponent targetStats = hitRoot.GetComponent<ActorStatsComponent>();
            if (targetStats != null)
            {
                DamageContext context = new DamageContext(gameObject, targetStats.gameObject, attackDamage, DamageSourceType.Weapon);
                CombatDamageManager.ApplyDamage(context);
            }
        }

        private void HandleDamaged(float _)
        {
            if (_isDead || _stats == null || _stats.IsDead || Time.time < _nextHitReactTime)
            {
                return;
            }

            _nextHitReactTime = Time.time + Mathf.Max(0.05f, hitReactCooldown);
            PlayHit();
        }

        private void HandleDied()
        {
            if (_isDeadAnimated)
            {
                return;
            }

            _isDead = true;
            _isDeadAnimated = true;
            _isAttackInProgress = false;
            PlayDeath();
        }

        private void SetupAnimationDrive()
        {
            if (animator == null)
            {
                _useParamDrive = false;
                return;
            }

            _speedParamHash = Animator.StringToHash(speedParamName);
            _moveSpeedParamHash = Animator.StringToHash(moveSpeedParamName);
            _attackSpeedParamHash = Animator.StringToHash(attackSpeedParamName);
            _walkBoolParamHash = Animator.StringToHash(walkBoolParamName);
            _detectTriggerParamHash = Animator.StringToHash(detectTriggerParamName);
            _attackTriggerAParamHash = Animator.StringToHash(attackTriggerAParamName);
            _attackTriggerBParamHash = Animator.StringToHash(attackTriggerBParamName);
            _hitTriggerParamHash = Animator.StringToHash(hitTriggerParamName);
            _deadBoolParamHash = Animator.StringToHash(deadBoolParamName);
            _deadTriggerParamHash = Animator.StringToHash(deadTriggerParamName);

            _hasSpeedParam = HasAnimatorParameter(speedParamName, AnimatorControllerParameterType.Float);
            _hasMoveSpeedParam = HasAnimatorParameter(moveSpeedParamName, AnimatorControllerParameterType.Float);
            _hasAttackSpeedParam = HasAnimatorParameter(attackSpeedParamName, AnimatorControllerParameterType.Float);
            _hasWalkBoolParam = HasAnimatorParameter(walkBoolParamName, AnimatorControllerParameterType.Bool);
            _hasDetectTriggerParam = HasAnimatorParameter(detectTriggerParamName, AnimatorControllerParameterType.Trigger);
            _hasAttackTriggerAParam = HasAnimatorParameter(attackTriggerAParamName, AnimatorControllerParameterType.Trigger);
            _hasAttackTriggerBParam = HasAnimatorParameter(attackTriggerBParamName, AnimatorControllerParameterType.Trigger);
            _hasHitTriggerParam = HasAnimatorParameter(hitTriggerParamName, AnimatorControllerParameterType.Trigger);
            _hasDeadBoolParam = HasAnimatorParameter(deadBoolParamName, AnimatorControllerParameterType.Bool);
            _hasDeadTriggerParam = HasAnimatorParameter(deadTriggerParamName, AnimatorControllerParameterType.Trigger);

            _useParamDrive = animationDriveMode == AnimationDriveMode.Parameter;
            if (animationDriveMode == AnimationDriveMode.Auto)
            {
                _useParamDrive = (_hasWalkBoolParam || _hasSpeedParam) && (_hasAttackTriggerAParam || _hasAttackTriggerBParam);
            }
        }

        private bool HasAnimatorParameter(string paramName, AnimatorControllerParameterType expectedType)
        {
            if (animator == null || string.IsNullOrEmpty(paramName))
            {
                return false;
            }

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter p = parameters[i];
                if (p != null && p.name == paramName && p.type == expectedType)
                {
                    return true;
                }
            }

            return false;
        }

        private void PlayIdle()
        {
            if (_useParamDrive)
            {
                if (_hasSpeedParam)
                {
                    animator.SetFloat(_speedParamHash, 0f);
                }
                if (_hasMoveSpeedParam)
                {
                    animator.SetFloat(_moveSpeedParamHash, 0f);
                }
                if (_hasWalkBoolParam)
                {
                    animator.SetBool(_walkBoolParamHash, false);
                }
                return;
            }

            PlayState(_idleHash);
        }

        private void PlayMove()
        {
            if (_useParamDrive)
            {
                if (_hasSpeedParam)
                {
                    animator.SetFloat(_speedParamHash, 1f);
                }
                if (_hasMoveSpeedParam)
                {
                    animator.SetFloat(_moveSpeedParamHash, GetEffectiveMoveSpeed());
                }
                if (_hasAttackSpeedParam)
                {
                    animator.SetFloat(_attackSpeedParamHash, Mathf.Max(0.01f, GetEffectiveAttackSpeed()));
                }
                if (_hasWalkBoolParam)
                {
                    animator.SetBool(_walkBoolParamHash, true);
                }
                return;
            }

            PlayState(_moveHash);
        }

        private void PlayAttack()
        {
            if (_useParamDrive)
            {
                if (_hasWalkBoolParam)
                {
                    animator.SetBool(_walkBoolParamHash, false);
                }
                if (_hasAttackSpeedParam)
                {
                    animator.SetFloat(_attackSpeedParamHash, Mathf.Max(0.01f, GetEffectiveAttackSpeed()));
                }
                if (_hasAttackTriggerAParam && _hasAttackTriggerBParam)
                {
                    if (Random.value < 0.5f)
                    {
                        animator.SetTrigger(_attackTriggerAParamHash);
                    }
                    else
                    {
                        animator.SetTrigger(_attackTriggerBParamHash);
                    }
                }
                else if (_hasAttackTriggerAParam)
                {
                    animator.SetTrigger(_attackTriggerAParamHash);
                }
                else if (_hasAttackTriggerBParam)
                {
                    animator.SetTrigger(_attackTriggerBParamHash);
                }
                return;
            }

            PlayState(_attackHash, true);
        }

        private void PlayHit()
        {
            if (_useParamDrive)
            {
                if (_hasHitTriggerParam)
                {
                    animator.SetTrigger(_hitTriggerParamHash);
                    return;
                }
            }

            PlayState(_hitHash, true);
        }

        private void PlayDeath()
        {
            if (animator != null)
            {
                if (_hasWalkBoolParam)
                {
                    animator.SetBool(_walkBoolParamHash, false);
                }
                if (_hasSpeedParam)
                {
                    animator.SetFloat(_speedParamHash, 0f);
                }
                if (_hasMoveSpeedParam)
                {
                    animator.SetFloat(_moveSpeedParamHash, 0f);
                }
            }

            if (_useParamDrive)
            {
                if (_hasDeadBoolParam)
                {
                    animator.SetBool(_deadBoolParamHash, true);
                }
                else if (_hasDeadTriggerParam)
                {
                    animator.SetTrigger(_deadTriggerParamHash);
                }
                else if (_hasHitTriggerParam)
                {
                    animator.SetTrigger(_hitTriggerParamHash);
                }
            }
            else
            {
                PlayState(_deathHash, true);
            }

            ApplyDeathCleanupAndDestroy();
        }

        private void ApplyDeathCleanupAndDestroy()
        {
            if (disableDamageCollidersOnDeath && _allColliders != null)
            {
                for (int i = 0; i < _allColliders.Length; i++)
                {
                    Collider c = _allColliders[i];
                    if (c != null)
                    {
                        c.enabled = false;
                    }
                }
            }

            if (disableRigidbodyOnDeath && _rigidbody != null)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.isKinematic = true;
            }

            Destroy(gameObject, Mathf.Max(0f, destroyDelayAfterDeath));
        }

        private bool ShouldUseAnimationRootMotion()
        {
            return useAnimationRootMotionForMovement && animator != null;
        }

        /// <summary>
        /// 计算期望朝向：使用 NavMesh 且有路径时朝向下一个路径拐点（绕开障碍），
        /// 否则朝向传入的回退方向（通常是目标方向）。
        /// </summary>
        private Vector3 GetSteerDirection(Vector3 fallbackDir)
        {
            if (UsingAgent && _agent.hasPath)
            {
                Vector3 steer = _agent.steeringTarget - transform.position;
                steer.y = 0f;
                if (steer.sqrMagnitude > 0.04f)
                {
                    return steer;
                }
            }

            return fallbackDir;
        }

        private void SetupSkillCaster()
        {
            _skillCaster = GetComponent<SkillCaster>();
#if UNITY_EDITOR
            if (useSkills && _skillCaster == null)
            {
                Debug.LogWarning($"{name}: 已启用技能，但未找到 SkillCaster，技能将不会施放。", this);
            }
#endif
        }

        private void SetupAgent()
        {
            if (!useNavMeshAgent)
            {
                return;
            }

            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null)
            {
                _agent = gameObject.AddComponent<NavMeshAgent>();
            }

            _agent.speed = GetEffectiveMoveSpeed();
            _agent.stoppingDistance = stoppingDistance;
            _agent.angularSpeed = 720f;
            _agent.acceleration = 40f;
            // 旋转由 FaceToTarget 手动处理，保证攻击朝向精确。
            _agent.updateRotation = false;
            // 根运动模式：位移交给动画（updatePosition=false），代理只用于寻路方向；
            // 非根运动模式：代理直接驱动位移（updatePosition=true）。
            _agent.updatePosition = !useAnimationRootMotionForMovement;

            // NavMeshAgent 与非 kinematic 刚体会争抢位置，改为 kinematic 让代理独占位移。
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true;
            }
        }

        /// <summary>
        /// 枪声警戒回调：听到范围内枪声且尚未发现玩家时，前往枪声点侦查。
        /// </summary>
        private void HandleGunshotAlert(Vector3 origin)
        {
            if (!respondToGunshots || _isDead || _stats == null || _stats.IsDead)
            {
                return;
            }

            // 已发现玩家时无需侦查（直接追击优先）。
            if (_hasDetectedTarget)
            {
                return;
            }

            if (Vector3.Distance(transform.position, origin) > hearingRadius)
            {
                return;
            }

            _investigatePosition = origin;
            _investigateUntil = Time.time + Mathf.Max(0.5f, investigateDuration);
            TriggerDetect();
        }

        /// <summary>
        /// 在技能范围内尝试施放敌人技能。返回是否成功施放（成功则锁定动作）。
        /// </summary>
        private bool TryCastSkill(float distanceToTarget)
        {
            if (_skillCaster == null || _isAttackInProgress || _isDead || _stats == null || _stats.IsDead)
            {
                return false;
            }

            if (distanceToTarget > skillRange)
            {
                return false;
            }

            // TryCast 内部处理冷却与目标；失败（冷却中/无目标）则回退普通攻击。
            if (!_skillCaster.TryCast(enemySkillType))
            {
                return false;
            }

            _isAttackInProgress = true;
            _attackLockUntilTime = Time.time + Mathf.Max(0.15f, skillCastLockDuration);
            PlayAttack();
            return true;
        }

        private float GetEffectiveMoveSpeed()
        {
            if (useStatusMoveSpeed && _stats != null)
            {
                return Mathf.Max(0.01f, _stats.CurrentAttributes.moveSpeed);
            }

            WarnIfStatusSpeedSourceUnavailable();
            return Mathf.Max(0.01f, moveSpeed);
        }

        private float GetEffectiveAttackSpeed()
        {
            if (useStatusAttackSpeed && _stats != null)
            {
                return Mathf.Max(0.01f, _stats.CurrentAttributes.baseAttackSpeed);
            }

            WarnIfStatusSpeedSourceUnavailable();
            return Mathf.Max(0.01f, attackSpeed);
        }

        private void WarnIfStatusSpeedSourceUnavailable()
        {
#if UNITY_EDITOR
            if (_hasWarnedMissingStatsForSpeedSource)
            {
                return;
            }

            if ((useStatusMoveSpeed || useStatusAttackSpeed) && _stats == null)
            {
                _hasWarnedMissingStatsForSpeedSource = true;
                Debug.LogWarning($"{name}: 已启用Status速度来源，但未找到 ActorStatsComponent，将回退到脚本速度字段。", this);
            }
#endif
        }

        private void SyncRootMotionSetting()
        {
            if (animator == null)
            {
                return;
            }

            // 根运动开关独立于 NavMesh：开启根运动时，位移由动画驱动（避免滑步）。
            animator.applyRootMotion = useAnimationRootMotionForMovement;
        }

        /// <summary>
        /// 动画事件调用：在攻击动画的有效帧触发伤害判定。
        /// </summary>
        public void AnimationEvent_ApplyAttackDamage()
        {
            ResolveAttackHit();
        }

        /// <summary>
        /// 动画事件调用：在攻击动画结束时解锁下一次攻击。
        /// </summary>
        public void AnimationEvent_AttackFinished()
        {
            _isAttackInProgress = false;
        }

        private void TriggerDetect()
        {
            if (_isDead || _stats == null || _stats.IsDead || !_useParamDrive || animator == null)
            {
                return;
            }

            if (_hasDetectTriggerParam)
            {
                animator.SetTrigger(_detectTriggerParamHash);
            }
        }

        private void PlayState(int stateHash, bool force = false)
        {
            if (animator == null || stateHash == 0)
            {
                return;
            }

            if (!force && _currentStateHash == stateHash)
            {
                return;
            }

            animator.CrossFade(stateHash, Mathf.Max(0.01f, crossFadeTime), 0, 0f);
            _currentStateHash = stateHash;
        }
    }
}
