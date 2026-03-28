using System.Collections.Generic;
using InboxZero.Core;
using InboxZero.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Full-screen deck builder styled like the Gmail inbox screen.
    /// Left column: active deck. Right column: collection pool.
    /// Triggered automatically after rest stops and level transitions.
    public class DeckBuilderScreen : MonoBehaviour
    {
        public static DeckBuilderScreen Instance { get; private set; }

        [Header("Font")]
        public TMP_FontAsset uiFont;

        [Header("Prefabs")]
        public GameObject pagePrefab;     // DraftsPage.prefab — layout for ShowInContent
        public GameObject cardRowPrefab;  // CardRow.prefab — shared with AllMailScreen

        /// Fired when the player clicks Done.
        public UnityEvent OnComplete = new UnityEvent();

        // ── Dark theme palette ────────────────────────────────────────────────
        static readonly Color BgColor       = new Color(0.08f,  0.06f,  0.10f);
        static readonly Color TopBarColor   = new Color(0.12f,  0.10f,  0.15f);
        static readonly Color ColHeaderBg   = new Color(0.15f,  0.13f,  0.18f);
        static readonly Color RowBg         = new Color(0.10f,  0.08f,  0.12f);
        static readonly Color RowHoverColor = new Color(0.20f,  0.17f,  0.26f);
        static readonly Color DividerColor  = new Color(0.22f,  0.20f,  0.25f);
        static readonly Color AccentColor   = new Color(0.102f, 0.451f, 0.910f);
        static readonly Color TextDark      = new Color(1.00f,  1.00f,  1.00f);
        static readonly Color TextMedium    = new Color(0.75f,  0.75f,  0.78f);
        static readonly Color TextLight     = new Color(0.50f,  0.50f,  0.55f);
        static readonly Color ErrorColor    = new Color(0.96f,  0.36f,  0.36f);

        static readonly Color AttackColor  = new Color(0.83f, 0.18f, 0.18f);
        static readonly Color DefendColor  = new Color(0.20f, 0.66f, 0.32f);
        static readonly Color SpecialColor = new Color(0.52f, 0.18f, 0.80f);

        // ── Layout constants ──────────────────────────────────────────────────
        const float TopBarH    = 26f;
        const float DividerH   = 1f;
        const float ColHeaderH = 18f;
        const float RowH       = 26f;
        const float RowGap     = 1f;
        const float DotSize    = 6f;
        const float NameW      = 104f;
        const float PreviewW   = 110f;
        const float RarityW    = 52f;
        const float CostW      = 18f;
        const float MarginL    = 10f;
        const float MarginR    = 6f;

        Canvas    _canvas;
        GameObject _panel;

        RectTransform    _deckContent;
        RectTransform    _collContent;
        TextMeshProUGUI  _statsLabel;
        TextMeshProUGUI  _deckHdrLabel;
        TextMeshProUGUI  _collHdrLabel;
        Color            _statsBaseColor;

        float _flashTimer;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _canvas = FindObjectOfType<Canvas>();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Show()
        {
            if (_panel != null) Destroy(_panel);
            BuildPanel();
        }

        /// Show inside a specific content area (SPA style — used by FloorMapScreen sidebar).
        public void ShowInContent(RectTransform contentArea)
        {
            if (_panel != null) Destroy(_panel);
            if (contentArea == null)   { Debug.LogError("[DeckBuilderScreen] No content area.");           return; }
            if (pagePrefab == null)    { Debug.LogError("[DeckBuilderScreen] pagePrefab missing.");        return; }
            if (cardRowPrefab == null) { Debug.LogError("[DeckBuilderScreen] cardRowPrefab missing.");     return; }

            _panel = Instantiate(pagePrefab, contentArea, false);
            var view = _panel.GetComponent<DraftsPageView>();
            if (view == null) { Debug.LogError("[DeckBuilderScreen] pagePrefab missing DraftsPageView."); return; }

            _statsLabel      = view.statsLabel;
            _deckHdrLabel    = view.deckHeaderLabel;
            _collHdrLabel    = view.collHeaderLabel;
            _deckContent     = view.deckContent;
            _collContent     = view.collContent;
            _statsBaseColor  = view.statsLabel != null ? view.statsLabel.color : TextMedium;

            PopulateColumns();
        }

        public void Hide()
        {
            if (_panel != null) Destroy(_panel);
            _panel = null;
        }

        // ── Panel construction ────────────────────────────────────────────────

        void BuildPanel()
        {
            if (_canvas == null) { Debug.LogError("[DeckBuilderScreen] No Canvas found."); return; }

            // Root — full-screen near-white Gmail background
            _panel = new GameObject("DeckBuilderPanel", typeof(RectTransform));
            _panel.layer = 5;
            var rt = _panel.GetComponent<RectTransform>();
            rt.SetParent(_canvas.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = BgColor;

            // ── Top bar ───────────────────────────────────────────────────────
            var topBarRt = MakeRect("TopBar", rt,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f),
                new Vector2(0, -TopBarH), new Vector2(0, TopBarH));
            topBarRt.gameObject.AddComponent<Image>().color = TopBarColor;

            // "< DECK BUILDER" title
            MakeLabel("Title", topBarRt,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                new Vector2(MarginL, 0), new Vector2(150, 0),
                "< DECK BUILDER", 14, TextAlignmentOptions.MidlineLeft, AccentColor);

            // Live composition stats (centre)
            var statsGo = MakeLabel("Stats", topBarRt,
                new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(230, 0),
                BuildStatsText(), 14, TextAlignmentOptions.Center, TextMedium);
            _statsLabel     = statsGo.GetComponent<TextMeshProUGUI>();
            _statsBaseColor = TextMedium;

            // DONE button (top-right, Google-blue pill)
            var doneBtnGo = new GameObject("DoneButton", typeof(RectTransform));
            doneBtnGo.layer = 5;
            var doneBtnRt  = doneBtnGo.GetComponent<RectTransform>();
            doneBtnRt.SetParent(topBarRt, false);
            doneBtnRt.anchorMin        = new Vector2(1, 0.5f);
            doneBtnRt.anchorMax        = new Vector2(1, 0.5f);
            doneBtnRt.pivot            = new Vector2(1, 0.5f);
            doneBtnRt.anchoredPosition = new Vector2(-MarginR, 0);
            doneBtnRt.sizeDelta        = new Vector2(52, 18);
            doneBtnGo.AddComponent<Image>().color = AccentColor;
            var doneBtn = doneBtnGo.AddComponent<Button>();
            doneBtn.onClick.AddListener(OnDone);
            SetButtonColors(doneBtn,
                AccentColor,
                new Color(0.15f, 0.52f, 0.98f),
                new Color(0.08f, 0.38f, 0.80f));
            var doneLbl = MakeLabel("Label", doneBtnRt,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, "DONE", 14, TextAlignmentOptions.Center, Color.white);
            doneLbl.GetComponent<TextMeshProUGUI>().enableWordWrapping = false;

            // ── Horizontal divider under top bar ──────────────────────────────
            MakeRect("HDiv", rt,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f),
                new Vector2(0, -(TopBarH + DividerH)), new Vector2(0, DividerH))
                .gameObject.AddComponent<Image>().color = DividerColor;

            // ── Column header strips ───────────────────────────────────────────
            float colHeaderY = -(TopBarH + DividerH + ColHeaderH);

            var deckHdrRt = MakeRect("DeckHeader", rt,
                new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(0f, 1f),
                new Vector2(0, colHeaderY), new Vector2(0, ColHeaderH));
            deckHdrRt.gameObject.AddComponent<Image>().color = ColHeaderBg;
            var deckHdrLbl = MakeLabel("Label", deckHdrRt,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f),
                new Vector2(MarginL, 0), new Vector2(0, 0),
                DeckHeaderText(), 14, TextAlignmentOptions.MidlineLeft, TextDark);
            _deckHdrLabel = deckHdrLbl.GetComponent<TextMeshProUGUI>();

            var collHdrRt = MakeRect("CollHeader", rt,
                new Vector2(0.5f, 1), new Vector2(1, 1), new Vector2(0f, 1f),
                new Vector2(0, colHeaderY), new Vector2(0, ColHeaderH));
            collHdrRt.gameObject.AddComponent<Image>().color = ColHeaderBg;
            var collHdrLbl = MakeLabel("Label", collHdrRt,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f),
                new Vector2(MarginL, 0), new Vector2(0, 0),
                CollHeaderText(), 14, TextAlignmentOptions.MidlineLeft, TextMedium);
            _collHdrLabel = collHdrLbl.GetComponent<TextMeshProUGUI>();

            // Column header bottom divider
            MakeRect("ColHDiv", rt,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f),
                new Vector2(0, -(TopBarH + DividerH + ColHeaderH + DividerH)), new Vector2(0, DividerH))
                .gameObject.AddComponent<Image>().color = DividerColor;

            // Vertical centre divider (full height)
            MakeRect("VDiv", rt,
                new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(DividerH, 0))
                .gameObject.AddComponent<Image>().color = DividerColor;

            // ── Scrollable list areas ──────────────────────────────────────────
            float listTop = TopBarH + DividerH + ColHeaderH + DividerH;

            _deckContent = BuildScrollView("DeckScroll", rt,
                new Vector2(0,    0), new Vector2(0.5f, 1),
                new Vector2(0, 0), new Vector2(0, -listTop));

            _collContent = BuildScrollView("CollScroll", rt,
                new Vector2(0.5f, 0), new Vector2(1, 1),
                new Vector2(0, 0), new Vector2(0, -listTop));

            PopulateColumns();
        }

        RectTransform BuildScrollView(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var scrollRt = go.GetComponent<RectTransform>();
            scrollRt.SetParent(parent, false);
            scrollRt.anchorMin = anchorMin;
            scrollRt.anchorMax = anchorMax;
            scrollRt.offsetMin = offsetMin;
            scrollRt.offsetMax = offsetMax;
            go.AddComponent<Image>().color = BgColor;

            // Viewport
            var viewGo = new GameObject("Viewport", typeof(RectTransform));
            viewGo.layer = 5;
            var viewRt = viewGo.GetComponent<RectTransform>();
            viewRt.SetParent(scrollRt, false);
            viewRt.anchorMin = Vector2.zero;
            viewRt.anchorMax = Vector2.one;
            viewRt.offsetMin = Vector2.zero;
            viewRt.offsetMax = Vector2.zero;
            viewGo.AddComponent<RectMask2D>();

            // Content
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.layer = 5;
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.SetParent(viewRt, false);
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot     = new Vector2(0, 1);
            contentRt.sizeDelta = Vector2.zero;

            var sr = go.AddComponent<ScrollRect>();
            sr.content           = contentRt;
            sr.viewport          = viewRt;
            sr.horizontal        = false;
            sr.vertical          = true;
            sr.scrollSensitivity = 20f;
            sr.movementType      = ScrollRect.MovementType.Clamped;

            return contentRt;
        }

        // ── Column population ─────────────────────────────────────────────────

        void PopulateColumns()
        {
            if (_deckContent == null || _collContent == null)
            {
                Debug.LogError("[DeckBuilderScreen] deckContent or collContent is null — check DraftsPageView fields on the prefab.");
                return;
            }

            foreach (Transform c in _deckContent) Destroy(c.gameObject);
            foreach (Transform c in _collContent)  Destroy(c.gameObject);

            var deck = GetActiveDeck();
            for (int i = 0; i < deck.Count; i++)
                AddCardRow(_deckContent, deck[i], i, isDeckCard: true);
            SetContentHeight(_deckContent, deck.Count);

            // Collection pool removed (TASK-38) — right column is unused.
            SetContentHeight(_collContent, 0);

            RefreshHeaders();
        }

        float RowUnitHeight => cardRowPrefab != null
            ? cardRowPrefab.GetComponent<RectTransform>().sizeDelta.y
            : RowH + RowGap;

        void AddCardRow(RectTransform parent, CardData card, int index, bool isDeckCard)
        {
            float yPos = -index * RowUnitHeight;

            GameObject rowGo;
            if (cardRowPrefab != null)
            {
                rowGo = Instantiate(cardRowPrefab, parent, false);
                rowGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, yPos);
                rowGo.GetComponent<CardRowView>()?.Bind(card, isUnlocked: true, isEnabled: true, onToggle: null);

                var prefabBtn      = rowGo.AddComponent<Button>();
                var prefabCaptured = card;
                prefabBtn.onClick.AddListener(isDeckCard ? () => OnRemoveCard(prefabCaptured) : () => OnAddCard(prefabCaptured));
                SetButtonColors(prefabBtn, new Color(0, 0, 0, 0), RowHoverColor, new Color(0.20f, 0.17f, 0.30f));
                return;
            }

            rowGo = new GameObject(card.cardName, typeof(RectTransform));
            rowGo.layer = 5;
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.SetParent(parent, false);
            rowRt.anchorMin        = new Vector2(0, 1);
            rowRt.anchorMax        = new Vector2(1, 1);
            rowRt.pivot            = new Vector2(0, 1);
            rowRt.anchoredPosition = new Vector2(0, yPos);
            rowRt.sizeDelta        = new Vector2(0, RowH);

            rowGo.AddComponent<Image>().color = RowBg;

            // Type-colour indicator dot (like unread/category dot in Gmail)
            var dotGo = new GameObject("Dot", typeof(RectTransform));
            dotGo.layer = 5;
            var dotRt = dotGo.GetComponent<RectTransform>();
            dotRt.SetParent(rowRt, false);
            dotRt.anchorMin        = new Vector2(0, 0.5f);
            dotRt.anchorMax        = new Vector2(0, 0.5f);
            dotRt.pivot            = new Vector2(0, 0.5f);
            dotRt.anchoredPosition = new Vector2(MarginL, 0);
            dotRt.sizeDelta        = new Vector2(DotSize, DotSize);
            dotGo.AddComponent<Image>().color = TypeColor(card.cardType);

            // Card name — fixed width, sender-column equivalent
            MakeLabel("Name", rowRt,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                new Vector2(MarginL + DotSize + 6f, 0), new Vector2(NameW, 0),
                card.cardName.ToUpper(), 14, TextAlignmentOptions.MidlineLeft, TextDark);

            // Effect preview — subject+preview equivalent (fixed width, grey)
            MakeLabel("Preview", rowRt,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                new Vector2(MarginL + DotSize + 6f + NameW + 6f, 0), new Vector2(PreviewW, 0),
                card.effectDescription, 14, TextAlignmentOptions.MidlineLeft, TextLight);

            // Rarity — date-column equivalent (right-aligned)
            MakeLabel("Rarity", rowRt,
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f),
                new Vector2(-(CostW + MarginR + 4f), 0), new Vector2(RarityW, 0),
                card.rarity.ToString().ToUpper(), 14, TextAlignmentOptions.MidlineRight,
                RarityColor(card.rarity));

            // AP cost pip (far right)
            MakeLabel("Cost", rowRt,
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f),
                new Vector2(-MarginR, 0), new Vector2(CostW, 0),
                card.apCost.ToString(), 14, TextAlignmentOptions.MidlineRight, TextMedium);

            // Bottom divider line
            var divGo = new GameObject("Div", typeof(RectTransform));
            divGo.layer = 5;
            var divRt = divGo.GetComponent<RectTransform>();
            divRt.SetParent(rowRt, false);
            divRt.anchorMin        = new Vector2(0, 0);
            divRt.anchorMax        = new Vector2(1, 0);
            divRt.pivot            = new Vector2(0, 0);
            divRt.anchoredPosition = Vector2.zero;
            divRt.sizeDelta        = new Vector2(0, DividerH);
            divGo.AddComponent<Image>().color = DividerColor;

            // Button with Gmail hover tint
            var btn = rowGo.AddComponent<Button>();
            var captured = card;
            btn.onClick.AddListener(isDeckCard ? () => OnRemoveCard(captured) : () => OnAddCard(captured));
            SetButtonColors(btn, new Color(0, 0, 0, 0), RowHoverColor, new Color(0.20f, 0.17f, 0.30f));
        }

        void SetContentHeight(RectTransform content, int rowCount)
        {
            content.sizeDelta = new Vector2(0, Mathf.Max(rowCount * RowUnitHeight, 0));
        }

        // ── Swap actions ──────────────────────────────────────────────────────

        void OnAddCard(CardData card)
        {
            var (canAdd, reason) = DeckCompositionChecker.CanAdd(card);
            if (!canAdd)
            {
                FlashError(reason);
                return;
            }
            GameManager.Instance.DrawPile.Add(card);
            PopulateColumns();
        }

        void OnRemoveCard(CardData card)
        {
            var gm = GameManager.Instance;
            if (!gm.DrawPile.Remove(card))
                gm.DiscardPile.Remove(card);
            PopulateColumns();
        }

        void OnDone()
        {
            Hide();
            OnComplete.Invoke();
        }

        // ── Error flash ───────────────────────────────────────────────────────

        void FlashError(string reason)
        {
            if (_statsLabel == null) return;
            _statsLabel.text  = reason;
            _statsLabel.color = ErrorColor;
            _flashTimer = 1.5f;
        }

        void Update()
        {
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f) RefreshStats();
            }
        }

        // ── Header refresh ────────────────────────────────────────────────────

        void RefreshHeaders()
        {
            RefreshStats();
            if (_deckHdrLabel != null) _deckHdrLabel.text = DeckHeaderText();
            if (_collHdrLabel != null) _collHdrLabel.text = CollHeaderText();
        }

        void RefreshStats()
        {
            if (_statsLabel == null) return;
            _statsLabel.text  = BuildStatsText();
            _statsLabel.color = _statsBaseColor;
        }

        static string BuildStatsText()
        {
            var deck = GetActiveDeck();
            int uncommons = 0, rares = 0;
            foreach (var c in deck)
            {
                if (c.rarity == CardRarity.Uncommon) uncommons++;
                if (c.rarity == CardRarity.Rare)     rares++;
            }
            return $"DECK {deck.Count}/{DeckCompositionChecker.MaxDeckSize}  " +
                   $"UC {uncommons}/{DeckCompositionChecker.MaxUncommons}  " +
                   $"RARE {rares}/{DeckCompositionChecker.MaxRares}";
        }

        static string DeckHeaderText()
        {
            int n = GetActiveDeck().Count;
            return $"ACTIVE DECK  ({n})";
        }

        static string CollHeaderText() => "COLLECTION  (0)";

        // ── Helpers ───────────────────────────────────────────────────────────

        static List<CardData> GetActiveDeck()
        {
            var gm  = GameManager.Instance;
            var all = new List<CardData>(gm.DrawPile.Count + gm.Hand.Count + gm.DiscardPile.Count);
            all.AddRange(gm.DrawPile);
            all.AddRange(gm.Hand);
            all.AddRange(gm.DiscardPile);
            return all;
        }

        static Color TypeColor(CardType type) => type switch
        {
            CardType.Attack => AttackColor,
            CardType.Defend => DefendColor,
            _               => SpecialColor,
        };

        static Color RarityColor(CardRarity rarity) => rarity switch
        {
            CardRarity.Common   => TextLight,
            CardRarity.Uncommon => new Color(0.18f, 0.62f, 0.28f),
            CardRarity.Rare     => new Color(0.52f, 0.18f, 0.80f),
            _                   => TextLight,
        };

        static void SetButtonColors(Button btn, Color normal, Color hover, Color pressed)
        {
            var cb = btn.colors;
            cb.normalColor      = normal;
            cb.highlightedColor = hover;
            cb.pressedColor     = pressed;
            btn.colors          = cb;
        }

        // Anchored rect helper (for strips / dividers that use a single anchor edge)
        RectTransform MakeRect(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = sizeDelta;
            return rt;
        }

        GameObject MakeLabel(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta,
            string text, int fontSize,
            TextAlignmentOptions alignment,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = sizeDelta;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text               = text;
            tmp.fontSize           = fontSize;
            tmp.color              = color;
            tmp.alignment          = alignment;
            tmp.enableWordWrapping = false;
            if (uiFont != null) tmp.font = uiFont;

            return go;
        }
    }
}
