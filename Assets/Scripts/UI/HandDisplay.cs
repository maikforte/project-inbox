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
        public CardView cardPrefab;

        [Tooltip("Panel shown on hover. Assign in inspector.")]
        public GameObject previewPanel;
        public TextMeshProUGUI previewName;
        public TextMeshProUGUI previewCost;
        public TextMeshProUGUI previewEffect;

        readonly List<CardView> _cards = new List<CardView>();
        bool _started;

        const float CardWidth   = 90f;
        const float CardHeight  = 110f;
        const float MinSpacing  = 30f;  // tightest overlap
        const float MaxSpacing  = 94f;  // natural gap: card width + 4px

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
            if (cardPrefab == null)
            {
                Debug.LogWarning("[HandDisplay] cardPrefab is not assigned.");
                return;
            }

            float cardW = CardWidth;

            // Calculate fan position.
            float containerW = cardContainer.rect.width;
            if (containerW <= 1f) containerW = 500f; // fallback before first layout pass

            float spacing   = totalCards <= 1 ? 0f
                : Mathf.Clamp((containerW - cardW) / (totalCards - 1), MinSpacing, MaxSpacing);
            float totalSpan = cardW + (totalCards - 1) * spacing;
            float x         = -totalSpan * 0.5f + cardW * 0.5f + cardIndex * spacing;

            var view = Instantiate(cardPrefab, cardContainer);
            var rt   = (RectTransform)view.transform;
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(CardWidth, CardHeight);
            rt.anchoredPosition = new Vector2(x, 0);

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
    }
}
