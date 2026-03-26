using System.Collections.Generic;
using InboxZero.Core;
using InboxZero.Data;
using UnityEngine;
using UnityEngine.Events;

namespace InboxZero.UI
{
    /// Read-only "All Mail" compendium screen — shows every card in the game.
    /// Cards the player owns (deck + collection) are shown normally;
    /// unacquired cards are dimmed with effect text replaced by "???".
    ///
    /// Layout lives in AllMailPanel prefab (edit that to change visuals).
    /// Row layout lives in CardRow prefab.
    public class AllMailScreen : MonoBehaviour
    {
        public static AllMailScreen Instance { get; private set; }

        [Header("Prefabs")]
        public GameObject panelPrefab;
        public GameObject cardRowPrefab;

        [Header("Card Registry")]
        [Tooltip("Assign the AllCardsRegistry SO here.")]
        public AllCardsRegistry registry;

        /// Fired when the player clicks Close.
        public UnityEvent OnClose = new UnityEvent();

        Canvas     _canvas;
        GameObject _panel;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _canvas  = FindObjectOfType<Canvas>();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// Show as a full-screen overlay on the canvas.
        public void Show()
        {
            if (_panel != null) Destroy(_panel);
            BuildPanel(_canvas != null ? _canvas.transform : null);
        }

        /// Show inside a specific content area (single-page-app style).
        public void ShowInContent(RectTransform contentArea)
        {
            if (_panel != null) Destroy(_panel);
            BuildPanel(contentArea);
        }

        public void Hide()
        {
            if (_panel != null) Destroy(_panel);
            _panel = null;
        }

        // ── Panel construction ────────────────────────────────────────────────

        void BuildPanel(Transform parent)
        {
            if (parent == null)       { Debug.LogError("[AllMailScreen] No parent to render into.");       return; }
            if (registry == null)     { Debug.LogWarning("[AllMailScreen] No AllCardsRegistry assigned."); return; }
            if (panelPrefab == null)  { Debug.LogError("[AllMailScreen] panelPrefab not assigned.");       return; }
            if (cardRowPrefab == null){ Debug.LogError("[AllMailScreen] cardRowPrefab not assigned.");     return; }

            _panel = Instantiate(panelPrefab, parent, false);

            var view = _panel.GetComponent<AllMailPanelView>();
            if (view == null) { Debug.LogError("[AllMailScreen] panelPrefab is missing AllMailPanelView."); return; }

            // Wire close button
            if (view.closeButton != null)
                view.closeButton.onClick.AddListener(OnCloseClicked);

            // Count unlocked cards for header
            var um = UnlockManager.Instance;
            int total         = registry.allCards.Count;
            int unlockedCount = 0;
            foreach (var c in registry.allCards)
                if (um == null || um.IsUnlocked(c)) unlockedCount++;

            if (view.statsLabel != null)
                view.statsLabel.text = $"ALL MAIL  {unlockedCount} / {total} UNLOCKED";
            if (view.columnHeaderLabel != null)
                view.columnHeaderLabel.text = $"ALL CARDS  ({total})";

            // Sort: Starter → Common → Uncommon → Rare, then alphabetically
            var cards = new List<CardData>(registry.allCards);
            cards.Sort((a, b) =>
            {
                int diff = ((int)a.rarity).CompareTo((int)b.rarity);
                return diff != 0 ? diff : string.Compare(a.cardName, b.cardName, System.StringComparison.Ordinal);
            });

            // Populate scroll rows
            if (view.scrollContent != null)
            {
                float rowH = cardRowPrefab.GetComponent<RectTransform>().sizeDelta.y;

                for (int i = 0; i < cards.Count; i++)
                {
                    var card   = cards[i];
                    var rowGo  = Instantiate(cardRowPrefab, view.scrollContent, false);
                    var rowRt  = rowGo.GetComponent<RectTransform>();
                    rowRt.anchoredPosition = new Vector2(0, -i * rowH);

                    bool isUnlocked = um == null || um.IsUnlocked(card);
                    bool isEnabled  = um == null || um.IsEnabled(card);

                    var rowView = rowGo.GetComponent<CardRowView>();
                    if (rowView != null)
                        rowView.Bind(card, isUnlocked, isEnabled, enabled => um?.SetEnabled(card, enabled));
                }

                view.scrollContent.sizeDelta = new Vector2(0, cards.Count * rowH);
            }
        }

        // ── Event handler ─────────────────────────────────────────────────────

        void OnCloseClicked()
        {
            Hide();
            OnClose.Invoke();
        }

    }
}
