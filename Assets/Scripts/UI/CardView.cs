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
    [System.Serializable]
    public struct RarityVisuals
    {
        public Sprite bg;
    }

    public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public CardData Data { get; private set; }

        [SerializeField] Image _bg;
        [SerializeField] TextMeshProUGUI _nameText;
        [SerializeField] TextMeshProUGUI _costText;
        [SerializeField] TextMeshProUGUI _effectText;

        [Header("Rarity Visuals")]
        [SerializeField] RarityVisuals _starter;
        [SerializeField] RarityVisuals _common;
        [SerializeField] RarityVisuals _uncommon;
        [SerializeField] RarityVisuals _rare;
        [SerializeField] RarityVisuals _legendary;

        public void Populate(CardData data)
        {
            Data = data;

            RarityVisuals visuals = data.rarity switch
            {
                CardRarity.Common     => _common,
                CardRarity.Uncommon   => _uncommon,
                CardRarity.Rare       => _rare,
                CardRarity.Legendary  => _legendary,
                _                     => _starter,
            };

            if (_bg != null && visuals.bg != null) _bg.sprite = visuals.bg;

            if (_nameText   != null) _nameText.text   = data.cardName.ToUpper();
            if (_costText   != null) _costText.text   = data.apCost.ToString();
            if (_effectText != null) _effectText.text = data.effectDescription;
        }

        // ── Input handlers ────────────────────────────────────────────────────

        public void SetHighlight(bool on)
        {
            if (_bg != null) _bg.color = on ? new Color(1f, 0.55f, 0.55f) : Color.white;
        }

        public void OnPointerClick(PointerEventData _)
        {
            if (Data == null) return;

            // Overflow discard mode — clicking discards this card instead of playing it.
            if (HandDisplay.Instance != null && HandDisplay.Instance.IsDiscardMode)
            {
                HandDisplay.Instance.DiscardFromHand(this);
                return;
            }

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
