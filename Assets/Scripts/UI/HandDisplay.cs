using System.Collections.Generic;
using InboxZero.Core;
using InboxZero.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace InboxZero.UI
{
    public class HandDisplay : MonoBehaviour
    {
        public static HandDisplay Instance { get; private set; }

        [Header("References")]
        public RectTransform cardContainer;
        public CardView cardPrefab;
        public TMP_FontAsset uiFont;

        [Tooltip("Panel shown on hover. Assign in inspector.")]
        public GameObject previewPanel;
        public TextMeshProUGUI previewName;
        public TextMeshProUGUI previewCost;
        public TextMeshProUGUI previewEffect;

        readonly List<CardView> _cards = new List<CardView>();
        bool        _started;
        GameObject  _overflowPrompt;

        public bool IsDiscardMode { get; private set; }

        const float CardWidth   = 48f;
        const float CardHeight  = 64f;
        const float MinSpacing  = 30f;
        const float MaxSpacing  = 94f;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (cardContainer != null)
            {
                var hlg = cardContainer.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null) hlg.enabled = false;

                for (int i = 0; i < cardContainer.childCount; i++)
                    cardContainer.GetChild(i).gameObject.SetActive(false);
            }
        }

        void Start()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnCombatStart.AddListener(ClearCards);
                TurnManager.Instance.OnOverflowDiscard.AddListener(EnterDiscardMode);
                TurnManager.Instance.OnPlayerTurnStart.AddListener(RefreshHand);
            }
            _started = true;
        }

        void OnEnable()
        {
            if (_started && TurnManager.Instance != null)
            {
                TurnManager.Instance.OnCombatStart.AddListener(ClearCards);
                TurnManager.Instance.OnOverflowDiscard.AddListener(EnterDiscardMode);
                TurnManager.Instance.OnPlayerTurnStart.AddListener(RefreshHand);
            }
        }

        void OnDisable()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnCombatStart.RemoveListener(ClearCards);
                TurnManager.Instance.OnOverflowDiscard.RemoveListener(EnterDiscardMode);
                TurnManager.Instance.OnPlayerTurnStart.RemoveListener(RefreshHand);
            }
        }

        // ── Hand management ───────────────────────────────────────────────────

        /// Appends any cards in GameManager.Hand that don't yet have a CardView,
        /// then repositions all cards. Called every turn start.
        public void RefreshHand()
        {
            var hand = GameManager.Instance.Hand;

            // Append views for newly drawn cards (hand grew since last refresh).
            for (int i = _cards.Count; i < hand.Count; i++)
                AppendCard(hand[i]);

            RepositionAll();
        }

        public void RemoveCard(CardView view)
        {
            _cards.Remove(view);
            if (view != null) Destroy(view.gameObject);
            RepositionAll();
        }

        /// Removes the card from the tracked list without destroying it —
        /// caller owns the animation + destruction.
        public void DetachCard(CardView view)
        {
            _cards.Remove(view);
        }

        public void ClearCards()
        {
            foreach (var c in _cards)
                if (c != null) Destroy(c.gameObject);
            _cards.Clear();
        }

        // ── Overflow discard mode ─────────────────────────────────────────────

        void EnterDiscardMode()
        {
            IsDiscardMode = true;
            foreach (var cv in _cards)
                cv?.SetHighlight(true);
            BuildOverflowPrompt();
        }

        void ExitDiscardMode()
        {
            IsDiscardMode = false;
            foreach (var cv in _cards)
                cv?.SetHighlight(false);
            if (_overflowPrompt != null) { Destroy(_overflowPrompt); _overflowPrompt = null; }
        }

        /// Called by CardView when IsDiscardMode is true.
        public void DiscardFromHand(CardView view)
        {
            ExitDiscardMode();
            DeckManager.Instance.DiscardCard(view.Data);
            _cards.Remove(view);
            if (view != null) Destroy(view.gameObject);
            TurnManager.Instance.CompleteOverflowDiscard();
        }

        void BuildOverflowPrompt()
        {
            var canvas = cardContainer.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            _overflowPrompt = new GameObject("OverflowPrompt", typeof(RectTransform));
            _overflowPrompt.layer = 5;
            var rt = _overflowPrompt.GetComponent<RectTransform>();
            rt.SetParent(canvas.transform, false);
            rt.SetAsLastSibling();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(0f, 18f);
            _overflowPrompt.AddComponent<Image>().color = new Color(0.65f, 0.08f, 0.08f, 0.92f);

            var txtGo = new GameObject("Txt", typeof(RectTransform));
            txtGo.layer = 5;
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.SetParent(rt, false);
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text               = "HAND FULL  --  CLICK A CARD TO DISCARD IT";
            tmp.fontSize           = 14;
            tmp.color              = Color.white;
            tmp.alignment          = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            if (uiFont != null) tmp.font = uiFont;
        }

        // ── Card factory ──────────────────────────────────────────────────────

        void AppendCard(CardData data)
        {
            if (data == null || cardContainer == null) return;
            if (cardPrefab == null)
            {
                Debug.LogWarning("[HandDisplay] cardPrefab is not assigned.");
                return;
            }

            var view = Instantiate(cardPrefab, cardContainer);
            var rt   = (RectTransform)view.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(CardWidth, CardHeight);

            view.Populate(data);
            _cards.Add(view);
        }

        void RepositionAll()
        {
            int   total      = _cards.Count;
            float containerW = cardContainer != null ? cardContainer.rect.width : 500f;
            if (containerW <= 1f) containerW = 500f;

            float spacing   = total <= 1 ? 0f
                : Mathf.Clamp((containerW - CardWidth) / (total - 1), MinSpacing, MaxSpacing);
            float totalSpan = CardWidth + (total - 1) * spacing;

            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] == null) continue;
                float x = -totalSpan * 0.5f + CardWidth * 0.5f + i * spacing;
                ((RectTransform)_cards[i].transform).anchoredPosition = new Vector2(x, 0);
            }
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
