using System.Collections;
using System.Collections.Generic;
using InboxZero.Combat;
using InboxZero.Core;
using InboxZero.Data;
using InboxZero.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace InboxZero.Enemies
{
    public class EnemyController : MonoBehaviour, ICombatTarget
    {
        [Header("UI")]
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI hpText;
        public Image hpBarFill;
        public Image portraitImage;
        public TextMeshProUGUI statusText;
        [SerializeField] InboxZero.UI.StatusChipDisplay statusChipDisplay;

        [Header("Timing")]
        [Tooltip("Seconds between enemy turn actions (for readability).")]
        public float actionDelay = 0.6f;

        // Fired when this enemy dies — CombatManager / TASK-09 listens.
        public UnityEvent OnDeath = new UnityEvent();

        public EnemyData Data { get; private set; }
        public int CurrentHP { get; private set; }
        public bool IsDead { get; private set; }

        // StatusEffectType → remaining turns
        readonly Dictionary<StatusEffectType, int> _statuses = new Dictionary<StatusEffectType, int>();

        // HP bar smooth lerp
        float _fillTarget;
        float _fillDisplay;

        int _currentShield;

        // Intent deck state
        readonly List<CardData> _intentDeck    = new List<CardData>();
        readonly List<CardData> _intentDiscard = new List<CardData>();

        /// The card telegraphed to the player — will be played on the enemy's next turn.
        public CardData CurrentIntentCard { get; private set; }

        const int GuiltDamagePerTurn = 2;

        // Flat bonuses applied per escalation level.
        public const int EscalationHPBonus  = 5;
        public const int EscalationDmgBonus = 2;

        // Effective max HP and damage bonus after escalation scaling.
        int _scaledMaxHP;
        int _damageBonus;
        public int DamageBonus   => _damageBonus;
        public int ScaledMaxHP   => _scaledMaxHP;

        // ── Initialisation ────────────────────────────────────────────────────

        public void Init(EnemyData data, int escalation = 0)
        {
            Data           = data;
            _scaledMaxHP   = data.maxHP + escalation * EscalationHPBonus;
            _damageBonus   = escalation * EscalationDmgBonus;
            CurrentHP      = _scaledMaxHP;
            IsDead         = false;
            _statuses.Clear();
            _fillTarget = _fillDisplay = 1f;

            _currentShield = 0;

            if (portraitImage != null)
            {
                portraitImage.sprite  = data.portrait;
                portraitImage.enabled = data.portrait != null;
            }

            CardEffectResolver.ActiveTarget = this;
            CardEffectResolver.ActiveEnemy  = this;

            // Set up intent deck — shuffle a copy so the source asset is never mutated.
            _intentDeck.Clear();
            _intentDiscard.Clear();
            CurrentIntentCard = null;
            if (data.intentDeck != null && data.intentDeck.Count > 0)
            {
                _intentDeck.AddRange(data.intentDeck);
                Shuffle(_intentDeck);
                DrawNextIntent();
            }
            UpdateUI();

            TurnManager.Instance.OnEnemyTurnStart.AddListener(OnEnemyTurnStart);
        }

        void OnDestroy()
        {
            if (TurnManager.Instance != null)
                TurnManager.Instance.OnEnemyTurnStart.RemoveListener(OnEnemyTurnStart);
        }

        // ── ICombatTarget ─────────────────────────────────────────────────────

        public void TakeDamage(int amount)
        {
            if (IsDead) return;
            int absorbed = Mathf.Min(_currentShield, amount);
            _currentShield -= absorbed;
            int net = amount - absorbed;
            CurrentHP -= net;
            UpdateUI();
            if (net > 0)
            {
                FloatingText.Spawn($"-{net}", portraitImage?.rectTransform, new Color(1f, 0.35f, 0.35f));
                CombatFX.Instance?.EnemyHit(portraitImage);
            }
            AudioManager.Instance?.PlayDamageHit();
            CheckDeath();
        }

        public void GainShield(int amount)
        {
            _currentShield += amount;
            UpdateUI();
        }

        public void Heal(int amount)
        {
            if (IsDead) return;
            CurrentHP = Mathf.Min(CurrentHP + amount, _scaledMaxHP);
            UpdateUI();
        }

        public void ApplyStatus(StatusEffectType type, int duration)
        {
            if (type == StatusEffectType.None) return;
            if (_statuses.ContainsKey(type))
            {
                // Guilt stacks additively; other statuses (Unread) refresh to the higher value.
                if (type == StatusEffectType.Guilt)
                    _statuses[type] += duration;
                else
                    _statuses[type] = Mathf.Max(_statuses[type], duration);
            }
            else
            {
                _statuses[type] = duration;
            }
            AudioManager.Instance?.PlayStatusApplied();
            UpdateUI();
        }

        // ── Enemy turn logic ──────────────────────────────────────────────────

        void Update()
        {
            if (hpBarFill == null || _fillDisplay == _fillTarget) return;
            _fillDisplay = Mathf.MoveTowards(_fillDisplay, _fillTarget, Time.deltaTime * 3f);
            hpBarFill.fillAmount = _fillDisplay;
        }

        void OnEnemyTurnStart()
        {
            if (IsDead) return;
            StartCoroutine(EnemyTurnRoutine());
        }

        IEnumerator EnemyTurnRoutine()
        {
            yield return new WaitForSeconds(actionDelay);

            // Guilt: enemy takes 2 damage per stack at start of its turn.
            if (_statuses.TryGetValue(StatusEffectType.Guilt, out int guiltStacks) && guiltStacks > 0)
            {
                TakeDamage(guiltStacks * GuiltDamagePerTurn);
                if (IsDead) yield break;
            }

            yield return new WaitForSeconds(actionDelay);

            // Unread: enemy skips attack this turn.
            bool frozen = _statuses.TryGetValue(StatusEffectType.Unread, out int unread) && unread > 0;
            if (frozen) EnemyIntentWidget.Instance?.ShowFrozen();
            if (!frozen)
            {
                if (CurrentIntentCard != null)
                {
                    // Animate the card sliding down and flipping to reveal itself.
                    Debug.Log($"[EnemyController] About to animate card: {CurrentIntentCard.cardName} | EnemyHandDisplay.Instance={EnemyHandDisplay.Instance != null}");
                    if (EnemyHandDisplay.Instance != null)
                        yield return EnemyHandDisplay.Instance.PlayCardAnimation(CurrentIntentCard);

                    // Card-driven attack.
                    CardEffectResolver.Resolve(CurrentIntentCard, EffectExecutor.Enemy);
                }
                else
                {
                    // Legacy flat-damage fallback (no intent deck assigned).
                    GameManager.Instance.TakeDamage(Data.damagePerTurn + _damageBonus);
                    if (Data.statusAppliedOnAttack != StatusEffectType.None)
                        ApplyStatusToPlayer(Data.statusAppliedOnAttack, Data.statusDuration);
                }
            }

            // Discard the played card and draw next intent for the player to see.
            if (CurrentIntentCard != null)
            {
                _intentDiscard.Add(CurrentIntentCard);
                CurrentIntentCard = null;
            }
            DrawNextIntent();

            yield return new WaitForSeconds(actionDelay);

            // Regen.
            if (Data.regenPerTurn > 0)
            {
                CurrentHP = Mathf.Min(CurrentHP + Data.regenPerTurn, _scaledMaxHP);
                UpdateUI();
            }

            // Shield resets at end of enemy turn (mirrors player shield reset).
            _currentShield = 0;

            // Tick all status durations down by 1, remove expired.
            TickStatuses();

            TurnManager.Instance.EndEnemyTurn();
        }

        // ── Intent deck ───────────────────────────────────────────────────────

        void DrawNextIntent()
        {
            if (_intentDeck.Count == 0 && _intentDiscard.Count == 0) return;

            if (_intentDeck.Count == 0)
            {
                _intentDeck.AddRange(_intentDiscard);
                _intentDiscard.Clear();
                Shuffle(_intentDeck);
            }

            CurrentIntentCard = _intentDeck[0];
            _intentDeck.RemoveAt(0);
            Debug.Log($"[EnemyController] Intent: {CurrentIntentCard.cardName}");
            EnemyIntentWidget.Instance?.ShowIntent(CurrentIntentCard);
        }

        static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        void TickStatuses()
        {
            var expired = new List<StatusEffectType>();
            foreach (var key in new List<StatusEffectType>(_statuses.Keys))
            {
                _statuses[key]--;
                if (_statuses[key] <= 0) expired.Add(key);
            }
            foreach (var key in expired) _statuses.Remove(key);
            UpdateUI();
        }

        void CheckDeath()
        {
            if (CurrentHP > 0) return;
            CurrentHP = 0;
            IsDead    = true;
            UpdateUI();
            TurnManager.Instance.OnEnemyTurnStart.RemoveListener(OnEnemyTurnStart);
            EnemyHandDisplay.Instance?.Hide();
            EnemyIntentWidget.Instance?.Hide();
            AudioManager.Instance?.PlayEnemyDeath();
            OnDeath.Invoke();
        }

        void ApplyStatusToPlayer(StatusEffectType type, int duration)
        {
            if (PlayerStatusManager.Instance != null)
                PlayerStatusManager.Instance.ApplyStatus(type, duration);
        }

        void UpdateUI()
        {
            if (nameText != null)
                nameText.text = Data != null ? Data.enemyName.ToUpper() : "";

            if (hpText != null)
                hpText.text = $"{Mathf.Max(CurrentHP, 0)} / {_scaledMaxHP}";

            if (Data != null && _scaledMaxHP > 0)
                _fillTarget = (float)Mathf.Max(CurrentHP, 0) / _scaledMaxHP;

            statusChipDisplay?.Refresh(_statuses);
        }
    }
}
