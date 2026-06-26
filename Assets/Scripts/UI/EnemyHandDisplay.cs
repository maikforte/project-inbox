using System.Collections;
using InboxZero.Data;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Displays face-down card backs at the top of the screen and plays a
    /// card-reveal animation using the same CardView prefab as the player.
    public class EnemyHandDisplay : MonoBehaviour
    {
        public static EnemyHandDisplay Instance { get; private set; }

        [SerializeField] RectTransform cardContainer;
        [SerializeField] CardView      cardPrefab;
        [SerializeField] Sprite        cardBackSprite;

        const float CardWidth  = 90f;
        const float CardHeight = 110f;
        const float MinSpacing = 30f;
        const float MaxSpacing = 94f;

        const float AnimStartY = 115f;   // canvas-space: near top hand area
        const float AnimLandY  = 20f;    // canvas-space: slightly above center

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Debug.Log("[EnemyHandDisplay] Awake — Instance set.");
        }

        // ── Face-down hand display ────────────────────────────────────────────

        public void Refresh(int count)
        {
            if (cardContainer == null) return;

            for (int i = cardContainer.childCount - 1; i >= 0; i--)
                Destroy(cardContainer.GetChild(i).gameObject);

            if (count <= 0) return;

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("CardBack", typeof(RectTransform));
                go.layer = 5;
                go.transform.SetParent(cardContainer, false);

                var rt = (RectTransform)go.transform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(CardWidth, CardHeight);

                go.AddComponent<CanvasRenderer>();
                var img = go.AddComponent<Image>();
                img.color         = Color.white;
                img.raycastTarget = false;
                if (cardBackSprite != null) img.sprite = cardBackSprite;
            }

            RepositionAll();
        }

        public void Hide()
        {
            if (cardContainer == null) return;
            for (int i = cardContainer.childCount - 1; i >= 0; i--)
                Destroy(cardContainer.GetChild(i).gameObject);
        }

        // ── Card-play animation ───────────────────────────────────────────────

        public Coroutine PlayCardAnimation(CardData card) =>
            StartCoroutine(PlayCardRoutine(card));

        IEnumerator PlayCardRoutine(CardData card)
        {
            if (card == null)
            {
                Debug.LogWarning("[EnemyHandDisplay] PlayCardRoutine: card is null.");
                yield break;
            }

            var canvasRoot = transform.parent;
            if (canvasRoot == null)
            {
                Debug.LogWarning("[EnemyHandDisplay] PlayCardRoutine: canvasRoot is null.");
                yield break;
            }

            if (cardPrefab == null)
            {
                Debug.LogWarning("[EnemyHandDisplay] PlayCardRoutine: cardPrefab not assigned.");
                yield break;
            }

            Debug.Log($"[EnemyHandDisplay] Playing card animation: {card.cardName}");

            // ── Instantiate the player CardView prefab ────────────────────────
            var view = Instantiate(cardPrefab, canvasRoot);
            view.transform.SetAsLastSibling();

            var rt = (RectTransform)view.transform;
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(CardWidth, CardHeight);
            rt.anchoredPosition = new Vector2(0f, AnimStartY);

            // Disable interaction — this card is display-only.
            var cg = view.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            view.Populate(card);

            // Start squished (face-down) — flips open as it slides down.
            rt.localScale = new Vector3(1f, 0f, 1f);

            // ── Phase 1: slide down + flip reveal ─────────────────────────────
            Vector2 startPos = rt.anchoredPosition;
            Vector2 landPos  = new Vector2(0f, AnimLandY);
            const float SlideDur = 0.28f;
            for (float t = 0f; t < SlideDur; t += Time.deltaTime)
            {
                float p = Mathf.SmoothStep(0f, 1f, t / SlideDur);
                rt.anchoredPosition = Vector2.Lerp(startPos, landPos, p);
                rt.localScale       = new Vector3(1f, p, 1f);
                yield return null;
            }
            rt.anchoredPosition = landPos;
            rt.localScale       = Vector3.one;

            // ── Phase 2: hold so the player can read it ───────────────────────
            yield return new WaitForSeconds(0.9f);

            // ── Phase 3: fade out in place ────────────────────────────────────
            const float FadeDur = 0.18f;
            for (float t = 0f; t < FadeDur; t += Time.deltaTime)
            {
                cg.alpha = 1f - t / FadeDur;
                yield return null;
            }

            Destroy(view.gameObject);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        void RepositionAll()
        {
            int   total      = cardContainer.childCount;
            float containerW = cardContainer.rect.width;
            if (containerW <= 1f) containerW = 500f;

            float spacing   = total <= 1 ? 0f
                : Mathf.Clamp((containerW - CardWidth) / (total - 1), MinSpacing, MaxSpacing);
            float totalSpan = CardWidth + (total - 1) * spacing;

            for (int i = 0; i < total; i++)
            {
                var childRt = (RectTransform)cardContainer.GetChild(i);
                float x = -totalSpan * 0.5f + CardWidth * 0.5f + i * spacing;
                childRt.anchoredPosition = new Vector2(x, 0f);
            }
        }
    }
}
