namespace GameFramework.Skill
{
    /// <summary>
    /// 技能目标模式：
    /// - SelfOrSingle: 单体（优先自身或前方最近目标）
    /// - SphereArea: 圆形范围
    /// - ConeArea: 扇形范围
    /// </summary>
    public enum SkillTargetMode
    {
        SelfOrSingle = 0,
        SphereArea = 1,
        ConeArea = 2
    }
}
