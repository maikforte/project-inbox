using System.Collections.Generic;
using InboxZero.Core;
using InboxZero.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    public class HandDisplay : MonoBehaviour
    {
        public static HandDisplay Instance { get; private set; }

        [Header("References")]
        public RectTransform cardContainer;

        [Tooltip("Panel shown on hover. Assign in inspector.")]
        public GameObject previewPanel;
        public TextMeshProUGUI previewName;
        public TextMeshProUGUI previewCost;
        public TextMeshProUGUI previewEffect;

        [Header("Sprites")]
        public Sprite cardBackground;

        [Header("Font")]
        public TMP_FontAsset cardFont;

        readonly List<CardView> _cards = new List<CardView>();
        bool _started;

        const float TypeBarHeight = 8f;
        const float Padding       = 4f;
        const float CardW         = 90f;
        const float CardH         = 110f;
        const float MaxSpacing    = 94f;  // natural gap: card width + 4px
        const float MinSpacing    = 30f;  // tightest overlap

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // Disable HorizontalLayoutGroup — cards are positioned manually.
            if (cardContainer != null)
            {
                var hlg = cardContainer.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null) hlg.enabled = false;

                // Deactivate legacy slot GOs (labels + slot backgrounds).
                for (int i = 0; i < cardContainer.childCount; i++)
                    cardContainer.GetChild(i).gameObject.SetActive(false);
            }
        }

        void Start()
        {
            if (TurnManager.Instance != null)
                TurnManager.Instance.OnPlayerTurnStart.AddListener(RefreshHand);
            _started = true;
        }

        void OnEnable()
        {
            if (_started && TurnManager.Instance != null)
                TurnManager.Instance.OnPlayerTurnStart.AddListener(RefreshHand);
        }

        void OnDisable()
        {
            if (TurnManager.Instance != null)
                TurnManager.Instance.OnPlayerTurnStart.RemoveListener(RefreshHand);
        }

        // ── Hand management ───────────────────────────────────────────────────

        public void RefreshHand()
        {
            ClearCards();
            var hand = GameManager.Instance.Hand;
            for (int i = 0; i < hand.Count; i++)
                SpawnCard(hand[i], i, hand.Count);
        }

        public void RemoveCard(CardView view)
        {
            _cards.Remove(view);
            if (view != null) Destroy(view.gameObject);
        }

        /// Removes the card from the tracked list without destroying it —
        /// caller owns the animation + destruction.
        public void DetachCard(CardView view)
        {
            _cards.Remove(view);
        }

        void ClearCards()
        {
            foreach (var c in _cards)
                if (c != null) Destroy(c.gameObject);
            _cards.Clear();
        }

        // ── Card factory ──────────────────────────────────────────────────────

        void SpawnCard(CardData data, int cardIndex, int totalCards)
        {
            if (data == null || cardContainer == null) return;

            // Calculate fan position: spread evenly up to MaxSpacing, overlap beyond 5 cards.
            float containerW = cardContainer.rect.width;
            if (containerW <= 1f) containerW = 500f; // fallback before first layout pass

            float spacing   = totalCards <= 1 ? 0f
                : Mathf.Clamp((containerW - CardW) / (totalCards - 1), MinSpacing, MaxSpacing);
            float totalSpan = CardW + (totalCards - 1) * spacing;
            float x         = -totalSpan * 0.5f + CardW * 0.5f + cardIndex * spacing;

            var go = new GameObject(data.cardName, typeof(RectTransform));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(cardContainer, false);
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(CardW, CardH);
            rt.anchoredPosition = new Vector2(x, 0);

            // Background
            var bg = go.AddComponent<Image>();
            if (cardBackground != null)
            {
                bg.sprite = cardBackground;
                bg.type   = Image.Type.Sliced;
            }
            bg.color = Color.white;

            go.AddComponent<GraphicRaycaster>();

            // Type bar (top strip)
            var barGo = new GameObject("TypeBar", typeof(RectTransform));
            barGo.layer = 5;
            var barRt = barGo.GetComponent<RectTransform>();
            barRt.SetParent(rt, false);
            barRt.anchorMin        = new Vector2(0, 1);
            barRt.anchorMax        = new Vector2(1, 1);
            barRt.pivot            = new Vector2(0.5f, 1f);
            barRt.sizeDelta        = new Vector2(0, TypeBarHeight);
            barRt.anchoredPosition = Vector2.zero;
            var barImg = barGo.AddComponent<Image>();

            // Cost badge (top-left)
            var costGo = MakeText("CostBadge", rt,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(0, 1),
                pivot: new Vector2(0, 1),
                anchoredPos: new Vector2(Padding, -TypeBarHeight - 1),
                size: new Vector2(16, 14),
                text: "?", fontSize: 14);

            // Card name
            var nameGo = MakeText("CardName", rt,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                pivot: new Vector2(0.5f, 1f),
                anchoredPos: new Vector2(0, -TypeBarHeight - Padding),
                size: new Vector2(0, 16),
                text: "NAME", fontSize: 14);
            nameGo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            // Effect text
            var effectGo = MakeText("EffectText", rt,
                anchorMin: new Vector2(0, 0), anchorMax: new Vector2(1, 1),
                pivot: new Vector2(0.5f, 0.5f),
                anchoredPos: new Vector2(0, -20),
                size: new Vector2(-8, -TypeBarHeight - 24),
                text: "", fontSize: 14);
            var effectTmp = effectGo.GetComponent<TextMeshProUGUI>();
            effectTmp.alignment          = TextAlignmentOptions.TopLeft;
            effectTmp.enableWordWrapping = true;

            var view = go.AddComponent<CardView>();
            view.SetReferences(bg, barImg,
                nameGo.GetComponent<TextMeshProUGUI>(),
                costGo.GetComponent<TextMeshProUGUI>(),
                effectTmp);
            view.Populate(data);

            _cards.Add(view);
        }

        // ── Preview panel ─────────────────────────────────────────────────────

        public void ShowPreview(CardData data)
        {
            if (previewPanel == null) return;
            previewPanel.SetActive(true);
            if (previewName   != null) previewName.text   = data.cardName.ToUpper();
            if (previewCost   != null) previewCost.text   = data.apCost + " AP";
            if (previewEffect != null) previewEffect.text = data.effectDescription;
        }

        public void HidePreview()
        {
            if (previewPanel != null) previewPanel.SetActive(false);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        GameObject MakeText(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size, string text, int fontSize)
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
            tmp.text     = text;
            tmp.fontSize = fontSize;
            tmp.color    = Color.white;
            if (cardFont != null) tmp.font = cardFont;

            return go;
        }
    }
}
