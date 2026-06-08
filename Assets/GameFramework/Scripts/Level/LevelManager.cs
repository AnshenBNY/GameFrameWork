using GameFramework.Core;
using UnityEngine;

namespace GameFramework.Level
{
    /// <summary>
    /// 关卡管理器（编排层）：
    /// 协调 RuntimeRegistry / SpawnService / PhaseFlow 完成关卡生命周期。
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private LevelDefinition currentLevel;
        [SerializeField] private Transform runtimeRoot;

        private readonly LevelRuntimeRegistry _registry = new LevelRuntimeRegistry();
        private readonly LevelSpawnService _spawnService = new LevelSpawnService();
        private readonly LevelPhaseFlow _phaseFlow = new LevelPhaseFlow();

        public LevelDefinition CurrentLevel => currentLevel;
        public int ActiveMonsterCount => _registry.ActiveMonsterCount;
        public int CurrentPhaseIndex => _phaseFlow.CurrentPhaseIndex;
        public int RemainingWavesInPhase => _phaseFlow.RemainingWavesInPhase;
        public bool HasValidPhase => _phaseFlow.IsCurrentPhaseValid(currentLevel);
        public string LastDiedActorName => _registry.LastDiedActorName;

        public string GetMonsterDebugSnapshot() => _registry.GetMonsterDebugSnapshot();

        public LevelPhaseType? CurrentPhaseType => _phaseFlow.GetCurrentPhaseType(currentLevel);

        public string CurrentPhaseId => _phaseFlow.GetCurrentPhaseId(currentLevel);

        private void Awake()
        {
            RuntimeContext.RegisterLevelManager(this);
        }

        private void OnEnable()
        {
            GameEventBus.OnActorDied += HandleActorDied;
            GameEventBus.OnLevelTriggerEntered += HandleTriggerEntered;
        }

        private void OnDisable()
        {
            GameEventBus.OnActorDied -= HandleActorDied;
            GameEventBus.OnLevelTriggerEntered -= HandleTriggerEntered;
        }

        private void Start()
        {
            if (currentLevel != null)
            {
                StartLevel(currentLevel);
            }
        }

        /// <summary>
        /// 启动关卡：清理旧内容，按定义重新构建运行时实体。
        /// </summary>
        public void StartLevel(LevelDefinition definition)
        {
            currentLevel = definition;
            ClearRuntime();

            if (currentLevel == null)
            {
                Debug.LogWarning("LevelManager: currentLevel 为空，无法启动关卡。");
                return;
            }

            Transform root = GetRuntimeRoot();
            _registry.SetSpawnedMap(_spawnService.SpawnMap(currentLevel, root));
            _spawnService.SpawnItems(currentLevel, root, _registry);
            _spawnService.SpawnMonsters(currentLevel, root, _registry);
            _spawnService.CreateTriggers(currentLevel, root, _registry);
            _phaseFlow.EnterPhase(currentLevel, 0, CompleteLevel);

            GameEventBus.RaiseLevelStarted(currentLevel.levelId);
        }

        public void CompleteLevel()
        {
            if (currentLevel == null)
            {
                return;
            }

            GameEventBus.RaiseLevelCompleted(currentLevel.levelId);
        }

        public void FailLevel()
        {
            if (currentLevel == null)
            {
                return;
            }

            GameEventBus.RaiseLevelFailed(currentLevel.levelId);
        }

        private void HandleActorDied(GameObject actor)
        {
            if (!_registry.TryTrackActorDeath(actor))
            {
                return;
            }

            if (_registry.ActiveMonsterCount == 0)
            {
                _phaseFlow.TryAdvanceByMonsterClear(
                    currentLevel,
                    CompleteLevel,
                    () => _phaseFlow.AdvancePhase(currentLevel, CompleteLevel),
                    () => _spawnService.SpawnMonsters(currentLevel, GetRuntimeRoot(), _registry));
            }
        }

        private void HandleTriggerEntered(string triggerId, GameObject actor)
        {
            _phaseFlow.TryHandleTrigger(
                currentLevel,
                triggerId,
                () => _phaseFlow.AdvancePhase(currentLevel, CompleteLevel));
        }

        private void ClearRuntime()
        {
            _registry.ClearAll();
            _phaseFlow.Reset();
        }

        private Transform GetRuntimeRoot()
        {
            if (runtimeRoot != null)
            {
                return runtimeRoot;
            }

            Transform existing = transform.Find("LevelRuntimeRoot");
            if (existing != null)
            {
                runtimeRoot = existing;
                return runtimeRoot;
            }

            GameObject root = new GameObject("LevelRuntimeRoot");
            root.transform.SetParent(transform, false);
            runtimeRoot = root.transform;
            return runtimeRoot;
        }
    }
}
