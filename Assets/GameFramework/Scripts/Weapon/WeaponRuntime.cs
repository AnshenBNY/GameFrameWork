using GameFramework.Combat;
using GameFramework.Stats;
using UnityEngine;

namespace GameFramework.Weapon
{
    /// <summary>
    /// 武器运行时组件：
    /// 1. 持有当前弹药状态。
    /// 2. 处理开火频率与弹道检测。
    /// 3. 将命中信息转换为统一 DamageContext。
    /// </summary>
    public class WeaponRuntime : MonoBehaviour
    {
        [SerializeField] private WeaponDefinition definition;
        [SerializeField] private Transform firePoint;
        [SerializeField] private LayerMask hitMask = ~0;

        private float _nextFireTime;
        private int _currentAmmo;
        private int _reserveAmmo;
        private bool _isReloading;
        private float _reloadEndTime;

        public WeaponDefinition Definition => definition;
        public int CurrentAmmo => _currentAmmo;
        public int ReserveAmmo => _reserveAmmo;
        public bool IsReloading => _isReloading;

        private void Awake()
        {
            if (definition != null)
            {
                _currentAmmo = definition.clipSize;
                _reserveAmmo = Mathf.Max(0, definition.initialReserveAmmo);
            }
        }

        private void Update()
        {
            TickReload();
        }

        public bool CanFire()
        {
            return definition != null
                   && Time.time >= _nextFireTime
                   && !_isReloading
                   && _currentAmmo > 0
                   && firePoint != null;
        }

        /// <summary>
        /// 尝试开火。成功返回 true。
        /// </summary>
        public bool TryFire(GameObject owner)
        {
            if (!CanFire())
            {
                return false;
            }

            _nextFireTime = Time.time + Mathf.Max(0.01f, definition.fireInterval);
            _currentAmmo--;

            int pellets = Mathf.Max(1, definition.pelletsPerShot);
            for (int i = 0; i < pellets; i++)
            {
                FireSinglePellet(owner);
            }

            // 打空弹匣时自动触发换弹，提升手感。
            if (_currentAmmo <= 0)
            {
                StartReload();
            }

            return true;
        }

        public void ReloadFull()
        {
            if (definition == null)
            {
                return;
            }

            _currentAmmo = definition.clipSize;
        }

        /// <summary>
        /// 开始换弹。返回 true 代表换弹已启动。
        /// </summary>
        public bool StartReload()
        {
            if (definition == null || _isReloading)
            {
                return false;
            }

            if (_currentAmmo >= definition.clipSize)
            {
                return false;
            }

            if (!definition.infiniteReserveAmmo && _reserveAmmo <= 0)
            {
                return false;
            }

            _isReloading = true;
            _reloadEndTime = Time.time + Mathf.Max(0.05f, definition.reloadDuration);
            return true;
        }

        /// <summary>
        /// 给后备弹药增加数量（拾取弹药包时调用）。
        /// </summary>
        public void AddReserveAmmo(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _reserveAmmo += amount;
        }

        private void TickReload()
        {
            if (!_isReloading || Time.time < _reloadEndTime || definition == null)
            {
                return;
            }

            int need = Mathf.Max(0, definition.clipSize - _currentAmmo);
            if (need <= 0)
            {
                _isReloading = false;
                return;
            }

            if (definition.infiniteReserveAmmo)
            {
                _currentAmmo += need;
            }
            else
            {
                int load = Mathf.Min(need, _reserveAmmo);
                _currentAmmo += load;
                _reserveAmmo -= load;
            }

            _isReloading = false;
        }

        private void FireSinglePellet(GameObject owner)
        {
            Vector3 direction = GetSpreadDirection(firePoint.forward, definition.spreadAngle);
            Ray ray = new Ray(firePoint.position, direction);

            if (Physics.Raycast(ray, out RaycastHit hit, definition.range, hitMask, QueryTriggerInteraction.Ignore))
            {
                ActorStatsComponent targetStats = hit.collider.GetComponentInParent<ActorStatsComponent>();
                if (targetStats != null)
                {
                    if (!GameFramework.Combat.CombatTargetingUtility.CanAffect(
                            owner,
                            targetStats.gameObject,
                            allowSelf: false,
                            allowFriendly: false,
                            allowHostile: true))
                    {
                        return;
                    }

                    DamageContext context = new DamageContext(owner, targetStats.gameObject, definition.pelletDamage, DamageSourceType.Weapon);
                    targetStats.ApplyDamage(context);
                }
            }

            if (definition.debugDrawRay)
            {
                Debug.DrawRay(ray.origin, ray.direction * definition.range, Color.red, 0.5f);
            }
        }

        private static Vector3 GetSpreadDirection(Vector3 forward, float spreadAngle)
        {
            if (spreadAngle <= 0f)
            {
                return forward.normalized;
            }

            float yaw = Random.Range(-spreadAngle, spreadAngle);
            float pitch = Random.Range(-spreadAngle, spreadAngle);
            Quaternion spread = Quaternion.Euler(pitch, yaw, 0f);
            return (spread * forward).normalized;
        }
    }
}
