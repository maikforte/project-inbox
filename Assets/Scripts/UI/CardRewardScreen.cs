using System.Collections.Generic;
using InboxZero.Core;
using InboxZero.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace InboxZero.UI
{
    public class CardRewardScreen : MonoBehaviour
    {
        public static CardRewardScreen Instance { get; private set; }

        [Header("Card Registry")]
        [Tooltip("All cards in the game. Reward pool is built at runtime from unlocked+enabled cards.")]
        public AllCardsRegistry registry;

        [Header("Card Prefab")]
        [Tooltip("Same CardDisplayView prefab used in All Mail. Toggle is hidden in reward context.")]
        public CardDisplayView cardDisplayPrefab;

        [Header("Font")]
        [Tooltip("BetterPixels — used for the title and skip button labels.")]
        public TMP_FontAsset cardFont;

        // Fired after the player picks or skips.
        public UnityEvent OnComplete = new UnityEvent();

        const int   CardCount   = 3;
        const float CardSpacing = 16f;

        Canvas     _canvas;
        GameObject _panel;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _canvas = FindObjectOfType<Canvas>();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Show(RewardTier tier)
        {
            var options = DrawOptions(tier);
            BuildPanel(options);
        }

        public void Hide()
        {
            if (_panel != null) Destroy(_panel);
            _panel = null;
        }

        // ── Pool filtering ────────────────────────────────────────────────────

        List<CardData> DrawOptions(RewardTier tier)
        {
            var allowed = AllowedRarities(tier);
            var um = UnlockManager.Instance;

            var pool = new List<CardData>();
            if (registry != null)
            {
                foreach (var card in registry.allCards)
                {
                    if (card == null) continue;
                    if (!allowed.Contains(card.rarity)) continue;
                    if (card.rarity == CardRarity.Starter) continue;
                    if (um != null && !um.IsEnabled(card)) continue;
                    pool.Add(card);
                }
            }

            // Shuffle and pick up to CardCount
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            var result = new List<CardData>();
            for (int i = 0; i < Mathf.Min(CardCount, pool.Count); i++)
                result.Add(pool[i]);
            return result;
        }

        // Common → Common only, Uncommon → Common/Uncommon,
        // Rare → Uncommon/Rare, Boss → Rare guaranteed
        static List<CardRarity> AllowedRarities(RewardTier tier) => tier switch
        {
            RewardTier.Common   => new List<CardRarity> { CardRarity.Common },
            RewardTier.Uncommon => new List<CardRarity> { CardRarity.Common, CardRarity.Uncommon },
            RewardTier.Rare     => new List<CardRarity> { CardRarity.Uncommon, CardRarity.Rare },
            _                   => new List<CardRarity> { CardRarity.Rare },
        };

        // ── Panel construction ────────────────────────────────────────────────

        void BuildPanel(List<CardData> options)
        {
            if (_canvas == null) { Debug.LogError("[CardRewardScreen] No Canvas found."); return; }
            if (cardDisplayPrefab == null) { Debug.LogError("[CardRewardScreen] cardDisplayPrefab not assigned."); return; }

            _panel = new GameObject("CardRewardPanel", typeof(RectTransform));
            _panel.layer = 5;
            var panelRt = _panel.GetComponent<RectTransform>();
            panelRt.SetParent(_canvas.transform, false);
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            // Dark overlay — blocks clicks to the combat scene underneath
            _panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

            // "CHOOSE A CARD" title
            MakeLabel("Title", panelRt,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -30f), new Vector2(300f, 20f),
                "CHOOSE A CARD", 14, TextAlignmentOptions.Center);

            // Horizontal card row — centred in the panel
            var rowGo = new GameObject("CardRow", typeof(RectTransform));
            rowGo.layer = 5;
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.SetParent(panelRt, false);
            rowRt.anchorMin = new Vector2(0.5f, 0.5f);
            rowRt.anchorMax = new Vector2(0.5f, 0.5f);
            rowRt.pivot     = new Vector2(0.5f, 0.5f);
            rowRt.anchoredPosition = Vector2.zero;
            rowRt.sizeDelta = Vector2.zero;

            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing            = CardSpacing;
            hlg.childAlignment     = TextAnchor.MiddleCenter;
            hlg.childControlWidth  = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;

            var csf = rowGo.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            foreach (var card in options)
                BuildRewardCard(rowRt, card);

            // Skip button
            var skipGo = new GameObject("SkipButton", typeof(RectTransform));
            skipGo.layer = 5;
            var skipRt = skipGo.GetComponent<RectTransform>();
            skipRt.SetParent(panelRt, false);
            skipRt.anchorMin        = new Vector2(0.5f, 0f);
            skipRt.anchorMax        = new Vector2(0.5f, 0f);
            skipRt.pivot            = new Vector2(0.5f, 0f);
            skipRt.anchoredPosition = new Vector2(0f, 20f);
            skipRt.sizeDelta        = new Vector2(80f, 20f);
            skipGo.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.35f);
            skipGo.AddComponent<Button>().onClick.AddListener(OnSkip);
            var skipLabel = MakeLabel("Label", skipRt,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, "SKIP", 14, TextAlignmentOptions.Center);
            skipLabel.GetComponent<TextMeshProUGUI>().enableWordWrapping = false;
        }

        void BuildRewardCard(RectTransform parent, CardData data)
        {
            var (canAdd, reason) = DeckCompositionChecker.CanAdd(data);

            var cdv = Instantiate(cardDisplayPrefab, parent, false);
            cdv.Populate(data);
            cdv.ShowToggle(false, false, null);   // hide ON/OFF toggle in reward context

            if (!canAdd)
            {
                cdv.SetDisabled(true);

                // Reason label centred over the card
                var reasonGo = new GameObject("Reason", typeof(RectTransform));
                reasonGo.layer = 5;
                var reasonRt = reasonGo.GetComponent<RectTransform>();
                reasonRt.SetParent(cdv.GetComponent<RectTransform>(), false);
                reasonRt.anchorMin = new Vector2(0f, 0.5f);
                reasonRt.anchorMax = new Vector2(1f, 0.5f);
                reasonRt.pivot     = new Vector2(0.5f, 0.5f);
                reasonRt.offsetMin = Vector2.zero;
                reasonRt.offsetMax = Vector2.zero;
                reasonRt.sizeDelta = new Vector2(0f, 28f);
                var reasonTmp = reasonGo.AddComponent<TextMeshProUGUI>();
                reasonTmp.text               = reason;
                reasonTmp.fontSize           = 14;
                reasonTmp.color              = new Color(1f, 0.35f, 0.35f);
                reasonTmp.alignment          = TextAlignmentOptions.Center;
                reasonTmp.enableWordWrapping = true;
                reasonTmp.raycastTarget      = false;
                if (cardFont != null) reasonTmp.font = cardFont;
            }
            else
            {
                // Wire click — add a Button on top so the whole card is clickable
                var btn = cdv.gameObject.AddComponent<Button>();
                var captured = data;
                btn.onClick.AddListener(() => OnPick(captured));

                // Subtle highlight on hover via colour tint
                var cb = btn.colors;
                cb.normalColor      = Color.white;
                cb.highlightedColor = new Color(0.85f, 0.85f, 1f);
                cb.pressedColor     = new Color(0.65f, 0.65f, 0.9f);
                btn.colors = cb;
            }
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        void OnPick(CardData card)
        {
            GameManager.Instance.DrawPile.Add(card);
            Hide();
            OnComplete.Invoke();
        }

        void OnSkip()
        {
            Hide();
            OnComplete.Invoke();
        }

        // ── Helper ────────────────────────────────────────────────────────────

        GameObject MakeLabel(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size, string text,
            int fontSize, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.color     = Color.white;
            tmp.alignment = align;
            if (cardFont != null) tmp.font = cardFont;

            return go;
        }
    }
}
