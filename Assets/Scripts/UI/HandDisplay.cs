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
        [Tooltip("CardSlots container — must have 5 pre-built CardSlot children.")]
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
        const float Padding = 4f;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            // Start runs after all Awakes, so TurnManager.Instance is guaranteed ready
            if (TurnManager.Instance != null)
                TurnManager.Instance.OnPlayerTurnStart.AddListener(RefreshHand);
            _started = true;
        }

        void OnEnable()
        {
            // For subsequent re-enables after Start has already run
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
            int slotIndex = 0;
            foreach (var card in GameManager.Instance.Hand)
                SpawnCard(card, slotIndex++);
        }

        public void RemoveCard(CardView view)
        {
            int idx = _cards.IndexOf(view);
            _cards.Remove(view);
            var slot = view != null ? view.transform.parent : null;
            Destroy(view.gameObject);
            ShowSlotLabel(slot);
        }

        /// Removes the card from the tracked list and reveals the slot label,
        /// but does NOT destroy the GameObject — caller owns the animation + destruction.
        public void DetachCard(CardView view)
        {
            var slot = view != null ? view.transform.parent : null;
            _cards.Remove(view);
            ShowSlotLabel(slot);
        }

        void ClearCards()
        {
            foreach (var c in _cards)
                if (c != null) Destroy(c.gameObject);
            _cards.Clear();

            // Restore all slot number labels
            if (cardContainer == null) return;
            for (int i = 0; i < cardContainer.childCount; i++)
                ShowSlotLabel(cardContainer.GetChild(i));
        }

        // Shows any immediate children of a slot that aren't the card content GO
        void ShowSlotLabel(Transform slot)
        {
            if (slot == null) return;
            for (int i = 0; i < slot.childCount; i++)
                slot.GetChild(i).gameObject.SetActive(true);
        }

        // ── Card factory ──────────────────────────────────────────────────────

        void SpawnCard(CardData data, int slotIndex)
        {
            if (data == null) return;
            if (cardContainer == null || slotIndex >= cardContainer.childCount) return;

            var slot = (RectTransform)cardContainer.GetChild(slotIndex);

            // Hide the slot number label while card is occupying this slot
            for (int i = 0; i < slot.childCount; i++)
                slot.GetChild(i).gameObject.SetActive(false);

            // Card content fills the slot completely
            var go = new GameObject(data.cardName, typeof(RectTransform));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(slot, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            // Background
            var bg = go.AddComponent<Image>();
            if (cardBackground != null) bg.sprite = cardBackground;
            bg.color = new Color(0.18f, 0.18f, 0.28f);

            go.AddComponent<GraphicRaycaster>();

            // Type bar (top strip)
            var barGo = new GameObject("TypeBar", typeof(RectTransform));
            barGo.layer = 5;
            var barRt = barGo.GetComponent<RectTransform>();
            barRt.SetParent(rt, false);
            barRt.anchorMin = new Vector2(0, 1);
            barRt.anchorMax = new Vector2(1, 1);
            barRt.pivot     = new Vector2(0.5f, 1f);
            barRt.sizeDelta = new Vector2(0, TypeBarHeight);
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
            effectTmp.alignment = TextAlignmentOptions.TopLeft;
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
