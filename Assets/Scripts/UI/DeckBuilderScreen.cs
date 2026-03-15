using System;
using System.Collections.Generic;
using InboxZero.Core;
using InboxZero.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Full-screen deck builder shown at Rest Stops and level transitions.
    /// Left column: active deck. Right column: collection pool.
    /// Click a collection card to add it to the deck; click an active deck card to remove it.
    public class DeckBuilderScreen : MonoBehaviour
    {
        public static DeckBuilderScreen Instance { get; private set; }

        [Header("Font")]
        public TMP_FontAsset uiFont;

        /// Fired when the player clicks Done.
        public UnityEvent OnComplete = new UnityEvent();

        // Layout constants (at 640×360)
        const float PanelW      = 290f;
        const float PanelH      = 280f;
        const float ColY        = 20f;   // anchoredPosition Y from centre
        const float RowH        = 18f;
        const float RowSpacing  = 2f;
        const float ColorBarW   = 6f;

        static readonly Color AttackColor  = new Color(0.85f, 0.18f, 0.18f);
        static readonly Color DefendColor  = new Color(0.18f, 0.75f, 0.25f);
        static readonly Color SpecialColor = new Color(0.55f, 0.18f, 0.80f);

        Canvas    _canvas;
        GameObject _panel;

        // Scrollable content roots
        RectTransform _deckContent;
        RectTransform _collectionContent;

        // Header labels updated on every change
        TextMeshProUGUI _headerLabel;

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

        public void Hide()
        {
            if (_panel != null) Destroy(_panel);
            _panel = null;
        }

        // ── Panel construction ────────────────────────────────────────────────

        void BuildPanel()
        {
            if (_canvas == null) { Debug.LogError("[DeckBuilderScreen] No Canvas found."); return; }

            // Root overlay
            _panel = new GameObject("DeckBuilderPanel", typeof(RectTransform));
            _panel.layer = 5;
            var rt = _panel.GetComponent<RectTransform>();
            rt.SetParent(_canvas.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.12f, 0.96f);

            // Title
            MakeLabel("Title", rt,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -14), new Vector2(400, 16),
                "// DECK BUILDER //", 14, TextAlignmentOptions.Center);

            // Header stats (rarity counts, deck size) — updated dynamically
            var headerGo = MakeLabel("Header", rt,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -30), new Vector2(400, 14),
                BuildHeaderText(), 14, TextAlignmentOptions.Center);
            _headerLabel = headerGo.GetComponent<TextMeshProUGUI>();
            _headerLabel.color = new Color(0.75f, 0.75f, 0.75f);

            // Column labels
            MakeLabel("DeckLabel", rt,
                new Vector2(0.25f, 0.5f), new Vector2(0.25f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, PanelH / 2f + 6f), new Vector2(PanelW, 14),
                "ACTIVE DECK", 14, TextAlignmentOptions.Center);

            MakeLabel("CollectionLabel", rt,
                new Vector2(0.75f, 0.5f), new Vector2(0.75f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, PanelH / 2f + 6f), new Vector2(PanelW, 14),
                "COLLECTION", 14, TextAlignmentOptions.Center);

            // Scroll views
            _deckContent       = BuildScrollView("DeckScroll",       rt, new Vector2(0.25f, 0.5f), new Vector2(0, ColY));
            _collectionContent = BuildScrollView("CollectionScroll", rt, new Vector2(0.75f, 0.5f), new Vector2(0, ColY));

            // Done button
            var doneGo = new GameObject("DoneButton", typeof(RectTransform));
            doneGo.layer = 5;
            var donRt = doneGo.GetComponent<RectTransform>();
            donRt.SetParent(rt, false);
            donRt.anchorMin        = new Vector2(0.5f, 0f);
            donRt.anchorMax        = new Vector2(0.5f, 0f);
            donRt.pivot            = new Vector2(0.5f, 0f);
            donRt.anchoredPosition = new Vector2(0, 12);
            donRt.sizeDelta        = new Vector2(100, 22);
            doneGo.AddComponent<Image>().color = new Color(0.12f, 0.35f, 0.18f);
            doneGo.AddComponent<Button>().onClick.AddListener(OnDone);
            var doneLbl = MakeLabel("Label", donRt,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, "DONE", 14, TextAlignmentOptions.Center);
            doneLbl.GetComponent<TextMeshProUGUI>().enableWordWrapping = false;

            PopulateColumns();
        }

        /// Creates a scroll view centred on <paramref name="anchor"/> and returns its content rect.
        RectTransform BuildScrollView(string name, RectTransform parent, Vector2 anchor, Vector2 offset)
        {
            var scrollGo = new GameObject(name, typeof(RectTransform));
            scrollGo.layer = 5;
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.SetParent(parent, false);
            scrollRt.anchorMin        = anchor;
            scrollRt.anchorMax        = anchor;
            scrollRt.pivot            = new Vector2(0.5f, 0.5f);
            scrollRt.sizeDelta        = new Vector2(PanelW, PanelH);
            scrollRt.anchoredPosition = offset;

            var bgImg = scrollGo.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.10f, 0.18f);

            // Viewport (mask)
            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            viewportGo.layer = 5;
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.SetParent(scrollRt, false);
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = new Vector2(2, 2);
            viewportRt.offsetMax = new Vector2(-2, -2);
            viewportGo.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            viewportGo.AddComponent<Mask>().showMaskGraphic = false;

            // Content
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.layer = 5;
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.SetParent(viewportRt, false);
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0, 0);

            // ScrollRect
            var sr = scrollGo.AddComponent<ScrollRect>();
            sr.content          = contentRt;
            sr.viewport         = viewportRt;
            sr.horizontal       = false;
            sr.vertical         = true;
            sr.scrollSensitivity = 20f;
            sr.movementType     = ScrollRect.MovementType.Clamped;

            return contentRt;
        }

        // ── Column population ─────────────────────────────────────────────────

        void PopulateColumns()
        {
            // Clear existing rows
            foreach (Transform child in _deckContent)       Destroy(child.gameObject);
            foreach (Transform child in _collectionContent) Destroy(child.gameObject);

            var gm   = GameManager.Instance;
            var deck = GetActiveDeck();

            // Active deck column
            for (int i = 0; i < deck.Count; i++)
            {
                var card = deck[i];
                AddCardRow(_deckContent, card, i, isDeckCard: true);
            }
            SetContentHeight(_deckContent, deck.Count);

            // Collection column
            for (int i = 0; i < gm.CardCollection.Count; i++)
            {
                var card = gm.CardCollection[i];
                AddCardRow(_collectionContent, card, i, isDeckCard: false);
            }
            SetContentHeight(_collectionContent, gm.CardCollection.Count);

            UpdateHeader();
        }

        void AddCardRow(RectTransform parent, CardData card, int index, bool isDeckCard)
        {
            float yPos = -index * (RowH + RowSpacing) - RowSpacing;

            var rowGo = new GameObject(card.cardName, typeof(RectTransform));
            rowGo.layer = 5;
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.SetParent(parent, false);
            rowRt.anchorMin        = new Vector2(0, 1);
            rowRt.anchorMax        = new Vector2(1, 1);
            rowRt.pivot            = new Vector2(0.5f, 1f);
            rowRt.anchoredPosition = new Vector2(0, yPos);
            rowRt.sizeDelta        = new Vector2(0, RowH);

            // Row background
            var rowImg = rowGo.AddComponent<Image>();
            rowImg.color = new Color(0.12f, 0.14f, 0.22f);

            // Type color bar
            var barGo = new GameObject("Bar", typeof(RectTransform));
            barGo.layer = 5;
            var barRt = barGo.GetComponent<RectTransform>();
            barRt.SetParent(rowRt, false);
            barRt.anchorMin        = new Vector2(0, 0);
            barRt.anchorMax        = new Vector2(0, 1);
            barRt.pivot            = new Vector2(0, 0.5f);
            barRt.anchoredPosition = Vector2.zero;
            barRt.sizeDelta        = new Vector2(ColorBarW, 0);
            barGo.AddComponent<Image>().color = TypeColor(card.cardType);

            // Card name
            MakeLabel("Name", rowRt,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f),
                new Vector2(ColorBarW + 4f, 0), new Vector2(-(ColorBarW + 4f + 60f), 0),
                card.cardName.ToUpper(), 14, TextAlignmentOptions.MidlineLeft);

            // Rarity
            var rarityColor = RarityColor(card.rarity);
            var rarGo = MakeLabel("Rarity", rowRt,
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f),
                new Vector2(-26f, 0), new Vector2(44f, 0),
                card.rarity.ToString().ToUpper(), 14, TextAlignmentOptions.MidlineRight);
            rarGo.GetComponent<TextMeshProUGUI>().color = rarityColor;

            // AP cost
            MakeLabel("Cost", rowRt,
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f),
                new Vector2(-2f, 0), new Vector2(20f, 0),
                card.apCost.ToString(), 14, TextAlignmentOptions.MidlineRight);

            // Button overlay
            var btn = rowGo.AddComponent<Button>();
            var captured = card;
            if (isDeckCard)
                btn.onClick.AddListener(() => OnRemoveCard(captured));
            else
                btn.onClick.AddListener(() => OnAddCard(captured));

            // Hover tint via color block
            var cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(0.7f, 0.9f, 1f);
            cb.pressedColor     = new Color(0.5f, 0.7f, 0.9f);
            btn.colors = cb;
        }

        void SetContentHeight(RectTransform content, int rowCount)
        {
            float h = rowCount * (RowH + RowSpacing) + RowSpacing;
            content.sizeDelta = new Vector2(0, Mathf.Max(h, 0));
        }

        // ── Actions ───────────────────────────────────────────────────────────

        void OnAddCard(CardData card)
        {
            var (canAdd, reason) = DeckCompositionChecker.CanAdd(card);
            if (!canAdd)
            {
                // Flash the header with the reason
                if (_headerLabel != null)
                {
                    _headerLabel.text  = reason;
                    _headerLabel.color = new Color(1f, 0.35f, 0.35f);
                    // Reset after a short delay via coroutine substitute: schedule in Update
                    _flashTimer = 1.2f;
                }
                return;
            }

            var gm = GameManager.Instance;
            gm.CardCollection.Remove(card);
            gm.DrawPile.Add(card);
            PopulateColumns();
        }

        void OnRemoveCard(CardData card)
        {
            var gm = GameManager.Instance;
            // Only allow removing from draw pile (not from hand mid-combat — builder is shown outside combat)
            if (!gm.DrawPile.Remove(card))
                gm.DiscardPile.Remove(card);
            gm.CardCollection.Add(card);
            PopulateColumns();
        }

        void OnDone()
        {
            Hide();
            OnComplete.Invoke();
        }

        // ── Flash timer ───────────────────────────────────────────────────────

        float _flashTimer;

        void Update()
        {
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f && _headerLabel != null)
                {
                    _headerLabel.text  = BuildHeaderText();
                    _headerLabel.color = new Color(0.75f, 0.75f, 0.75f);
                }
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        void UpdateHeader()
        {
            if (_headerLabel != null)
            {
                _headerLabel.text  = BuildHeaderText();
                _headerLabel.color = new Color(0.75f, 0.75f, 0.75f);
            }
        }

        static string BuildHeaderText()
        {
            var deck = GetActiveDeck();
            int uncommons = 0, rares = 0;
            foreach (var c in deck)
            {
                if (c.rarity == CardRarity.Uncommon) uncommons++;
                if (c.rarity == CardRarity.Rare)     rares++;
            }
            return $"DECK: {deck.Count}/{DeckCompositionChecker.MaxDeckSize}   " +
                   $"UNCOMMON: {uncommons}/{DeckCompositionChecker.MaxUncommons}   " +
                   $"RARE: {rares}/{DeckCompositionChecker.MaxRares}";
        }

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
            CardType.Attack  => AttackColor,
            CardType.Defend  => DefendColor,
            _                => SpecialColor,
        };

        static Color RarityColor(CardRarity rarity) => rarity switch
        {
            CardRarity.Common   => new Color(0.75f, 0.75f, 0.75f),
            CardRarity.Uncommon => new Color(0.40f, 0.85f, 0.40f),
            CardRarity.Rare     => new Color(0.75f, 0.40f, 1.00f),
            _                   => Color.white,
        };

        GameObject MakeLabel(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size, string text, int fontSize,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left)
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
            tmp.text               = text;
            tmp.fontSize           = fontSize;
            tmp.color              = Color.white;
            tmp.alignment          = alignment;
            tmp.enableWordWrapping = false;
            if (uiFont != null) tmp.font = uiFont;

            return go;
        }
    }
}
