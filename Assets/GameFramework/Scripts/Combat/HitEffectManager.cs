using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Combat
{
    /// <summary>
    /// 命中特效管理器：统一处理火花/弹孔的生成与复用。
    /// </summary>
    public class HitEffectManager : MonoBehaviour
    {
        [System.Serializable]
        private struct HitEffectRule
        {
            public HitEffectType effectType;
            public bool allowSparks;
            public bool allowBulletHole;
        }

        [SerializeField] private int maxBulletHoles = 50;
        [SerializeField] private float bulletHoleScale = 0.07f;
        [SerializeField] private HitEffectRule[] effectRules =
        {
            new HitEffectRule { effectType = HitEffectType.Environment, allowSparks = true, allowBulletHole = true },
            new HitEffectRule { effectType = HitEffectType.DamageableHostile, allowSparks = false, allowBulletHole = false },
            new HitEffectRule { effectType = HitEffectType.DamageableFriendly, allowSparks = false, allowBulletHole = false },
            new HitEffectRule { effectType = HitEffectType.DamageableNeutral, allowSparks = false, allowBulletHole = false },
            new HitEffectRule { effectType = HitEffectType.Unknown, allowSparks = true, allowBulletHole = true }
        };

        private readonly List<GameObject> _bulletHoles = new List<GameObject>();
        private int _bulletHoleSlot;
        private static HitEffectManager _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                return;
            }

            _instance = this;
        }

        public static void EnsureInstance()
        {
            if (_instance != null)
            {
                return;
            }

            GameObject go = new GameObject("HitEffectManager");
            _instance = go.AddComponent<HitEffectManager>();
        }

        public static void HandleHit(in HitEffectRequest request)
        {
            if (_instance == null)
            {
                EnsureInstance();
            }

            _instance?.HandleHitInternal(request);
        }

        private void HandleHitInternal(in HitEffectRequest request)
        {
            bool allowSparks;
            bool allowBulletHole;
            ResolveRuleFlags(request.EffectType, out allowSparks, out allowBulletHole);
            bool placeSparks = request.PlaceSparks && allowSparks;
            bool placeBulletHole = request.PlaceBulletHole && allowBulletHole;

            if (placeSparks && request.SparksPrefab != null)
            {
                GameObject instantSparks = Instantiate(request.SparksPrefab);
                instantSparks.SetActive(true);
                instantSparks.transform.position = request.HitPoint;
                instantSparks.transform.parent = request.SparksPrefab.transform.parent;
            }

            if (!placeBulletHole)
            {
                return;
            }

            Quaternion hitRotation = Quaternion.FromToRotation(Vector3.back, request.HitNormal);
            GameObject bullet = GetOrCreateBulletHole(request.BulletHoleMaterial);
            if (bullet == null)
            {
                return;
            }

            bullet.transform.position = request.HitPoint + 0.01f * request.HitNormal;
            bullet.transform.rotation = hitRotation;
            bullet.transform.SetParent(request.HitTransform);
        }

        private void ResolveRuleFlags(HitEffectType effectType, out bool allowSparks, out bool allowBulletHole)
        {
            if (effectRules != null)
            {
                for (int i = 0; i < effectRules.Length; i++)
                {
                    if (effectRules[i].effectType == effectType)
                    {
                        allowSparks = effectRules[i].allowSparks;
                        allowBulletHole = effectRules[i].allowBulletHole;
                        return;
                    }
                }
            }

            allowSparks = true;
            allowBulletHole = true;
        }

        private GameObject GetOrCreateBulletHole(Material bulletHoleMaterial)
        {
            int safeMax = Mathf.Max(1, maxBulletHoles);
            GameObject bullet;
            if (_bulletHoles.Count < safeMax)
            {
                bullet = CreateBulletHoleObject(bulletHoleMaterial);
                _bulletHoles.Add(bullet);
            }
            else
            {
                bullet = _bulletHoles[_bulletHoleSlot];
                if (bullet == null)
                {
                    bullet = CreateBulletHoleObject(bulletHoleMaterial);
                    _bulletHoles[_bulletHoleSlot] = bullet;
                }
                _bulletHoleSlot = (_bulletHoleSlot + 1) % safeMax;
            }

            return bullet;
        }

        private GameObject CreateBulletHoleObject(Material bulletHoleMaterial)
        {
            GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Quad);
            MeshRenderer renderer = bullet.GetComponent<MeshRenderer>();
            if (renderer != null && bulletHoleMaterial != null)
            {
                renderer.material = bulletHoleMaterial;
            }

            Collider col = bullet.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }

            bullet.transform.localScale = Vector3.one * bulletHoleScale;
            bullet.name = "BulletHole";
            return bullet;
        }
    }
}
