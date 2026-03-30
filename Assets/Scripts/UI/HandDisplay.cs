using System.Collections;
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
        public TMP_FontAsset uiFont;

        [Tooltip("Panel shown on hover. Assign in inspector.")]
        public GameObject previewPanel;
        public TextMeshProUGUI previewName;
        public TextMeshProUGUI previewCost;
        public TextMeshProUGUI previewEffect;

        readonly List<CardView> _cards = new List<CardView>();
        bool _started;

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
                TurnManager.Instance.OnPlayerTurnEnd.AddListener(ClearCards);
                TurnManager.Instance.OnPlayerTurnStart.AddListener(RefreshHand);
            }
            _started = true;
        }

        void OnEnable()
        {
            if (_started && TurnManager.Instance != null)
            {
                TurnManager.Instance.OnCombatStart.AddListener(ClearCards);
                TurnManager.Instance.OnPlayerTurnEnd.AddListener(ClearCards);
                TurnManager.Instance.OnPlayerTurnStart.AddListener(RefreshHand);
            }
        }

        void OnDisable()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnCombatStart.RemoveListener(ClearCards);
                TurnManager.Instance.OnPlayerTurnEnd.RemoveListener(ClearCards);
                TurnManager.Instance.OnPlayerTurnStart.RemoveListener(RefreshHand);
            }
        }

        // ── Hand management ───────────────────────────────────────────────────

        /// Appends any cards in GameManager.Hand that don't yet have a CardView,
        /// repositions all cards, then animates newly added ones from the deck.
        public void RefreshHand()
        {
            var hand = GameManager.Instance.Hand;
            int prevCount = _cards.Count;

            for (int i = prevCount; i < hand.Count; i++)
                AppendCard(hand[i]);

            RepositionAll();

            // Animate newly added cards flying in from the deck.
            if (DeckWidget.Instance != null)
            {
                for (int i = prevCount; i < _cards.Count; i++)
                {
                    if (_cards[i] == null) continue;
                    var rt = (RectTransform)_cards[i].transform;
                    Vector2 target = rt.anchoredPosition;
                    StartCoroutine(AnimateCardDraw(rt, target, i - prevCount));
                }
            }
        }

        IEnumerator AnimateCardDraw(RectTransform rt, Vector2 target, int index)
        {
            if (rt == null) yield break;

            // Stagger each card slightly so multiple draws feel sequential.
            if (index > 0) yield return new WaitForSeconds(index * 0.07f);
            if (rt == null) yield break;

            // Teleport card to deck position (in cardContainer local space).
            rt.anchoredPosition = GetDeckLocalPos();

            const float Duration = 0.2f;
            float t = 0f;
            Vector2 start = rt.anchoredPosition;
            while (t < Duration)
            {
                if (rt == null) yield break;
                t += Time.deltaTime;
                float p = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / Duration), 3f); // ease-out cubic
                rt.anchoredPosition = Vector2.Lerp(start, target, p);
                yield return null;
            }

            if (rt != null) rt.anchoredPosition = target;
        }

        Vector2 GetDeckLocalPos()
        {
            var deckRT = DeckWidget.Instance != null
                ? (RectTransform)DeckWidget.Instance.transform
                : null;
            if (deckRT == null || cardContainer == null) return Vector2.zero;

            var canvas = cardContainer.GetComponentInParent<Canvas>();
            var cam    = canvas != null ? canvas.worldCamera : null;

            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, deckRT.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(cardContainer, screen, cam, out Vector2 local);
            return local;
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

            view.Populate(data);
            _cards.Add(view);
        }

        void RepositionAll()
        {
            int total = _cards.Count;
            if (total == 0) return;

            float cardW      = ((RectTransform)_cards[0].transform).rect.width;
            float containerW = cardContainer != null ? cardContainer.rect.width : 500f;
            if (containerW <= 1f) containerW = 500f;

            float spacing   = total <= 1 ? 0f
                : Mathf.Clamp((containerW - cardW) / (total - 1), MinSpacing, MaxSpacing);
            float totalSpan = cardW + (total - 1) * spacing;

            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] == null) continue;
                float x = -totalSpan * 0.5f + cardW * 0.5f + i * spacing;
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
