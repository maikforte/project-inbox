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
        [SerializeField] Image _icon;
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

            if (_icon       != null) { _icon.sprite = data.icon; _icon.enabled = data.icon != null; }
            if (_nameText   != null) _nameText.text   = data.cardName.ToUpper();
            if (_costText   != null) _costText.text   = data.apCost.ToString();
            if (_effectText != null) _effectText.text = data.effectDescription;
        }

        // ── Input handlers ────────────────────────────────────────────────────

        public void OnPointerClick(PointerEventData _)
        {
            if (Data == null) return;

            if (TurnManager.Instance == null || !TurnManager.Instance.CanPlayCard(Data.apCost)) return;

            HandDisplay.Instance.HidePreview();
            transform.localScale = Vector3.one;

            TurnManager.Instance.TrySpendAP(Data.apCost);

            // Attack cards delay effect resolution until the card reaches the enemy.
            bool isAttack = Data.cardType == CardType.Attack;
            if (!isAttack)
                CardEffectResolver.Resolve(Data);

            DeckManager.Instance.PlayCard(Data);
            AudioManager.Instance?.PlayCardPlay();

            HandDisplay.Instance.DetachCard(this);
            StartCoroutine(isAttack ? AnimateSlapAndDestroy() : AnimatePlayAndDestroy());
            HandDisplay.Instance.RefreshHand();
        }

        // Non-attack cards: slide up and fade.
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

        // Attack cards: fly to enemy, fire effects on impact, then shrink-fade.
        IEnumerator AnimateSlapAndDestroy()
        {
            var enemy = CardEffectResolver.ActiveEnemy;

            // Fallback if no active enemy (e.g. enemy just died on a multi-effect card).
            if (enemy == null || enemy.portraitImage == null)
            {
                CardEffectResolver.Resolve(Data);
                yield return StartCoroutine(AnimatePlayAndDestroy());
                yield break;
            }

            var cg = gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            // Re-parent to canvas root so the card can travel anywhere on screen.
            var canvas = GetComponentInParent<Canvas>();
            Vector3 worldStart  = transform.position;
            Vector3 worldTarget = enemy.portraitImage.transform.position;
            if (canvas != null) transform.SetParent(canvas.transform, worldPositionStays: true);

            // Phase 1 — fly to enemy (ease-in acceleration).
            const float FlyDuration = 0.14f;
            float t = 0f;
            while (t < FlyDuration)
            {
                t += Time.deltaTime;
                float p  = Mathf.Clamp01(t / FlyDuration);
                float ep = p * p;   // ease-in
                transform.position = Vector3.Lerp(worldStart, worldTarget, ep);
                transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.15f, ep);
                yield return null;
            }

            // Impact — resolve effects here: triggers TakeDamage → panel shake + floating text.
            CardEffectResolver.Resolve(Data);
            transform.localScale = Vector3.one * 1.35f;
            yield return new WaitForSeconds(0.04f);

            // Phase 2 — shrink and fade at impact point.
            const float FadeDuration = 0.14f;
            t = 0f;
            while (t < FadeDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / FadeDuration);
                cg.alpha             = 1f - p;
                transform.localScale = Vector3.one * Mathf.Lerp(1.35f, 0.5f, p);
                yield return null;
            }

            Destroy(gameObject);
        }

        public void OnPointerEnter(PointerEventData _)
        {
            HandDisplay.Instance?.ShowPreview(Data);
            transform.localScale = Vector3.one * 1.08f;
        }

        public void OnPointerExit(PointerEventData _)
        {
            HandDisplay.Instance?.HidePreview();
            transform.localScale = Vector3.one;
        }
    }
}
