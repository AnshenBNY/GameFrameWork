using GameFramework.Combat;
using GameFramework.Stats;
using UnityEngine;

namespace GameFramework.AI
{
    /// <summary>
    /// 最小敌人控制器：
    /// - 检测玩家
    /// - 追击至射程
    /// - 射线攻击（优先打 ActorStatsComponent）
    /// </summary>
    [RequireComponent(typeof(ActorStatsComponent))]
    public class BasicEnemyController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private string targetTag = "Player";
        [SerializeField] private float targetRefreshInterval = 0.5f;

        [Header("Move")]
        [SerializeField] private float detectRange = 18f;
        [SerializeField] private float attackRange = 10f;
        [SerializeField] private float stoppingDistance = 2f;
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float turnSpeed = 8f;

        [Header("Attack")]
        [SerializeField] private float attackInterval = 0.8f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private Transform firePoint;
        [SerializeField] private LayerMask attackMask = ~0;
        [SerializeField] private bool useCombatLayerDefaults = true;

        private ActorStatsComponent _stats;
        private float _nextAttackTime;
        private float _nextRefreshTime;

        private void Awake()
        {
            _stats = GetComponent<ActorStatsComponent>();
            if (firePoint == null)
            {
                firePoint = transform;
            }

            if (useCombatLayerDefaults)
            {
                attackMask = CombatLayers.EnemyCombatMask;
            }
        }

        private void Update()
        {
            if (_stats == null || _stats.IsDead)
            {
                return;
            }

            if (target == null || Time.time >= _nextRefreshTime)
            {
                RefreshTarget();
            }

            if (target == null)
            {
                return;
            }

            Vector3 toTarget = target.position - transform.position;
            float distance = toTarget.magnitude;
            if (distance > detectRange)
            {
                return;
            }

            FaceToTarget(toTarget);

            if (distance > attackRange)
            {
                MoveToward(toTarget, distance);
                return;
            }

            if (distance < stoppingDistance)
            {
                return;
            }

            TryAttack(distance);
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

            Vector3 moveDir = toTarget.normalized;
            moveDir.y = 0f;
            transform.position += moveDir * (moveSpeed * Time.deltaTime);
        }

        private void TryAttack(float distance)
        {
            if (Time.time < _nextAttackTime)
            {
                return;
            }

            _nextAttackTime = Time.time + Mathf.Max(0.05f, attackInterval);

            Vector3 origin = firePoint != null ? firePoint.position + Vector3.up * 1.1f : transform.position + Vector3.up * 1.1f;
            Vector3 targetPoint = target.position + Vector3.up * 1.0f;
            Vector3 dir = (targetPoint - origin).normalized;

            if (!Physics.Raycast(origin, dir, out RaycastHit hit, distance + 0.5f, attackMask, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            GameObject hitRoot = hit.transform.root.gameObject;
            ActorStatsComponent targetStats = hitRoot.GetComponent<ActorStatsComponent>();
            if (targetStats != null)
            {
                DamageContext context = new DamageContext(gameObject, targetStats.gameObject, attackDamage, DamageSourceType.Weapon);
                targetStats.ApplyDamage(context);
            }
        }
    }
}
