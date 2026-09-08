using GameFramework.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.UI
{
    /// <summary>
    /// 关卡结算界面（UGUI，代码构建）：
    /// 监听 <see cref="GameEventBus.OnLevelCompleted"/> / <see cref="GameEventBus.OnLevelFailed"/>，
    /// 弹出全屏半透明遮罩与结果标题。
    /// 保持无按钮的最小实现；后续可在此接入“重试/下一关”交互（需 EventSystem）。
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelResultScreen : MonoBehaviour
    {
        [SerializeField] private Color winColor = new Color(0.3f, 0.9f, 0.4f, 1f);
        [SerializeField] private Color loseColor = new Color(0.95f, 0.35f, 0.25f, 1f);
        [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.6f);

        private GameObject _panel;
        private Text _titleText;
        private Text _subtitleText;
        private Font _font;

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUi();
            HidePanel();
        }

        private void OnEnable()
        {
            GameEventBus.OnLevelCompleted += HandleLevelCompleted;
            GameEventBus.OnLevelFailed += HandleLevelFailed;
        }

        private void OnDisable()
        {
            GameEventBus.OnLevelCompleted -= HandleLevelCompleted;
            GameEventBus.OnLevelFailed -= HandleLevelFailed;
        }

        private void HandleLevelCompleted(string levelId)
        {
            // 内置字体无中文字形，标题使用 ASCII 文案避免缺字。
            ShowResult("MISSION COMPLETE", $"Level Cleared - {levelId}", winColor);
        }

        private void HandleLevelFailed(string levelId)
        {
            ShowResult("MISSION FAILED", $"Level Failed - {levelId}", loseColor);
        }

        private void ShowResult(string title, string subtitle, Color color)
        {
            if (_panel == null)
            {
                return;
            }

            _panel.SetActive(true);
            if (_titleText != null)
            {
                _titleText.text = title;
                _titleText.color = color;
            }

            if (_subtitleText != null)
            {
                _subtitleText.text = subtitle;
            }
        }

        private void HidePanel()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private void BuildUi()
        {
            Canvas canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // 高于普通 HUD

            CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _panel = new GameObject("ResultPanel", typeof(RectTransform));
            _panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRt = _panel.GetComponent<RectTransform>();
            Stretch(panelRt);
            Image overlay = _panel.AddComponent<Image>();
            Texture2D whiteTex = Texture2D.whiteTexture;
            overlay.sprite = Sprite.Create(whiteTex, new Rect(0f, 0f, whiteTex.width, whiteTex.height), new Vector2(0.5f, 0.5f), 100f);
            overlay.color = overlayColor;
            overlay.raycastTarget = false;

            _titleText = CreateText("Title", panelRt, "", 96, TextAnchor.MiddleCenter);
            RectTransform titleRt = _titleText.rectTransform;
            titleRt.anchorMin = new Vector2(0.5f, 0.5f);
            titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.pivot = new Vector2(0.5f, 0.5f);
            titleRt.anchoredPosition = new Vector2(0f, 40f);
            titleRt.sizeDelta = new Vector2(1200f, 160f);
            _titleText.fontStyle = FontStyle.Bold;

            _subtitleText = CreateText("Subtitle", panelRt, "", 32, TextAnchor.MiddleCenter);
            RectTransform subRt = _subtitleText.rectTransform;
            subRt.anchorMin = new Vector2(0.5f, 0.5f);
            subRt.anchorMax = new Vector2(0.5f, 0.5f);
            subRt.pivot = new Vector2(0.5f, 0.5f);
            subRt.anchoredPosition = new Vector2(0f, -60f);
            subRt.sizeDelta = new Vector2(1200f, 60f);
            _subtitleText.color = new Color(0.88f, 0.9f, 0.95f, 1f);
        }

        private Text CreateText(string name, Transform parent, string content, int fontSize, TextAnchor anchor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = _font;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
