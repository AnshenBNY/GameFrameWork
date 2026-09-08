using GameFramework.Core;
using UnityEngine;

namespace GameFramework.Level
{
    /// <summary>
    /// 最小关卡调试 HUD：
    /// 显示当前阶段、怪物数量与关卡状态，便于快速联调。
    /// </summary>
    public class LevelDebugHud : MonoBehaviour
    {
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private Vector2 anchor = new Vector2(12f, 12f);
        [SerializeField] private int width = 430;
        [SerializeField] private int height = 230;

        private string _runtimeStatus = "Idle";
        private GUIStyle _labelStyle;
        private GUIStyle _boxStyle;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void ResolveDependencies()
        {
            if (levelManager != null)
            {
                return;
            }

            levelManager = GetComponent<LevelManager>();
            if (levelManager != null)
            {
                return;
            }

            RuntimeContext context = RuntimeContext.Instance;
            if (context != null)
            {
                context.TryGetLevelManager(out levelManager);
            }
        }

        private void OnEnable()
        {
            GameEventBus.OnLevelStarted += HandleLevelStarted;
            GameEventBus.OnLevelCompleted += HandleLevelCompleted;
            GameEventBus.OnLevelFailed += HandleLevelFailed;
        }

        private void OnDisable()
        {
            GameEventBus.OnLevelStarted -= HandleLevelStarted;
            GameEventBus.OnLevelCompleted -= HandleLevelCompleted;
            GameEventBus.OnLevelFailed -= HandleLevelFailed;
        }

        private void OnGUI()
        {
            EnsureStyles();

            Rect area = new Rect(anchor.x, anchor.y, width, height);
            GUI.Box(area, "Level Debug", _boxStyle);

            string levelId = levelManager != null && levelManager.CurrentLevel != null
                ? levelManager.CurrentLevel.levelId
                : "(none)";

            string phase = "N/A";
            if (levelManager != null && levelManager.HasValidPhase)
            {
                phase = $"{levelManager.CurrentPhaseIndex}: {levelManager.CurrentPhaseId} ({levelManager.CurrentPhaseType})";
            }

            int monsterCount = levelManager != null ? levelManager.ActiveMonsterCount : -1;
            int remainingWaves = levelManager != null ? levelManager.RemainingWavesInPhase : 0;
            string lastDiedActor = levelManager != null ? levelManager.LastDiedActorName : "(n/a)";
            string monstersSnapshot = levelManager != null ? levelManager.GetMonsterDebugSnapshot() : "(n/a)";

            Rect textRect = new Rect(area.x + 12f, area.y + 30f, area.width - 20f, area.height - 20f);
            string content =
                $"<b>Status</b>: {_runtimeStatus}\n" +
                $"<b>Level</b>: {levelId}\n" +
                $"<b>Phase</b>: {phase}\n" +
                $"<b>Monsters Alive</b>: {monsterCount}\n" +
                $"<b>Remaining Waves</b>: {remainingWaves}\n" +
                $"<b>Last Died Actor</b>: {lastDiedActor}\n" +
                $"<b>Monster List</b>: {monstersSnapshot}";
            GUI.Label(textRect, content, _labelStyle);
        }

        private void EnsureStyles()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    richText = true
                };
            }

            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.UpperLeft
                };
            }
        }

        private void HandleLevelStarted(string levelId)
        {
            _runtimeStatus = $"Running ({levelId})";
        }

        private void HandleLevelCompleted(string levelId)
        {
            _runtimeStatus = $"Completed ({levelId})";
        }

        private void HandleLevelFailed(string levelId)
        {
            _runtimeStatus = $"Failed ({levelId})";
        }
    }
}
