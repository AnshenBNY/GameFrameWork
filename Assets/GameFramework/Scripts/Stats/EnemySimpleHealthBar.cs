using UnityEngine;

namespace GameFramework.Stats
{
    /// <summary>
    /// 敌人头顶简易血条（OnGUI 实现，无需额外 UI 预制体）。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ActorStatsComponent))]
    public class EnemySimpleHealthBar : MonoBehaviour
    {
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);
        [SerializeField] private Vector2 size = new Vector2(72f, 8f);
        [SerializeField] private bool showWhenFull = true;
        [SerializeField] private float maxDisplayDistance = 45f;
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.65f);
        [SerializeField] private Color fillColor = new Color(0.18f, 0.95f, 0.2f, 1f);
        [SerializeField] private Color lowHealthColor = new Color(1f, 0.32f, 0.2f, 1f);

        private ActorStatsComponent _stats;
        private Camera _cachedCamera;

        private void Awake()
        {
            _stats = GetComponent<ActorStatsComponent>();
        }

        private void LateUpdate()
        {
            if (_cachedCamera == null || !_cachedCamera.isActiveAndEnabled)
            {
                _cachedCamera = Camera.main;
            }
        }

        private void OnGUI()
        {
            if (_stats == null || _cachedCamera == null)
            {
                return;
            }

            float maxHealth = Mathf.Max(1f, _stats.CurrentAttributes.MaxHealth);
            float currentHealth = Mathf.Clamp(_stats.CurrentHealth, 0f, maxHealth);
            float ratio = currentHealth / maxHealth;

            if (!showWhenFull && ratio >= 0.999f)
            {
                return;
            }

            Vector3 worldPos = transform.position + worldOffset;
            Vector3 screen = _cachedCamera.WorldToScreenPoint(worldPos);
            if (screen.z <= 0f)
            {
                return;
            }

            if (Vector3.Distance(_cachedCamera.transform.position, worldPos) > maxDisplayDistance)
            {
                return;
            }

            float x = screen.x - size.x * 0.5f;
            float y = Screen.height - screen.y - size.y * 0.5f;
            Rect bgRect = new Rect(x, y, size.x, size.y);
            Rect fillRect = new Rect(x + 1f, y + 1f, Mathf.Max(0f, (size.x - 2f) * ratio), size.y - 2f);

            Color oldColor = GUI.color;
            GUI.color = backgroundColor;
            GUI.DrawTexture(bgRect, Texture2D.whiteTexture);

            GUI.color = ratio <= 0.25f ? lowHealthColor : fillColor;
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            GUI.color = oldColor;
        }
    }
}
