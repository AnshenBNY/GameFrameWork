using System;
using UnityEngine;

namespace GameFramework.Skill
{
    /// <summary>
    /// 技能装配表：绑定角色的 4 个技能槽位。
    /// </summary>
    [Serializable]
    public class SkillLoadout
    {
        public SkillDefinition active1;
        public SkillDefinition active2;
        public SkillDefinition ultimate;
        public SkillDefinition passive;

        public SkillDefinition GetByType(SkillType type)
        {
            return type switch
            {
                SkillType.Active1 => active1,
                SkillType.Active2 => active2,
                SkillType.Ultimate => ultimate,
                SkillType.Passive => passive,
                _ => null
            };
        }
    }
    
    
    
}
