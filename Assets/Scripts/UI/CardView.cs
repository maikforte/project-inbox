using System.Collections;
using InboxZero.Combat;
using InboxZero.Core;
using InboxZero.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InboxZero.UI
{
    public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public CardData Data { get; private set; }

        Image _bg;
        Image _typeBar;
        TextMeshProUGUI _nameText;
        TextMeshProUGUI _costText;
        TextMeshProUGUI _effectText;

        static readonly Color AttackColor  = new Color(0.85f, 0.18f, 0.18f);
        static readonly Color DefendColor  = new Color(0.18f, 0.75f, 0.25f);
        static readonly Color SpecialColor = new Color(0.55f, 0.18f, 0.80f);

        public void Populate(CardData data)
        {
            Data = data;

            _typeBar.color = data.cardType switch
            {
                CardType.Attack  => AttackColor,
                CardType.Defend  => DefendColor,
                _                => SpecialColor,
            };

            _nameText.text   = data.cardName.ToUpper();
            _costText.text   = data.apCost.ToString();
            _effectText.text = data.effectDescription;
        }

        public void SetReferences(Image bg, Image typeBar,
            TextMeshProUGUI nameText, TextMeshProUGUI costText, TextMeshProUGUI effectText)
        {
            _bg         = bg;
            _typeBar    = typeBar;
            _nameText   = nameText;
            _costText   = costText;
            _effectText = effectText;
        }

        // ── Input handlers ────────────────────────────────────────────────────

        public void OnPointerClick(PointerEventData _)
        {
            if (Data == null) return;
            if (!TurnManager.Instance.CanPlayCard(Data.apCost)) return;

            HandDisplay.Instance.HidePreview();
            transform.localScale = Vector3.one;

            TurnManager.Instance.TrySpendAP(Data.apCost);
            CardEffectResolver.Resolve(Data);
            DeckManager.Instance.PlayCard(Data);
            AudioManager.Instance?.PlayCardPlay();

            // Detach from hand list, animate the card flying away, then update hand display.
            HandDisplay.Instance.DetachCard(this);
            StartCoroutine(AnimatePlayAndDestroy());
            HandDisplay.Instance.RefreshHand();
        }

        IEnumerator AnimatePlayAndDestroy()
        {
            // Reparent to canvas root so the card can move freely above the hand container.
            var canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (canvas == null) { Destroy(gameObject); yield break; }

            transform.SetParent(canvas.transform, worldPositionStays: true);
            transform.SetAsLastSibling();

            var rt = (RectTransform)transform;
            var startPos = rt.anchoredPosition;

            var cg = gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            const float Duration = 0.18f;
            float t = 0f;
            while (t < Duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / Duration);
                rt.anchoredPosition = startPos + Vector2.up * (p * 50f);
                cg.alpha = 1f - p;
                yield return null;
            }

            Destroy(gameObject);
        }

        public void OnPointerEnter(PointerEventData _)
        {
            HandDisplay.Instance.ShowPreview(Data);
            transform.localScale = Vector3.one * 1.08f;
        }

        public void OnPointerExit(PointerEventData _)
        {
            HandDisplay.Instance.HidePreview();
            transform.localScale = Vector3.one;
        }
    }
}
