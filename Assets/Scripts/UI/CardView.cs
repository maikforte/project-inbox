using System;
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
        [SerializeField] Transform _costBadgeRoot;
        [SerializeField] TextMeshProUGUI _effectText;

        [Header("Rarity Visuals")]
        [SerializeField] RarityVisuals _starter;
        [SerializeField] RarityVisuals _common;
        [SerializeField] RarityVisuals _uncommon;
        [SerializeField] RarityVisuals _rare;
        [SerializeField] RarityVisuals _legendary;

        [Header("Display Mode")]
        [SerializeField] GameObject _disabledOverlay;
        [SerializeField] Button _poolToggle;
        [SerializeField] TextMeshProUGUI _toggleLabel;

        bool _displayMode;
        Action _onHoverEnter;
        Action _onHoverExit;

        // Set by HandDisplay.RepositionAll — the card's intended slot position in cardContainer space.
        public Vector2 HandAnchoredPosition { get; set; }
        // Set true when the card is played so the draw animation coroutine yields control.
        public bool IsBeingPlayed { get; private set; }

        static readonly Color AmberOn = new Color(1.00f, 0.88f, 0.55f);
        static readonly Color TextDim = new Color(0.50f, 0.50f, 0.55f);

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
            if (_nameText   != null) _nameText.text = data.cardName.ToUpper();
            if (_costBadgeRoot != null)
            {
                string costStr = "AP " + data.apCost;
                foreach (var tmp in _costBadgeRoot.GetComponentsInChildren<TextMeshProUGUI>(true))
                    tmp.text = costStr;
            }
            if (_effectText != null) _effectText.text = data.effectDescription;
        }

        // ── Display mode API (used by AllMailScreen and CardRewardScreen) ──────

        public void SetDisplayMode(bool display)
        {
            _displayMode = display;
        }

        public void SetLocked(bool locked)
        {
            if (!locked) return;
            if (_icon != null) _icon.gameObject.SetActive(false);
            if (_nameText != null) _nameText.text = "???";
            if (_costBadgeRoot != null)
                foreach (var tmp in _costBadgeRoot.GetComponentsInChildren<TextMeshProUGUI>(true))
                    tmp.text = "?";
            if (_effectText != null) _effectText.text = "???";
        }

        public void SetDisabled(bool disabled)
        {
            if (_disabledOverlay != null) _disabledOverlay.SetActive(disabled);
        }

        public void ShowToggle(bool show, bool isEnabled, Action<bool> onToggle)
        {
            if (_poolToggle == null) return;
            _poolToggle.gameObject.SetActive(show);
            if (!show) return;

            SetToggleVisual(isEnabled);
            _poolToggle.onClick.RemoveAllListeners();
            bool state = isEnabled;
            _poolToggle.onClick.AddListener(() =>
            {
                state = !state;
                SetToggleVisual(state);
                SetDisabled(!state);
                onToggle?.Invoke(state);
            });
        }

        public void SetHoverCallbacks(Action onEnter, Action onExit)
        {
            _onHoverEnter = onEnter;
            _onHoverExit  = onExit;
        }

        void SetToggleVisual(bool on)
        {
            if (_toggleLabel != null)
            {
                _toggleLabel.text  = on ? "ON" : "OFF";
                _toggleLabel.color = on ? AmberOn : TextDim;
            }
        }

        // ── Input handlers ────────────────────────────────────────────────────

        public void OnPointerClick(PointerEventData _)
        {
            if (_displayMode) return;
            if (Data == null) return;

            if (TurnManager.Instance == null || !TurnManager.Instance.CanPlayCard(Data.apCost)) return;

            HandDisplay.Instance.HidePreview();
            HandDisplay.Instance.OnCardHoverExit(this);

            TurnManager.Instance.TrySpendAP(Data.apCost);

            // Attack cards delay effect resolution until the card reaches the enemy.
            // Defend cards resolve immediately (shield applies on click); animation is cosmetic.
            bool isAttack = Data.cardType == CardType.Attack;
            bool isDefend = Data.cardType == CardType.Defend;
            if (!isAttack)
                CardEffectResolver.Resolve(Data);

            DeckManager.Instance.PlayCard(Data);
            AudioManager.Instance?.PlayCardPlay();

            HandDisplay.Instance.DetachCard(this);

            // Snap to the final hand slot so the play animation always starts from the correct
            // position and scale, even if the draw animation hasn't finished yet.
            IsBeingPlayed = true;
            var snapRt = (RectTransform)transform;
            snapRt.anchoredPosition = HandAnchoredPosition;
            snapRt.localScale = Vector3.one;

            StartCoroutine(
                isAttack ? AnimateSlapAndDestroy() :
                isDefend ? AnimateShieldAndDestroy() :
                AnimatePlayAndDestroy());
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

        // Defend cards: anticipate (grow toward camera) → fly to player panel (shrink into screen) → vanish.
        IEnumerator AnimateShieldAndDestroy()
        {
            var playerPanel = CombatFX.Instance?.PlayerPanel;

            if (playerPanel == null)
            {
                yield return StartCoroutine(AnimatePlayAndDestroy());
                yield break;
            }

            var cf = GetComponent<CardFloat>();
            if (cf != null) cf.enabled = false;

            var cc = HandDisplay.Instance?.cardContainer;
            Vector3 startWorld = cc != null
                ? cc.TransformPoint(new Vector3(HandAnchoredPosition.x, HandAnchoredPosition.y, 0f))
                : transform.position;

            Vector3 targetWorld = playerPanel.transform.position;
            targetWorld.z = startWorld.z;
            transform.localScale = Vector3.one;

            ((RectTransform)transform).anchoredPosition = HandAnchoredPosition;

            var cg = gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            var overrideCanvas = gameObject.AddComponent<Canvas>();
            overrideCanvas.overrideSorting = true;
            overrideCanvas.sortingOrder = 100;

            Vector3 toTarget     = targetWorld - startWorld;
            Vector3 dir          = toTarget.normalized;
            float   pullDist     = Mathf.Max(toTarget.magnitude * 0.15f, 0.3f);
            Vector3 anticipateAt = startWorld - dir * pullDist;

            // === Phase 0 — Anticipation: pull back, grow toward the player (4th-wall) ===
            const float AntDuration = 0.13f;
            float t = 0f;
            while (t < AntDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / AntDuration);
                float c = p * (2f - p);
                transform.position   = Vector3.Lerp(startWorld, anticipateAt, c);
                transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.5f, c);
                yield return null;
            }
            transform.position   = anticipateAt;
            transform.localScale = Vector3.one * 1.5f;

            // === Phase 1 — Fly to player panel: build speed, shrink into screen (4th-wall) ===
            const float FlyDuration = 0.22f;
            t = 0f;
            while (t < FlyDuration)
            {
                t += Time.deltaTime;
                float p  = Mathf.Clamp01(t / FlyDuration);
                float ep = p * p * p;
                transform.position   = Vector3.Lerp(anticipateAt, targetWorld, ep);
                transform.localScale = Vector3.one * Mathf.Lerp(1.5f, 0.1f, p * p);
                yield return null;
            }
            transform.position   = targetWorld;
            transform.localScale = Vector3.one * 0.1f;

            yield return new WaitForSeconds(0.05f);

            // === Phase 2 — Vanish at player panel ===
            const float VanishDuration = 0.1f;
            t = 0f;
            while (t < VanishDuration)
            {
                t += Time.deltaTime;
                cg.alpha = 1f - Mathf.Clamp01(t / VanishDuration);
                yield return null;
            }

            Destroy(gameObject);
        }

        // Attack cards: anticipate (grow toward camera) → fly to enemy (shrink into screen) → vanish.
        IEnumerator AnimateSlapAndDestroy()
        {
            var enemy = CardEffectResolver.ActiveEnemy;

            if (enemy == null || enemy.portraitImage == null)
            {
                CardEffectResolver.Resolve(Data);
                yield return StartCoroutine(AnimatePlayAndDestroy());
                yield break;
            }

            // Stop CardFloat so it doesn't fight transform.position writes during the animation.
            var cf = GetComponent<CardFloat>();
            if (cf != null) cf.enabled = false;

            // Compute start position from the known hand slot in cardContainer space.
            // Reading transform.position AFTER AddComponent<Canvas> can return stale/wrong
            // values due to Unity's nested-canvas layout recalculation, so we derive the
            // world position from the authoritative HandAnchoredPosition instead.
            var cc = HandDisplay.Instance?.cardContainer;
            Vector3 startWorld = cc != null
                ? cc.TransformPoint(new Vector3(HandAnchoredPosition.x, HandAnchoredPosition.y, 0f))
                : transform.position;

            Vector3 targetWorld = enemy.portraitImage.transform.position;
            targetWorld.z = startWorld.z;
            transform.localScale = Vector3.one;

            // Teleport the card to its hand slot before adding overlay components.
            ((RectTransform)transform).anchoredPosition = HandAnchoredPosition;

            var cg = gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            // Bump sort order so this card renders on top of all panels.
            // We stay in the original hierarchy so coordinates never need conversion.
            var overrideCanvas = gameObject.AddComponent<Canvas>();
            overrideCanvas.overrideSorting = true;
            overrideCanvas.sortingOrder    = 100;

            // Direction from card to enemy; anticipation pulls the opposite way.
            // pullDist is in world units (scale = 0.03125 units/pixel), so 0.3f ≈ 10 px minimum.
            Vector3 toEnemy      = targetWorld - startWorld;
            Vector3 dir          = toEnemy.normalized;
            float   pullDist     = Mathf.Max(toEnemy.magnitude * 0.15f, 0.3f);
            Vector3 anticipateAt = startWorld - dir * pullDist;

            // === Phase 0 — Anticipation: pull back, grow toward the player (4th-wall) ===
            const float AntDuration = 0.13f;
            float t = 0f;
            while (t < AntDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / AntDuration);
                float c = p * (2f - p);                         // ease-out quad
                transform.position   = Vector3.Lerp(startWorld, anticipateAt, c);
                transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.5f, c);
                yield return null;
            }
            transform.position   = anticipateAt;
            transform.localScale = Vector3.one * 1.5f;

            // === Phase 1 — Fly to enemy: build speed, shrink away from player (4th-wall) ===
            const float FlyDuration = 0.22f;
            t = 0f;
            while (t < FlyDuration)
            {
                t += Time.deltaTime;
                float p  = Mathf.Clamp01(t / FlyDuration);
                float ep = p * p * p;                           // ease-in cubic — momentum builds
                transform.position   = Vector3.Lerp(anticipateAt, targetWorld, ep);
                transform.localScale = Vector3.one * Mathf.Lerp(1.5f, 0.1f, p * p);
                yield return null;
            }
            transform.position   = targetWorld;
            transform.localScale = Vector3.one * 0.1f;

            // Impact — resolve effects at the moment the card reaches the enemy.
            CardEffectResolver.Resolve(Data);
            yield return new WaitForSeconds(0.05f);

            // === Phase 2 — Vanish at the enemy center ===
            const float VanishDuration = 0.1f;
            t = 0f;
            while (t < VanishDuration)
            {
                t += Time.deltaTime;
                cg.alpha = 1f - Mathf.Clamp01(t / VanishDuration);
                yield return null;
            }

            Destroy(gameObject);
        }

        public void OnPointerEnter(PointerEventData _)
        {
            if (_displayMode) { _onHoverEnter?.Invoke(); return; }
            HandDisplay.Instance?.ShowPreview(Data);
            HandDisplay.Instance?.OnCardHoverEnter(this);
        }

        public void OnPointerExit(PointerEventData _)
        {
            if (_displayMode) { _onHoverExit?.Invoke(); return; }
            HandDisplay.Instance?.HidePreview();
            HandDisplay.Instance?.OnCardHoverExit(this);
        }
    }
}
