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

        [Header("Hover Lift")]
        [Tooltip("Pixels the hovered card rises.")]
        public float hoverLiftPrimary = 18f;
        [Tooltip("Pixels the immediate neighbours rise.")]
        public float hoverLiftAdjacent = 9f;

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
                TurnManager.Instance.OnPlayerTurnEnd.AddListener(StartDiscardAnimation);
                TurnManager.Instance.OnPlayerTurnStart.AddListener(RefreshHand);
            }
            _started = true;
        }

        void OnEnable()
        {
            if (_started && TurnManager.Instance != null)
            {
                TurnManager.Instance.OnCombatStart.AddListener(ClearCards);
                TurnManager.Instance.OnPlayerTurnEnd.AddListener(StartDiscardAnimation);
                TurnManager.Instance.OnPlayerTurnStart.AddListener(RefreshHand);
            }
        }

        void OnDisable()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnCombatStart.RemoveListener(ClearCards);
                TurnManager.Instance.OnPlayerTurnEnd.RemoveListener(StartDiscardAnimation);
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

            StartCoroutine(AnimateNewCards(prevCount));
        }

        IEnumerator AnimateNewCards(int prevCount)
        {
            // Hide new cards immediately so they don't flash at their spawn position.
            for (int i = prevCount; i < _cards.Count; i++)
                if (_cards[i] != null) _cards[i].transform.localScale = Vector3.zero;

            // Wait one frame so RectTransform layout has calculated card widths.
            yield return null;

            RepositionAll();

            for (int i = prevCount; i < _cards.Count; i++)
            {
                if (_cards[i] == null) continue;
                var rt = (RectTransform)_cards[i].transform;
                Vector2 target = rt.anchoredPosition;
                StartCoroutine(AnimateCardDraw(rt, target, i - prevCount, _cards[i]));
            }
        }

        IEnumerator AnimateCardDraw(RectTransform rt, Vector2 target, int index, CardView owner = null)
        {
            if (rt == null) yield break;

            // Stagger each card slightly so multiple draws feel sequential.
            if (index > 0) yield return new WaitForSeconds(index * 0.07f);
            if (rt == null) yield break;

            // Start at deck position, scaled down to roughly deck-card size.
            rt.anchoredPosition = GetDeckLocalPos();
            rt.localScale = Vector3.one * 0.15f;

            const float Duration = 0.32f;
            float t = 0f;
            Vector2 start = rt.anchoredPosition;

            while (t < Duration)
            {
                // If the card was played mid-draw, stop so the play animation owns the transform.
                if (rt == null || (owner != null && owner.IsBeingPlayed)) yield break;
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / Duration);

                // Ease-out cubic — same curve drives both position and scale
                // so the card feels like it's physically expanding as it arrives.
                float curve = 1f - Mathf.Pow(1f - p, 3f);
                rt.anchoredPosition = Vector2.Lerp(start, target, curve);
                rt.localScale       = Vector3.one * Mathf.Lerp(0.15f, 1f, curve);

                yield return null;
            }

            if (rt != null && (owner == null || !owner.IsBeingPlayed))
            {
                rt.anchoredPosition = target;
                rt.localScale       = Vector3.one;
            }
        }

        // ── Discard animation ─────────────────────────────────────────────────

        void StartDiscardAnimation() => StartCoroutine(AnimateDiscardAll());

        IEnumerator AnimateDiscardAll()
        {
            var toDiscard = new List<CardView>(_cards);
            _cards.Clear();  // detach immediately so hand logic sees an empty hand

            for (int i = 0; i < toDiscard.Count; i++)
            {
                if (toDiscard[i] == null) continue;
                StartCoroutine(AnimateCardToArchive(toDiscard[i], i));
            }
            yield break;
        }

        IEnumerator AnimateCardToArchive(CardView card, int staggerIndex)
        {
            if (staggerIndex > 0)
                yield return new WaitForSeconds(staggerIndex * 0.04f);

            if (card == null) yield break;

            // Stop the float bob so it doesn't fight the discard animation.
            var cf = card.GetComponent<CardFloat>();
            if (cf != null) cf.enabled = false;

            var rt = (RectTransform)card.transform;
            Vector2 start      = rt.anchoredPosition;
            Vector2 archivePos = GetArchiveLocalPos();
            Vector3 startScale = rt.localScale;

            const float Duration = 0.25f;
            float t = 0f;

            while (t < Duration)
            {
                if (rt == null) yield break;
                t += Time.deltaTime;
                float p     = Mathf.Clamp01(t / Duration);
                float curve = p * p;  // ease-in: accelerates into the archive

                rt.anchoredPosition = Vector2.Lerp(start, archivePos, curve);
                rt.localScale       = Vector3.Lerp(startScale, Vector3.one * 0.15f, curve);
                yield return null;
            }

            if (card != null) Destroy(card.gameObject);
        }

        Vector2 GetArchiveLocalPos()
        {
            if (cardContainer == null) return Vector2.zero;

            if (ArchiveWidget.Instance != null)
            {
                Vector3 world = ArchiveWidget.Instance.transform.position;
                return cardContainer.InverseTransformPoint(world);
            }

            // Fallback: bottom-left corner of the container.
            Rect r = cardContainer.rect;
            return new Vector2(r.xMin, r.yMin - 60f);
        }

        Vector2 GetDeckLocalPos()
        {
            if (cardContainer == null) return Vector2.zero;

            if (DeckWidget.Instance != null)
            {
                // Convert the deck widget's world position directly into
                // cardContainer local space — no screen-space round-trip.
                Vector3 world = DeckWidget.Instance.transform.position;
                return cardContainer.InverseTransformPoint(world);
            }

            // Fallback: bottom-right corner of the container.
            Rect r = cardContainer.rect;
            return new Vector2(r.xMax, r.yMin - 60f);
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
                var slotPos = new Vector2(x, 0);
                ((RectTransform)_cards[i].transform).anchoredPosition = slotPos;
                _cards[i].HandAnchoredPosition = slotPos;
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

        // ── Hover lift ────────────────────────────────────────────────────────

        public void OnCardHoverEnter(CardView card)
        {
            int idx = _cards.IndexOf(card);
            if (idx < 0) return;
            ApplyHoverLift(idx);
        }

        public void OnCardHoverExit(CardView card)
        {
            ApplyHoverLift(-1);
        }

        void ApplyHoverLift(int hoveredIdx)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] == null) continue;
                var cf = _cards[i].GetComponent<CardFloat>();
                if (cf == null) continue;

                float lift = 0f;
                if (hoveredIdx >= 0)
                {
                    int dist = Mathf.Abs(i - hoveredIdx);
                    lift = dist == 0 ? hoverLiftPrimary
                         : dist == 1 ? hoverLiftAdjacent
                         : 0f;
                }
                cf.SetLiftTarget(lift);
            }
        }
    }
}
