using System;
using UnityEngine;

namespace GameFramework.Level
{
    /// <summary>
    /// 关卡阶段状态机：触发到达、防守波次、结算推进。
    /// </summary>
    public class LevelPhaseFlow
    {
        private int _currentPhaseIndex = -1;
        private int _remainingWavesInPhase;

        public int CurrentPhaseIndex => _currentPhaseIndex;
        public int RemainingWavesInPhase => _remainingWavesInPhase;

        public void Reset()
        {
            _currentPhaseIndex = -1;
            _remainingWavesInPhase = 0;
        }

        public bool IsCurrentPhaseValid(LevelDefinition level)
        {
            return level != null
                   && level.phases != null
                   && _currentPhaseIndex >= 0
                   && _currentPhaseIndex < level.phases.Count;
        }

        public LevelPhaseType? GetCurrentPhaseType(LevelDefinition level)
        {
            if (!IsCurrentPhaseValid(level))
            {
                return null;
            }

            return level.phases[_currentPhaseIndex].phaseType;
        }

        public string GetCurrentPhaseId(LevelDefinition level)
        {
            if (!IsCurrentPhaseValid(level))
            {
                return string.Empty;
            }

            return level.phases[_currentPhaseIndex].phaseId;
        }

        public void EnterPhase(LevelDefinition level, int index, Action completeLevel)
        {
            if (level == null)
            {
                return;
            }

            if (level.phases == null || level.phases.Count == 0)
            {
                // 未配置阶段时，回退到旧逻辑：清怪即通关。
                _currentPhaseIndex = -1;
                _remainingWavesInPhase = 0;
                return;
            }

            if (index < 0 || index >= level.phases.Count)
            {
                completeLevel?.Invoke();
                return;
            }

            _currentPhaseIndex = index;
            LevelPhaseData phase = level.phases[_currentPhaseIndex];
            _remainingWavesInPhase = Mathf.Max(1, phase.requiredWaveCount);

            if (phase.phaseType == LevelPhaseType.Complete)
            {
                completeLevel?.Invoke();
            }
        }

        public bool TryHandleTrigger(LevelDefinition level, string triggerId, Action advancePhase)
        {
            if (!IsCurrentPhaseValid(level))
            {
                return false;
            }

            LevelPhaseData phase = level.phases[_currentPhaseIndex];
            if (phase.phaseType != LevelPhaseType.ReachTrigger)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(phase.requiredTriggerId) && phase.requiredTriggerId == triggerId)
            {
                advancePhase?.Invoke();
                return true;
            }

            return false;
        }

        public bool TryAdvanceByMonsterClear(
            LevelDefinition level,
            Action completeLevel,
            Action advancePhase,
            Action spawnNextWave)
        {
            if (!IsCurrentPhaseValid(level))
            {
                // 兼容无阶段配置模式：清怪即通关。
                completeLevel?.Invoke();
                return true;
            }

            LevelPhaseData phase = level.phases[_currentPhaseIndex];
            if (phase.phaseType != LevelPhaseType.DefendWaves)
            {
                return false;
            }

            _remainingWavesInPhase--;
            if (_remainingWavesInPhase <= 0)
            {
                advancePhase?.Invoke();
            }
            else
            {
                spawnNextWave?.Invoke();
            }

            return true;
        }

        public void AdvancePhase(LevelDefinition level, Action completeLevel)
        {
            EnterPhase(level, _currentPhaseIndex + 1, completeLevel);
        }
    }
}
