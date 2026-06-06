using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.TPS.UI
{
    /// <summary>
    /// 轻量提示 UI：负责显示/隐藏一条教学提示文本。
    /// </summary>
    [DisallowMultipleComponent]
    public class GameplayHintUI : MonoBehaviour
    {
        [SerializeField] private Text messageText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeSpeed = 6f;

        private bool _visible;
        private bool _initialized;
        private bool _initFailedLogged;

        private void Awake()
        {
            // Awake 仅保留轻量初始化，组件解析放到 Start 并支持重试。
        }

        private void OnEnable()
        {
            UIManager.RegisterGameplayHintUI(this);
            UIEventChannel.OnHintShowRequested += HandleHintShowRequested;
            UIEventChannel.OnHintHideRequested += HandleHintHideRequested;
        }

        private void OnDisable()
        {
            UIEventChannel.OnHintShowRequested -= HandleHintShowRequested;
            UIEventChannel.OnHintHideRequested -= HandleHintHideRequested;
            UIManager.UnregisterGameplayHintUI(this);
        }

        private void Start()
        {
            if (TryInitializeComponents())
            {
                return;
            }

            StartCoroutine(RetryInitializeComponents());
        }

        private void Update()
        {
            if (!_initialized || canvasGroup == null)
            {
                return;
            }

            float targetAlpha = _visible ? 1f : 0f;
            if (fadeSpeed <= 0f)
            {
                canvasGroup.alpha = targetAlpha;
            }
            else
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
            }
            canvasGroup.blocksRaycasts = _visible;
            canvasGroup.interactable = _visible;
        }

        public void ShowMessage(string message)
        {
            if (!_initialized)
            {
                return;
            }

            if (messageText != null)
            {
                messageText.text = message ?? string.Empty;
            }

            _visible = true;
            gameObject.SetActive(true);
        }

        public void HideMessage()
        {
            if (!_initialized)
            {
                return;
            }

            _visible = false;
        }

        private void HandleHintShowRequested(string message)
        {
            ShowMessage(message);
        }

        private void HandleHintHideRequested()
        {
            HideMessage();
        }

        private void SetVisibleImmediate(bool visible)
        {
            _visible = visible;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.blocksRaycasts = visible;
                canvasGroup.interactable = visible;
            }
        }

        private bool TryInitializeComponents()
        {
            if (messageText == null)
            {
                Transform label = transform.Find("Label");
                if (label != null)
                {
                    messageText = label.GetComponent<Text>();
                }
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (canvasGroup == null)
            {
                return false;
            }

            SetVisibleImmediate(false);
            _initialized = true;
            return true;
        }

        private IEnumerator RetryInitializeComponents()
        {
            const float retryDuration = 2f;
            const float retryInterval = 0.2f;
            float deadline = Time.time + retryDuration;
            while (!_initialized && Time.time < deadline)
            {
                yield return new WaitForSeconds(retryInterval);
                if (TryInitializeComponents())
                {
                    yield break;
                }
            }

            if (!_initialized && !_initFailedLogged)
            {
                _initFailedLogged = true;
                Debug.LogError("GameplayHintUI: initialization failed, missing Label/Text reference.", this);
                enabled = false;
            }
        }
    }
}
