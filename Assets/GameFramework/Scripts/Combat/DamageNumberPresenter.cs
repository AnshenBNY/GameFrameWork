using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Combat
{
    /// <summary>
    /// 最小伤害飘字展示（调试版，OnGUI）。
    /// 订阅 CombatDamageManager.OnDamageResolved 实时显示伤害。
    /// </summary>
    [DisallowMultipleComponent]
    public class DamageNumberPresenter : MonoBehaviour
    {
        [SerializeField] private bool enablePresenter = true;
        [SerializeField] private float lifetime = 0.75f;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.8f, 0f);
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color killColor = new Color(1f, 0.35f, 0.2f, 1f);

        private readonly List<DamagePopup> _activePopups = new List<DamagePopup>();
        private GUIStyle _style;
        private Camera _camera;

        private void OnEnable()
        {
            CombatDamageManager.OnDamageResolved += HandleDamageResolved;
        }

        private void OnDisable()
        {
            CombatDamageManager.OnDamageResolved -= HandleDamageResolved;
            _activePopups.Clear();
        }

        private void Update()
        {
            if (!enablePresenter)
            {
                return;
            }

            if (_camera == null || !_camera.isActiveAndEnabled)
            {
                _camera = Camera.main;
            }

            float dt = Time.deltaTime;
            for (int i = _activePopups.Count - 1; i >= 0; i--)
            {
                DamagePopup popup = _activePopups[i];
                popup.Age += dt;
                popup.WorldPosition += Vector3.up * (0.8f * dt);
                if (popup.Age >= popup.Lifetime || popup.Target == null)
                {
                    _activePopups.RemoveAt(i);
                    continue;
                }

                _activePopups[i] = popup;
            }
        }

        private void OnGUI()
        {
            if (!enablePresenter || _camera == null || _activePopups.Count == 0)
            {
                return;
            }

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 20,
                    fontStyle = FontStyle.Bold
                };
            }

            for (int i = 0; i < _activePopups.Count; i++)
            {
                DamagePopup popup = _activePopups[i];
                Vector3 screen = _camera.WorldToScreenPoint(popup.WorldPosition);
                if (screen.z <= 0f)
                {
                    continue;
                }

                float alpha = Mathf.Clamp01(1f - (popup.Age / popup.Lifetime));
                Color color = popup.Killed ? killColor : normalColor;
                color.a *= alpha;
                _style.normal.textColor = color;

                Rect rect = new Rect(screen.x - 40f, Screen.height - screen.y - 16f, 80f, 24f);
                GUI.Label(rect, popup.Text, _style);
            }
        }

        private void HandleDamageResolved(DamageContext context, float dealt, bool killed)
        {
            if (!enablePresenter || dealt <= 0f || context.Target == null)
            {
                return;
            }

            _activePopups.Add(new DamagePopup
            {
                Target = context.Target.transform,
                WorldPosition = context.Target.transform.position + worldOffset,
                Text = Mathf.RoundToInt(dealt).ToString(),
                Lifetime = Mathf.Max(0.1f, lifetime),
                Age = 0f,
                Killed = killed
            });
        }

        private struct DamagePopup
        {
            public Transform Target;
            public Vector3 WorldPosition;
            public string Text;
            public float Lifetime;
            public float Age;
            public bool Killed;
        }
    }
}
