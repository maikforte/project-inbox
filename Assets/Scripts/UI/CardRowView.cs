using System;
using InboxZero.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Sits on the CardRow prefab. Call Bind() to populate a row with card data.
    public class CardRowView : MonoBehaviour
    {
        [Header("Child References")]
        public Image           background;
        public Image           dot;
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI previewLabel;
        public TextMeshProUGUI rarityLabel;
        public TextMeshProUGUI costLabel;
        public Image           divider;
        public Button          toggleButton;

        // ── Palette ───────────────────────────────────────────────────────────
        static readonly Color AttackColor  = new Color(0.83f, 0.18f, 0.18f);
        static readonly Color DefendColor  = new Color(0.20f, 0.66f, 0.32f);
        static readonly Color SpecialColor = new Color(0.52f, 0.18f, 0.80f);
        static readonly Color TextDark     = new Color(1.00f, 1.00f, 1.00f);
        static readonly Color TextMedium   = new Color(0.75f, 0.75f, 0.78f);
        static readonly Color TextLight    = new Color(0.50f, 0.50f, 0.55f);
        static readonly Color AmberOn      = new Color(1.00f, 0.88f, 0.55f);
        static readonly Color ToggleOff    = new Color(0.28f, 0.26f, 0.32f);

        const float LockedAlpha = 0.35f;

        // ── Public API ────────────────────────────────────────────────────────

        /// <param name="isUnlocked">False → card shows as locked (???). True → shows normally.</param>
        /// <param name="isEnabled">Whether the card is enabled in the drop pool.</param>
        /// <param name="onToggle">Invoked with the new enabled state when clicked. Null hides the toggle.</param>
        public void Bind(CardData card, bool isUnlocked, bool isEnabled, Action<bool> onToggle)
        {
            if (!isUnlocked) { BindLocked(card); return; }
            BindUnlocked(card, isEnabled, onToggle);
        }

        // ── Private ───────────────────────────────────────────────────────────

        void BindLocked(CardData card)
        {
            Color Fade(Color c) => new Color(c.r, c.g, c.b, LockedAlpha);

            if (dot != null)          dot.color            = Fade(TextLight);
            if (nameLabel != null)  { nameLabel.text       = card.cardName.ToUpper(); nameLabel.color   = Fade(TextDark); }
            if (previewLabel != null){ previewLabel.text   = "???";                   previewLabel.color = Fade(TextLight); }
            if (rarityLabel != null) { rarityLabel.text    = card.rarity.ToString().ToUpper(); rarityLabel.color = Fade(TextLight); }
            if (costLabel != null)   { costLabel.text      = "?";                     costLabel.color    = Fade(TextMedium); }
            if (toggleButton != null)  toggleButton.gameObject.SetActive(false);
        }

        void BindUnlocked(CardData card, bool isEnabled, Action<bool> onToggle)
        {
            if (dot != null)          dot.color            = TypeColor(card.cardType);
            if (nameLabel != null)  { nameLabel.text       = card.cardName.ToUpper(); nameLabel.color   = TextDark; }
            if (previewLabel != null){ previewLabel.text   = card.effectDescription;  previewLabel.color = TextLight; }
            if (rarityLabel != null) { rarityLabel.text    = card.rarity.ToString().ToUpper(); rarityLabel.color = RarityColor(card.rarity); }
            if (costLabel != null)   { costLabel.text      = card.apCost.ToString();  costLabel.color   = TextMedium; }

            bool isStarter  = card.rarity == CardRarity.Starter;
            bool showToggle = !isStarter && onToggle != null;

            if (toggleButton != null)
            {
                toggleButton.gameObject.SetActive(showToggle);
                if (showToggle)
                {
                    SetToggleVisual(isEnabled);
                    toggleButton.onClick.RemoveAllListeners();
                    bool state = isEnabled;
                    toggleButton.onClick.AddListener(() =>
                    {
                        state = !state;
                        SetToggleVisual(state);
                        onToggle(state);
                    });
                }
            }
        }

        void SetToggleVisual(bool on)
        {
            if (toggleButton == null) return;
            var img = toggleButton.GetComponent<Image>();
            if (img != null) img.color = on ? AmberOn : ToggleOff;
            var label = toggleButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = on ? "ON" : "OFF";
        }

        static Color TypeColor(CardType type) => type switch
        {
            CardType.Attack => AttackColor,
            CardType.Defend => DefendColor,
            _               => SpecialColor,
        };

        static Color RarityColor(CardRarity rarity) => rarity switch
        {
            CardRarity.Uncommon => new Color(0.18f, 0.62f, 0.28f),
            CardRarity.Rare     => new Color(0.52f, 0.18f, 0.80f),
            _                   => TextLight,
        };
    }
}
