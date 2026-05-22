using GameFramework.Skill;
using GameFramework.Stats;
using GameFramework.Weapon;
using UnityEngine;

namespace GameFramework.AI
{
    /// <summary>
    /// 怪物基础 AI（示例骨架）：
    /// - 朝玩家移动
    /// - 到达射程后攻击
    /// - 冷却好时释放技能
    /// </summary>
    [RequireComponent(typeof(ActorStatsComponent))]
    public class MonsterBrain : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private WeaponRuntime weaponRuntime;
        [SerializeField] private SkillCaster skillCaster;
        [SerializeField] private float attackDistance = 15f;
        [SerializeField] private float stopDistance = 2f;

        private ActorStatsComponent _stats;

        private void Awake()
        {
            _stats = GetComponent<ActorStatsComponent>();
        }

        private void Update()
        {
            if (_stats.IsDead || target == null)
            {
                return;
            }

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            if (distance > stopDistance)
            {
                Vector3 moveDir = toTarget.normalized;
                transform.position += moveDir * _stats.CurrentAttributes.moveSpeed * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(moveDir, Vector3.up),
                    Time.deltaTime * 8f);
            }

            if (distance <= attackDistance && weaponRuntime != null)
            {
                weaponRuntime.TryFire(gameObject);
            }

            if (distance <= attackDistance && skillCaster != null)
            {
                // 简化策略：优先终极，失败则主动1。
                if (!skillCaster.TryCast(SkillType.Ultimate))
                {
                    skillCaster.TryCast(SkillType.Active1);
                }
            }
        }
    }
}
