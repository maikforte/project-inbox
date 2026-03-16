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
        public Image background;
        public Image dot;
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI previewLabel;
        public TextMeshProUGUI rarityLabel;
        public TextMeshProUGUI costLabel;
        public Image divider;

        // ── Palette (matches AllMailScreen) ──────────────────────────────────
        static readonly Color AttackColor  = new Color(0.83f, 0.18f, 0.18f);
        static readonly Color DefendColor  = new Color(0.20f, 0.66f, 0.32f);
        static readonly Color SpecialColor = new Color(0.52f, 0.18f, 0.80f);
        static readonly Color TextDark     = new Color(0.13f, 0.13f, 0.13f);
        static readonly Color TextMedium   = new Color(0.40f, 0.40f, 0.40f);
        static readonly Color TextLight    = new Color(0.60f, 0.60f, 0.60f);

        const float UnownedAlpha = 0.35f;

        public void Bind(CardData card, bool isOwned)
        {
            float alpha = isOwned ? 1f : UnownedAlpha;
            Color Fade(Color c) => new Color(c.r, c.g, c.b, c.a * alpha);

            if (dot != null)
                dot.color = Fade(isOwned ? TypeColor(card.cardType) : TextLight);

            if (nameLabel != null)
            {
                nameLabel.text  = card.cardName.ToUpper();
                nameLabel.color = Fade(TextDark);
            }

            if (previewLabel != null)
            {
                previewLabel.text  = isOwned ? card.effectDescription : "???";
                previewLabel.color = Fade(TextLight);
            }

            if (rarityLabel != null)
            {
                rarityLabel.text  = card.rarity.ToString().ToUpper();
                rarityLabel.color = Fade(isOwned ? RarityColor(card.rarity) : TextLight);
            }

            if (costLabel != null)
            {
                costLabel.text  = card.apCost.ToString();
                costLabel.color = Fade(TextMedium);
            }
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
