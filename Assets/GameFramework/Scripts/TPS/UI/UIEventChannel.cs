using System;

namespace GameFramework.TPS.UI
{
    /// <summary>
    /// UI 事件通道（最小版）：
    /// - 业务层发布事件
    /// - UI 表现层订阅事件
    /// </summary>
    public static class UIEventChannel
    {
        public static event Action<string> OnHintShowRequested;
        public static event Action OnHintHideRequested;

        public static void RequestShowHint(string message)
        {
            OnHintShowRequested?.Invoke(message ?? string.Empty);
        }

        public static void RequestHideHint()
        {
            OnHintHideRequested?.Invoke();
        }
    }
}
