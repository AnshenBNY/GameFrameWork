using System;

namespace GameFramework.Level
{
    /// <summary>
    /// 关卡阶段配置：
    /// 以数组形式组合后即可表达线性关卡流程。
    /// </summary>
    [Serializable]
    public class LevelPhaseData
    {
        public string phaseId;
        public LevelPhaseType phaseType = LevelPhaseType.ReachTrigger;

        // ReachTrigger 所需字段
        public string requiredTriggerId;

        // DefendWaves 所需字段（当前简化为“清空场上怪物”）
        public int requiredWaveCount = 1;
    }
}
