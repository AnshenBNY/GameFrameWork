using GameFramework.Skill;
using GameFramework.Stats;
using GameFramework.Weapon;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.Character
{
    /// <summary>
    /// 玩家战斗 HUD：
    /// - 生命值 / 弹药 / 技能冷却
    /// - 屏幕中心准心
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

        [Header("准心")]
        [SerializeField] private RectTransform crosshairRoot;
        [SerializeField] private bool createCrosshairIfMissing = true;
        [SerializeField] private Color crosshairColor = new Color(1f, 1f, 1f, 0.95f);
        [SerializeField] private float crosshairLineLength = 10f;
        [SerializeField] private float crosshairLineThickness = 2f;
        [SerializeField] private float crosshairGap = 4f;

        private void Awake()
        {
            if (createCrosshairIfMissing && crosshairRoot == null)
            {
                crosshairRoot = BuildCrosshair(transform);
            }
        }

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

        /// <summary>
        /// 在屏幕中心创建简易十字准心。
        /// </summary>
        private RectTransform BuildCrosshair(Transform parent)
        {
            GameObject rootGo = new GameObject("Crosshair");
            rootGo.transform.SetParent(parent, false);

            RectTransform root = rootGo.AddComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = Vector2.zero;

            CreateCrosshairLine(root, "CrosshairUp", new Vector2(0f, crosshairGap), new Vector2(crosshairLineThickness, crosshairLineLength));
            CreateCrosshairLine(root, "CrosshairDown", new Vector2(0f, -crosshairGap), new Vector2(crosshairLineThickness, crosshairLineLength));
            CreateCrosshairLine(root, "CrosshairLeft", new Vector2(-crosshairGap, 0f), new Vector2(crosshairLineLength, crosshairLineThickness));
            CreateCrosshairLine(root, "CrosshairRight", new Vector2(crosshairGap, 0f), new Vector2(crosshairLineLength, crosshairLineThickness));

            return root;
        }

        private void CreateCrosshairLine(RectTransform parent, string name, Vector2 anchoredPos, Vector2 size)
        {
            GameObject lineGo = new GameObject(name);
            lineGo.transform.SetParent(parent, false);

            RectTransform rect = lineGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Image image = lineGo.AddComponent<Image>();
            image.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Simple;
            image.color = crosshairColor;
            image.raycastTarget = false;
        }
    }
}
