using UnityEngine;

namespace GameFramework.Weapon
{
    /// <summary>
    /// 在 Game 视口显示射击射线与命中点（LineRenderer + 命中标记球）。
    /// </summary>
    public class WeaponShotVisualizer : MonoBehaviour
    {
        [Header("开关")]
        [SerializeField] private bool showInGame = true;

        [Header("射线")]
        [SerializeField] private float displayDuration = 0.45f;
        [SerializeField] private float lineWidth = 0.03f;
        [SerializeField] private Color rayColor = new Color(1f, 0.85f, 0.15f, 0.95f);

        [Header("命中点")]
        [SerializeField] private Color hitColor = new Color(1f, 0.25f, 0.25f, 1f);
        [SerializeField] private float hitMarkerDiameter = 0.16f;

        private Material _lineMaterial;
        private Material _hitMaterial;

        /// <summary>
        /// 显示一次射击轨迹。
        /// </summary>
        public void ShowShot(Vector3 origin, Vector3 endPoint, bool hasHit, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!showInGame || !isActiveAndEnabled)
            {
                return;
            }

            CreateRayLine(origin, endPoint);

            if (hasHit)
            {
                CreateHitMarker(hitPoint, hitNormal);
            }
        }

        private void CreateRayLine(Vector3 origin, Vector3 endPoint)
        {
            GameObject lineGo = new GameObject("ShotRay");
            lineGo.transform.SetParent(transform, false);

            LineRenderer line = lineGo.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, origin);
            line.SetPosition(1, endPoint);
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.numCornerVertices = 4;
            line.numCapVertices = 4;
            line.material = GetLineMaterial();
            line.startColor = rayColor;
            line.endColor = rayColor;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            line.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            Destroy(lineGo, displayDuration);
        }

        private void CreateHitMarker(Vector3 hitPoint, Vector3 hitNormal)
        {
            GameObject markerGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            markerGo.name = "ShotHitMarker";
            markerGo.transform.SetParent(transform, false);
            markerGo.transform.position = hitPoint + hitNormal * (hitMarkerDiameter * 0.25f);
            markerGo.transform.localScale = Vector3.one * hitMarkerDiameter;

            Collider collider = markerGo.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            MeshRenderer renderer = markerGo.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetHitMaterial();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            Destroy(markerGo, displayDuration);
        }

        private Material GetLineMaterial()
        {
            if (_lineMaterial == null)
            {
                _lineMaterial = CreateUnlitMaterial(rayColor);
            }

            return _lineMaterial;
        }

        private Material GetHitMaterial()
        {
            if (_hitMaterial == null)
            {
                _hitMaterial = CreateUnlitMaterial(hitColor);
            }

            return _hitMaterial;
        }

        private static Material CreateUnlitMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(shader);
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            return material;
        }
    }
}
