using System.Collections.Generic;
using InboxZero.Core;
using InboxZero.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Read-only "All Mail" compendium screen — shows every card in the game.
    /// Cards the player owns (deck + collection) are shown normally;
    /// unacquired cards are dimmed with effect text replaced by "???".
    public class AllMailScreen : MonoBehaviour
    {
        public static AllMailScreen Instance { get; private set; }

        [Header("Font")]
        public TMP_FontAsset uiFont;

        [Header("Card Registry")]
        [Tooltip("Assign the AllCardsRegistry SO here.")]
        public AllCardsRegistry registry;

        /// Fired when the player clicks Close.
        public UnityEvent OnClose = new UnityEvent();

        // ── Gmail colour palette (matches DeckBuilderScreen) ──────────────────
        static readonly Color BgColor      = new Color(0.961f, 0.961f, 0.961f);
        static readonly Color TopBarColor  = new Color(0.914f, 0.941f, 0.984f);
        static readonly Color ColHeaderBg  = new Color(0.930f, 0.930f, 0.930f);
        static readonly Color RowBg        = new Color(0.996f, 0.996f, 0.996f);
        static readonly Color DividerColor = new Color(0.855f, 0.855f, 0.855f);
        static readonly Color AccentColor  = new Color(0.102f, 0.451f, 0.910f);
        static readonly Color TextDark     = new Color(0.13f,  0.13f,  0.13f);
        static readonly Color TextMedium   = new Color(0.40f,  0.40f,  0.40f);
        static readonly Color TextLight    = new Color(0.60f,  0.60f,  0.60f);

        static readonly Color AttackColor  = new Color(0.83f, 0.18f, 0.18f);
        static readonly Color DefendColor  = new Color(0.20f, 0.66f, 0.32f);
        static readonly Color SpecialColor = new Color(0.52f, 0.18f, 0.80f);

        // ── Layout constants ──────────────────────────────────────────────────
        const float TopBarH      = 26f;
        const float DividerH     = 1f;
        const float ColHeaderH   = 18f;
        const float RowH         = 26f;
        const float RowGap       = 1f;
        const float DotSize      = 6f;
        const float NameW        = 104f;
        const float PreviewW     = 220f;
        const float RarityW      = 60f;
        const float CostW        = 18f;
        const float MarginL      = 10f;
        const float MarginR      = 6f;
        const float UnownedAlpha = 0.35f;

        Canvas     _canvas;
        GameObject _panel;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _canvas  = FindObjectOfType<Canvas>();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Show()
        {
            if (_panel != null) Destroy(_panel);
            BuildPanel();
        }

        public void Hide()
        {
            if (_panel != null) Destroy(_panel);
            _panel = null;
        }

        // ── Panel construction ────────────────────────────────────────────────

        void BuildPanel()
        {
            if (_canvas == null) { Debug.LogError("[AllMailScreen] No Canvas found."); return; }
            if (registry == null)
            {
                Debug.LogWarning("[AllMailScreen] No AllCardsRegistry assigned.");
                return;
            }

            _panel = new GameObject("AllMailPanel", typeof(RectTransform));
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

            MakeLabel("Title", topBarRt,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                new Vector2(MarginL, 0), new Vector2(120, 0),
                "< ALL MAIL", 14, TextAlignmentOptions.MidlineLeft, AccentColor);

            // Owned / total count (centre)
            var owned      = GetOwnedCards();
            int total      = registry.allCards.Count;
            int ownedCount = 0;
            foreach (var c in registry.allCards)
                if (owned.Contains(c)) ownedCount++;

            MakeLabel("Stats", topBarRt,
                new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(200, 0),
                $"ALL MAIL  {ownedCount} / {total}", 14, TextAlignmentOptions.Center, TextMedium);

            // CLOSE button
            var closeBtnGo = new GameObject("CloseButton", typeof(RectTransform));
            closeBtnGo.layer = 5;
            var closeBtnRt  = closeBtnGo.GetComponent<RectTransform>();
            closeBtnRt.SetParent(topBarRt, false);
            closeBtnRt.anchorMin        = new Vector2(1, 0.5f);
            closeBtnRt.anchorMax        = new Vector2(1, 0.5f);
            closeBtnRt.pivot            = new Vector2(1, 0.5f);
            closeBtnRt.anchoredPosition = new Vector2(-MarginR, 0);
            closeBtnRt.sizeDelta        = new Vector2(52, 18);
            closeBtnGo.AddComponent<Image>().color = AccentColor;
            var closeBtn = closeBtnGo.AddComponent<Button>();
            closeBtn.onClick.AddListener(OnCloseClicked);
            SetButtonColors(closeBtn,
                AccentColor,
                new Color(0.15f, 0.52f, 0.98f),
                new Color(0.08f, 0.38f, 0.80f));
            var closeLbl = MakeLabel("Label", closeBtnRt,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                "CLOSE", 14, TextAlignmentOptions.Center, Color.white);
            closeLbl.GetComponent<TextMeshProUGUI>().enableWordWrapping = false;

            // ── Divider under top bar ──────────────────────────────────────────
            MakeRect("HDiv", rt,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f),
                new Vector2(0, -(TopBarH + DividerH)), new Vector2(0, DividerH))
                .gameObject.AddComponent<Image>().color = DividerColor;

            // ── Column header ─────────────────────────────────────────────────
            float colHeaderY = -(TopBarH + DividerH + ColHeaderH);
            var hdrRt = MakeRect("Header", rt,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0f, 1f),
                new Vector2(0, colHeaderY), new Vector2(0, ColHeaderH));
            hdrRt.gameObject.AddComponent<Image>().color = ColHeaderBg;
            MakeLabel("Label", hdrRt,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f),
                new Vector2(MarginL, 0), new Vector2(0, 0),
                $"ALL CARDS  ({total})", 14, TextAlignmentOptions.MidlineLeft, TextDark);

            MakeRect("ColHDiv", rt,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f),
                new Vector2(0, -(TopBarH + DividerH + ColHeaderH + DividerH)), new Vector2(0, DividerH))
                .gameObject.AddComponent<Image>().color = DividerColor;

            // ── Scrollable card list ───────────────────────────────────────────
            float listTop = TopBarH + DividerH + ColHeaderH + DividerH;
            var content = BuildScrollView("Scroll", rt,
                new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(0, 0), new Vector2(0, -listTop));

            // Sort: Starter → Common → Uncommon → Rare, then alphabetically
            var cards = new List<CardData>(registry.allCards);
            cards.Sort((a, b) =>
            {
                int diff = ((int)a.rarity).CompareTo((int)b.rarity);
                if (diff != 0) return diff;
                return string.Compare(a.cardName, b.cardName, System.StringComparison.Ordinal);
            });

            for (int i = 0; i < cards.Count; i++)
                AddCardRow(content, cards[i], i, owned.Contains(cards[i]));

            content.sizeDelta = new Vector2(0, Mathf.Max(cards.Count * (RowH + RowGap), 0));
        }

        // ── Scroll view ───────────────────────────────────────────────────────

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

            var viewGo = new GameObject("Viewport", typeof(RectTransform));
            viewGo.layer = 5;
            var viewRt = viewGo.GetComponent<RectTransform>();
            viewRt.SetParent(scrollRt, false);
            viewRt.anchorMin = Vector2.zero;
            viewRt.anchorMax = Vector2.one;
            viewRt.offsetMin = Vector2.zero;
            viewRt.offsetMax = Vector2.zero;
            viewGo.AddComponent<RectMask2D>();

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

        // ── Card row ──────────────────────────────────────────────────────────

        void AddCardRow(RectTransform parent, CardData card, int index, bool isOwned)
        {
            float yPos = -index * (RowH + RowGap);
            float alpha = isOwned ? 1f : UnownedAlpha;

            Color Fade(Color c) => new Color(c.r, c.g, c.b, c.a * alpha);

            var rowGo = new GameObject(card.cardName, typeof(RectTransform));
            rowGo.layer = 5;
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.SetParent(parent, false);
            rowRt.anchorMin        = new Vector2(0, 1);
            rowRt.anchorMax        = new Vector2(1, 1);
            rowRt.pivot            = new Vector2(0, 1);
            rowRt.anchoredPosition = new Vector2(0, yPos);
            rowRt.sizeDelta        = new Vector2(0, RowH);
            rowGo.AddComponent<Image>().color = RowBg;

            // Type-color dot
            var dotGo = new GameObject("Dot", typeof(RectTransform));
            dotGo.layer = 5;
            var dotRt = dotGo.GetComponent<RectTransform>();
            dotRt.SetParent(rowRt, false);
            dotRt.anchorMin        = new Vector2(0, 0.5f);
            dotRt.anchorMax        = new Vector2(0, 0.5f);
            dotRt.pivot            = new Vector2(0, 0.5f);
            dotRt.anchoredPosition = new Vector2(MarginL, 0);
            dotRt.sizeDelta        = new Vector2(DotSize, DotSize);
            dotGo.AddComponent<Image>().color = Fade(isOwned ? TypeColor(card.cardType) : TextLight);

            // Card name
            MakeLabel("Name", rowRt,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                new Vector2(MarginL + DotSize + 6f, 0), new Vector2(NameW, 0),
                card.cardName.ToUpper(), 14, TextAlignmentOptions.MidlineLeft, Fade(TextDark));

            // Effect description or "???" if unowned
            MakeLabel("Preview", rowRt,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                new Vector2(MarginL + DotSize + 6f + NameW + 6f, 0), new Vector2(PreviewW, 0),
                isOwned ? card.effectDescription : "???", 14,
                TextAlignmentOptions.MidlineLeft, Fade(TextLight));

            // Rarity badge
            MakeLabel("Rarity", rowRt,
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f),
                new Vector2(-(CostW + MarginR + 4f), 0), new Vector2(RarityW, 0),
                card.rarity.ToString().ToUpper(), 14, TextAlignmentOptions.MidlineRight,
                Fade(isOwned ? RarityColor(card.rarity) : TextLight));

            // AP cost
            MakeLabel("Cost", rowRt,
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f),
                new Vector2(-MarginR, 0), new Vector2(CostW, 0),
                card.apCost.ToString(), 14, TextAlignmentOptions.MidlineRight, Fade(TextMedium));

            // Row bottom divider
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
        }

        // ── Event handler ─────────────────────────────────────────────────────

        void OnCloseClicked()
        {
            Hide();
            OnClose.Invoke();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        static HashSet<CardData> GetOwnedCards()
        {
            var gm    = GameManager.Instance;
            var owned = new HashSet<CardData>();
            owned.UnionWith(gm.DrawPile);
            owned.UnionWith(gm.Hand);
            owned.UnionWith(gm.DiscardPile);
            owned.UnionWith(gm.CardCollection);
            return owned;
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

        static void SetButtonColors(Button btn, Color normal, Color hover, Color pressed)
        {
            var cb = btn.colors;
            cb.normalColor      = normal;
            cb.highlightedColor = hover;
            cb.pressedColor     = pressed;
            btn.colors          = cb;
        }

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
            TextAlignmentOptions alignment, Color color)
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
