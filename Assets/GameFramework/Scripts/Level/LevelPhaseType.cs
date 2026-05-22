namespace GameFramework.Level
{
    /// <summary>
    /// 关卡阶段类型：
    /// - ReachTrigger: 到达指定触发点
    /// - DefendWaves: 防守波次（清空当前怪物）
    /// - Complete: 关卡结算
    /// </summary>
    public enum LevelPhaseType
    {
        ReachTrigger = 0,
        DefendWaves = 1,
        Complete = 2
    }
}
