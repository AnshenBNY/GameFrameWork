using UnityEngine;

namespace GameFramework.Combat
{
    public struct HitEffectRequest
    {
        public Vector3 Origin;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
        public Transform HitTransform;
        public HitEffectType EffectType;
        public bool PlaceSparks;
        public bool PlaceBulletHole;
        public Material BulletHoleMaterial;
        public GameObject SparksPrefab;
    }
}
