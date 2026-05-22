using GameFramework.Skill;
using GameFramework.Stats;
using GameFramework.Weapon;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.Character
{
    /// <summary>
    /// 玩家战斗 HUD：
    /// - 生命值
    /// - 弹药
    /// - 技能冷却状态
    /// 说明：这里使用 UGUI 文本做最小实现，后续可替换为正式 UI 框架。
    /// </summary>
    public class PlayerCombatHud : MonoBehaviour
    {
        [SerializeField] private ActorStatsComponent playerStats;
        [SerializeField] private WeaponRuntime weaponRuntime;
        [SerializeField] private SkillCaster skillCaster;

        [Header("UI 绑定")]
        [SerializeField] private Text healthText;
        [SerializeField] private Text ammoText;
        [SerializeField] private Text skillText;

        private void Update()
        {
            RefreshHealth();
            RefreshAmmo();
            RefreshSkillInfo();
        }

        private void RefreshHealth()
        {
            if (healthText == null || playerStats == null)
            {
                return;
            }

            healthText.text = $"HP: {playerStats.CurrentHealth:0} / {playerStats.CurrentAttributes.MaxHealth:0}";
        }

        private void RefreshAmmo()
        {
            if (ammoText == null || weaponRuntime == null || weaponRuntime.Definition == null)
            {
                return;
            }

            string reloadTag = weaponRuntime.IsReloading ? " [Reloading]" : string.Empty;
            ammoText.text = $"Ammo: {weaponRuntime.CurrentAmmo} / {weaponRuntime.Definition.clipSize} | Reserve: {weaponRuntime.ReserveAmmo}{reloadTag}";
        }

        private void RefreshSkillInfo()
        {
            if (skillText == null || skillCaster == null || skillCaster.Loadout == null)
            {
                return;
            }

            SkillDefinition active1 = skillCaster.Loadout.active1;
            SkillDefinition active2 = skillCaster.Loadout.active2;
            SkillDefinition ultimate = skillCaster.Loadout.ultimate;

            string s1 = BuildSkillState("1", active1);
            string s2 = BuildSkillState("2", active2);
            string s3 = BuildSkillState("Q", ultimate);
            skillText.text = $"{s1}    {s2}    {s3}";
        }

        private string BuildSkillState(string key, SkillDefinition skill)
        {
            if (skill == null)
            {
                return $"{key}:None";
            }

            bool ready = skillCaster.IsSkillReady(skill);
            return $"{key}:{skill.displayName}({(ready ? "Ready" : "CD")})";
        }
    }
}
