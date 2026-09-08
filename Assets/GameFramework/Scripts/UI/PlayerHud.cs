using GameFramework.Skill;
using GameFramework.Stats;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.UI
{
    /// <summary>
    /// 玩家平视 HUD（UGUI，代码构建）：
    /// - 左下角：血量条 + 护甲数值。
    /// - 底部中央：技能槽（按键 + 名称 + 径向冷却）。
    /// 设计要点：
    /// 1. 自包含——运行时自建 Canvas，不依赖场景中预置 UI 结构。
    /// 2. 数据只读——从玩家的 <see cref="ActorStatsComponent"/> 与 <see cref="SkillCaster"/> 拉取，不反向写入。
    /// 3. 延迟绑定——玩家若尚未生成，会持续按 Tag 重试查找。
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerHud : MonoBehaviour
    {
        private readonly struct SkillSlotUi
        {
            public readonly SkillType type;
            public readonly Image cooldownOverlay;
            public readonly Text cooldownText;
            public readonly Text nameText;

            public SkillSlotUi(SkillType type, Image cooldownOverlay, Text cooldownText, Text nameText)
            {
                this.type = type;
                this.cooldownOverlay = cooldownOverlay;
                this.cooldownText = cooldownText;
                this.nameText = nameText;
            }
        }

        [Header("绑定目标")]
        [SerializeField] private string playerTag = "Player";

        [Header("外观")]
        [SerializeField] private Color healthColor = new Color(0.2f, 0.85f, 0.28f, 1f);
        [SerializeField] private Color lowHealthColor = new Color(0.95f, 0.28f, 0.2f, 1f);
        [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.55f);
        [SerializeField] private Color slotReadyColor = new Color(0.15f, 0.55f, 0.95f, 0.9f);
        [SerializeField] private Color cooldownMaskColor = new Color(0f, 0f, 0f, 0.72f);

        // 技能槽显示配置：类型 + 显示用按键标签。
        private static readonly (SkillType type, string key)[] SlotConfigs =
        {
            (SkillType.Active1, "1"),
            (SkillType.Active2, "2"),
            (SkillType.Ultimate, "Q"),
        };

        private ActorStatsComponent _stats;
        private SkillCaster _caster;
        private Font _font;
        private Sprite _uiSprite;

        private Image _healthFill;
        private Text _healthText;
        private Text _armorText;
        private SkillSlotUi[] _slots;

        private float _nextBindRetryTime;

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUi();
            TryBindPlayer();
        }

        private void Update()
        {
            if (_stats == null || _caster == null)
            {
                if (Time.time >= _nextBindRetryTime)
                {
                    TryBindPlayer();
                }
                return;
            }

            RefreshHealth();
            RefreshSkills();
        }

        private void TryBindPlayer()
        {
            _nextBindRetryTime = Time.time + 0.5f;

            if (string.IsNullOrEmpty(playerTag))
            {
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player == null)
            {
                return;
            }

            _stats = player.GetComponentInParent<ActorStatsComponent>();
            _caster = player.GetComponentInParent<SkillCaster>();
        }

        private void RefreshHealth()
        {
            float maxHealth = Mathf.Max(1f, _stats.CurrentAttributes.MaxHealth);
            float current = Mathf.Clamp(_stats.CurrentHealth, 0f, maxHealth);
            float ratio = current / maxHealth;

            if (_healthFill != null)
            {
                _healthFill.fillAmount = ratio;
                _healthFill.color = Color.Lerp(lowHealthColor, healthColor, Mathf.Clamp01((ratio - 0.15f) / 0.35f));
            }

            if (_healthText != null)
            {
                _healthText.text = $"HP {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maxHealth)}";
            }

            if (_armorText != null)
            {
                // 内置字体无中文字形，统一使用 ASCII 文案避免缺字。
                _armorText.text = $"ARMOR {Mathf.RoundToInt(_stats.CurrentAttributes.armor)}";
            }
        }

        private void RefreshSkills()
        {
            if (_slots == null)
            {
                return;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                SkillSlotUi slot = _slots[i];
                float ratio = _caster.GetCooldownRatio(slot.type);
                float remaining = _caster.GetCooldownRemaining(slot.type);

                if (slot.cooldownOverlay != null)
                {
                    slot.cooldownOverlay.fillAmount = ratio;
                }

                if (slot.cooldownText != null)
                {
                    slot.cooldownText.text = remaining > 0.05f ? Mathf.CeilToInt(remaining).ToString() : string.Empty;
                }

                if (slot.nameText != null)
                {
                    SkillDefinition def = _caster.Loadout.GetByType(slot.type);
                    string desired = def != null ? def.displayName : "-";
                    if (slot.nameText.text != desired)
                    {
                        slot.nameText.text = desired;
                    }
                }
            }
        }

        // ---------- UI 构建 ----------

        private void BuildUi()
        {
            _uiSprite = CreateWhiteSprite();

            Canvas canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            BuildHealthPanel(canvas.transform);
            BuildSkillBar(canvas.transform);
        }

        private void BuildHealthPanel(Transform parent)
        {
            RectTransform panel = CreateRect("HealthPanel", parent);
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(0f, 0f);
            panel.pivot = new Vector2(0f, 0f);
            panel.anchoredPosition = new Vector2(40f, 40f);
            panel.sizeDelta = new Vector2(420f, 96f);

            Image bg = panel.gameObject.AddComponent<Image>();
            bg.sprite = _uiSprite;
            bg.color = panelColor;
            bg.raycastTarget = false;

            // 血条底槽
            RectTransform barBg = CreateRect("HealthBarBg", panel);
            StretchWithMargin(barBg, 16f, 44f, 16f, 16f);
            Image barBgImg = barBg.gameObject.AddComponent<Image>();
            barBgImg.sprite = _uiSprite;
            barBgImg.color = new Color(0f, 0f, 0f, 0.6f);
            barBgImg.raycastTarget = false;

            // 血条填充
            RectTransform fill = CreateRect("HealthBarFill", barBg);
            StretchFull(fill);
            _healthFill = fill.gameObject.AddComponent<Image>();
            _healthFill.sprite = _uiSprite;
            _healthFill.color = healthColor;
            _healthFill.raycastTarget = false;
            _healthFill.type = Image.Type.Filled;
            _healthFill.fillMethod = Image.FillMethod.Horizontal;
            _healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _healthFill.fillAmount = 1f;

            // 血量数值
            _healthText = CreateText("HealthText", panel, "HP", 22, TextAnchor.MiddleLeft);
            RectTransform hpRt = _healthText.rectTransform;
            hpRt.anchorMin = new Vector2(0f, 1f);
            hpRt.anchorMax = new Vector2(1f, 1f);
            hpRt.pivot = new Vector2(0f, 1f);
            hpRt.anchoredPosition = new Vector2(18f, -6f);
            hpRt.sizeDelta = new Vector2(-36f, 34f);

            // 护甲数值
            _armorText = CreateText("ArmorText", panel, "ARMOR 0", 18, TextAnchor.MiddleRight);
            RectTransform armorRt = _armorText.rectTransform;
            armorRt.anchorMin = new Vector2(0f, 1f);
            armorRt.anchorMax = new Vector2(1f, 1f);
            armorRt.pivot = new Vector2(1f, 1f);
            armorRt.anchoredPosition = new Vector2(-18f, -6f);
            armorRt.sizeDelta = new Vector2(-36f, 34f);
            _armorText.color = new Color(0.8f, 0.85f, 1f, 1f);
        }

        private void BuildSkillBar(Transform parent)
        {
            const float slotSize = 84f;
            const float spacing = 18f;
            int count = SlotConfigs.Length;
            float totalWidth = count * slotSize + (count - 1) * spacing;

            RectTransform bar = CreateRect("SkillBar", parent);
            bar.anchorMin = new Vector2(0.5f, 0f);
            bar.anchorMax = new Vector2(0.5f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = new Vector2(0f, 40f);
            bar.sizeDelta = new Vector2(totalWidth, slotSize + 26f);

            _slots = new SkillSlotUi[count];
            for (int i = 0; i < count; i++)
            {
                float x = -totalWidth * 0.5f + i * (slotSize + spacing) + slotSize * 0.5f;
                _slots[i] = BuildSkillSlot(bar, SlotConfigs[i].type, SlotConfigs[i].key, x, slotSize);
            }
        }

        private SkillSlotUi BuildSkillSlot(Transform parent, SkillType type, string key, float centerX, float size)
        {
            RectTransform slot = CreateRect($"Slot_{type}", parent);
            slot.anchorMin = new Vector2(0.5f, 0f);
            slot.anchorMax = new Vector2(0.5f, 0f);
            slot.pivot = new Vector2(0.5f, 0f);
            slot.anchoredPosition = new Vector2(centerX, 26f);
            slot.sizeDelta = new Vector2(size, size);

            Image bg = slot.gameObject.AddComponent<Image>();
            bg.sprite = _uiSprite;
            bg.color = slotReadyColor;
            bg.raycastTarget = false;

            // 冷却遮罩：径向填充，从满到空表示冷却推进。
            RectTransform overlayRt = CreateRect("Cooldown", slot);
            StretchFull(overlayRt);
            Image overlay = overlayRt.gameObject.AddComponent<Image>();
            overlay.sprite = _uiSprite;
            overlay.color = cooldownMaskColor;
            overlay.raycastTarget = false;
            overlay.type = Image.Type.Filled;
            overlay.fillMethod = Image.FillMethod.Radial360;
            overlay.fillOrigin = (int)Image.Origin360.Top;
            overlay.fillClockwise = false;
            overlay.fillAmount = 0f;

            // 按键标签（右上角）
            Text keyText = CreateText("Key", slot, key, 22, TextAnchor.UpperRight);
            RectTransform keyRt = keyText.rectTransform;
            StretchWithMargin(keyRt, 4f, 4f, 6f, 0f);
            keyText.color = Color.white;
            keyText.fontStyle = FontStyle.Bold;

            // 冷却剩余秒数（中央大字）
            Text cdText = CreateText("CooldownText", slot, string.Empty, 34, TextAnchor.MiddleCenter);
            StretchFull(cdText.rectTransform);
            cdText.color = Color.white;
            cdText.fontStyle = FontStyle.Bold;

            // 技能名称（槽位下方）
            Text nameText = CreateText("Name", parent, "-", 16, TextAnchor.MiddleCenter);
            RectTransform nameRt = nameText.rectTransform;
            nameRt.anchorMin = new Vector2(0.5f, 0f);
            nameRt.anchorMax = new Vector2(0.5f, 0f);
            nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.anchoredPosition = new Vector2(centerX, 24f);
            nameRt.sizeDelta = new Vector2(size + 20f, 22f);
            nameText.color = new Color(0.9f, 0.92f, 1f, 1f);

            return new SkillSlotUi(type, overlay, cdText, nameText);
        }

        // ---------- 构建辅助 ----------

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private Text CreateText(string name, Transform parent, string content, int fontSize, TextAnchor anchor)
        {
            RectTransform rt = CreateRect(name, parent);
            StretchFull(rt);
            Text text = rt.gameObject.AddComponent<Text>();
            text.font = _font;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void StretchWithMargin(RectTransform rt, float left, float top, float right, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>
        /// 生成一张 1x1 白色 Sprite，供纯色/填充型 Image 使用，
        /// 避免 Filled 类型无 sprite 时不渲染的问题。
        /// </summary>
        private static Sprite CreateWhiteSprite()
        {
            Texture2D tex = Texture2D.whiteTexture;
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
