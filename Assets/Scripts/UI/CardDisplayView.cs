using System;
using InboxZero.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Display-only card used in the All Mail compendium grid.
    /// No combat interaction — no click-to-play, no hover scale.
    /// Use Populate(), SetLocked(), SetDisabled(), and ShowToggle() to configure.
    public class CardDisplayView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Child References")]
        public Image           background;
        public Image           icon;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI effectText;

        [Header("Rarity Visuals")]
        public RarityVisuals starterVisuals;
        public RarityVisuals commonVisuals;
        public RarityVisuals uncommonVisuals;
        public RarityVisuals rareVisuals;
        public RarityVisuals legendaryVisuals;

        [Header("Overlays")]
        public GameObject      disabledOverlay;  // full-card grey tint, default inactive
        public Button          poolToggle;        // bottom bar ON/OFF toggle
        public TextMeshProUGUI toggleLabel;

        public CardData Data { get; private set; }

        static readonly Color AmberOn = new Color(1.00f, 0.88f, 0.55f);
        static readonly Color TextDim = new Color(0.50f, 0.50f, 0.55f);

        // ── Public API ────────────────────────────────────────────────────────

        public void Populate(CardData data)
        {
            Data = data;

            RarityVisuals visuals = data.rarity switch
            {
                CardRarity.Common    => commonVisuals,
                CardRarity.Uncommon  => uncommonVisuals,
                CardRarity.Rare      => rareVisuals,
                CardRarity.Legendary => legendaryVisuals,
                _                    => starterVisuals,
            };

            if (background != null && visuals.bg != null) background.sprite = visuals.bg;
            if (icon != null) { icon.sprite = data.icon; icon.enabled = data.icon != null; }
            if (nameText != null)   nameText.text   = data.cardName.ToUpper();
            if (costText != null)   costText.text   = data.apCost.ToString();
            if (effectText != null) effectText.text = data.effectDescription;
        }

        /// Hides icon and replaces text with ??? to indicate a locked/unknown card.
        public void SetLocked(bool locked)
        {
            if (!locked) return;
            if (icon != null)       icon.gameObject.SetActive(false);
            if (nameText != null)   nameText.text   = "???";
            if (costText != null)   costText.text   = "?";
            if (effectText != null) effectText.text = "???";
        }

        /// Shows or hides the grey disabled overlay (greyed-out, not transparent).
        public void SetDisabled(bool disabled)
        {
            if (disabledOverlay != null) disabledOverlay.SetActive(disabled);
        }

        /// Configures the pool-toggle button. Pass show=false for starters and locked cards.
        public void ShowToggle(bool show, bool isEnabled, Action<bool> onToggle)
        {
            if (poolToggle == null) return;
            poolToggle.gameObject.SetActive(show);
            if (!show) return;

            SetToggleVisual(isEnabled);
            poolToggle.onClick.RemoveAllListeners();
            bool state = isEnabled;
            poolToggle.onClick.AddListener(() =>
            {
                state = !state;
                SetToggleVisual(state);
                SetDisabled(!state);
                onToggle?.Invoke(state);
            });
        }

        // ── Hover callbacks (wired by AllMailScreen, not combat) ──────────────

        Action _onHoverEnter;
        Action _onHoverExit;

        public void SetHoverCallbacks(Action onEnter, Action onExit)
        {
            _onHoverEnter = onEnter;
            _onHoverExit  = onExit;
        }

        public void OnPointerEnter(PointerEventData _) => _onHoverEnter?.Invoke();
        public void OnPointerExit(PointerEventData _)  => _onHoverExit?.Invoke();

        // ── Private ───────────────────────────────────────────────────────────

        void SetToggleVisual(bool on)
        {
            if (toggleLabel != null)
            {
                toggleLabel.text  = on ? "ON" : "OFF";
                toggleLabel.color = on ? AmberOn : TextDim;
            }
        }
    }
}
