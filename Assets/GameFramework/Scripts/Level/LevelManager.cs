using System.Collections.Generic;
using GameFramework.Core;
using UnityEngine;

namespace GameFramework.Level
{
    /// <summary>
    /// 关卡管理器（核心）：
    /// 1. 根据 LevelDefinition 初始化地图、触发器、刷新点。
    /// 2. 负责关卡级事件通信与流程管理。
    /// 3. 维护怪物存活数量，并触发关卡完成。
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private LevelDefinition currentLevel;
        [SerializeField] private Transform runtimeRoot;

        private readonly List<GameObject> _spawnedMonsters = new List<GameObject>();
        private readonly List<GameObject> _spawnedItems = new List<GameObject>();
        private readonly List<GameObject> _runtimeTriggers = new List<GameObject>();

        private GameObject _spawnedMap;
        private int _currentPhaseIndex = -1;
        private int _remainingWavesInPhase = 0;

        public LevelDefinition CurrentLevel => currentLevel;

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

            if (currentLevel.mapPrefab != null)
            {
                _spawnedMap = Instantiate(currentLevel.mapPrefab, root);
            }

            SpawnItems(root);
            SpawnMonsters(root);
            CreateTriggers(root);
            EnterPhase(0);

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

        private void SpawnItems(Transform root)
        {
            foreach (SpawnPointData spawn in currentLevel.itemSpawnPoints)
            {
                if (spawn.prefab == null)
                {
                    continue;
                }

                GameObject go = Instantiate(
                    spawn.prefab,
                    spawn.position,
                    Quaternion.Euler(spawn.eulerAngles),
                    root);
                _spawnedItems.Add(go);
            }
        }

        private void SpawnMonsters(Transform root)
        {
            foreach (SpawnPointData spawn in currentLevel.monsterSpawnPoints)
            {
                if (spawn.prefab == null)
                {
                    continue;
                }

                GameObject go = Instantiate(
                    spawn.prefab,
                    spawn.position,
                    Quaternion.Euler(spawn.eulerAngles),
                    root);

                _spawnedMonsters.Add(go);
                GameEventBus.RaiseMonsterSpawned(go);
            }
        }

        private void CreateTriggers(Transform root)
        {
            foreach (LevelTriggerData triggerData in currentLevel.triggers)
            {
                GameObject triggerGo = new GameObject($"Trigger_{triggerData.triggerId}");
                triggerGo.transform.SetParent(root);
                triggerGo.transform.position = triggerData.position;

                BoxCollider box = triggerGo.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = triggerData.size;

                LevelRuntimeTrigger runtimeTrigger = triggerGo.AddComponent<LevelRuntimeTrigger>();
                runtimeTrigger.Initialize(triggerData.triggerId);

                _runtimeTriggers.Add(triggerGo);
            }
        }

        private void HandleActorDied(GameObject actor)
        {
            if (_spawnedMonsters.Remove(actor))
            {
                if (_spawnedMonsters.Count == 0)
                {
                    TryAdvanceByMonsterClear();
                }
            }
        }

        private void HandleTriggerEntered(string triggerId, GameObject actor)
        {
            if (!IsCurrentPhaseValid())
            {
                return;
            }

            LevelPhaseData phase = currentLevel.phases[_currentPhaseIndex];
            if (phase.phaseType != LevelPhaseType.ReachTrigger)
            {
                return;
            }

            if (!string.IsNullOrEmpty(phase.requiredTriggerId) && phase.requiredTriggerId == triggerId)
            {
                AdvancePhase();
            }
        }

        private void ClearRuntime()
        {
            if (_spawnedMap != null)
            {
                Destroy(_spawnedMap);
                _spawnedMap = null;
            }

            foreach (GameObject go in _spawnedMonsters)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }
            _spawnedMonsters.Clear();

            foreach (GameObject go in _spawnedItems)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }
            _spawnedItems.Clear();

            foreach (GameObject go in _runtimeTriggers)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }
            _runtimeTriggers.Clear();

            _currentPhaseIndex = -1;
            _remainingWavesInPhase = 0;
        }

        private Transform GetRuntimeRoot()
        {
            if (runtimeRoot != null)
            {
                return runtimeRoot;
            }

            GameObject root = GameObject.Find("LevelRuntimeRoot");
            if (root == null)
            {
                root = new GameObject("LevelRuntimeRoot");
            }

            runtimeRoot = root.transform;
            return runtimeRoot;
        }

        private void EnterPhase(int index)
        {
            if (currentLevel == null)
            {
                return;
            }

            if (currentLevel.phases == null || currentLevel.phases.Count == 0)
            {
                // 未配置阶段时，回退到旧逻辑：清怪即通关。
                _currentPhaseIndex = -1;
                return;
            }

            if (index < 0 || index >= currentLevel.phases.Count)
            {
                CompleteLevel();
                return;
            }

            _currentPhaseIndex = index;
            LevelPhaseData phase = currentLevel.phases[_currentPhaseIndex];
            _remainingWavesInPhase = Mathf.Max(1, phase.requiredWaveCount);

            if (phase.phaseType == LevelPhaseType.Complete)
            {
                CompleteLevel();
            }
        }

        private void AdvancePhase()
        {
            EnterPhase(_currentPhaseIndex + 1);
        }

        private void TryAdvanceByMonsterClear()
        {
            if (!IsCurrentPhaseValid())
            {
                // 兼容无阶段配置模式：清怪即通关。
                CompleteLevel();
                return;
            }

            LevelPhaseData phase = currentLevel.phases[_currentPhaseIndex];
            if (phase.phaseType != LevelPhaseType.DefendWaves)
            {
                return;
            }

            _remainingWavesInPhase--;
            if (_remainingWavesInPhase <= 0)
            {
                AdvancePhase();
            }
            else
            {
                // 当前简化策略：复用原刷新点再刷一波。
                SpawnMonsters(GetRuntimeRoot());
            }
        }

        private bool IsCurrentPhaseValid()
        {
            return currentLevel != null
                   && currentLevel.phases != null
                   && _currentPhaseIndex >= 0
                   && _currentPhaseIndex < currentLevel.phases.Count;
        }
    }
}
