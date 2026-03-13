using System.Collections;
using System.Collections.Generic;
using InboxZero.Combat;
using InboxZero.Core;
using InboxZero.Data;
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
        public TextMeshProUGUI statusText;

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

        const int GuiltDamagePerTurn = 2;

        // ── Initialisation ────────────────────────────────────────────────────

        public void Init(EnemyData data)
        {
            Data      = data;
            CurrentHP = data.maxHP;
            IsDead    = false;
            _statuses.Clear();

            CardEffectResolver.ActiveTarget = this;

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
            CurrentHP -= amount;
            UpdateUI();
            CheckDeath();
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
            UpdateUI();
        }

        // ── Enemy turn logic ──────────────────────────────────────────────────

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
            if (!frozen)
            {
                // Attack player — GameManager.TakeDamage handles shield absorption.
                GameManager.Instance.TakeDamage(Data.damagePerTurn);

                // Apply status to player if this enemy has one.
                if (Data.statusAppliedOnAttack != StatusEffectType.None)
                    ApplyStatusToPlayer(Data.statusAppliedOnAttack, Data.statusDuration);
            }

            yield return new WaitForSeconds(actionDelay);

            // Regen.
            if (Data.regenPerTurn > 0)
            {
                CurrentHP = Mathf.Min(CurrentHP + Data.regenPerTurn, Data.maxHP);
                UpdateUI();
            }

            // Tick all status durations down by 1, remove expired.
            TickStatuses();

            TurnManager.Instance.EndEnemyTurn();
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
                hpText.text = $"{Mathf.Max(CurrentHP, 0)} / {(Data != null ? Data.maxHP : 0)}";

            if (hpBarFill != null && Data != null)
                hpBarFill.fillAmount = (float)Mathf.Max(CurrentHP, 0) / Data.maxHP;

            if (statusText != null)
            {
                if (_statuses.Count == 0)
                {
                    statusText.text = "";
                }
                else
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var kv in _statuses)
                        if (kv.Value > 0)
                            sb.Append($"{kv.Key.ToString().ToUpper()} {kv.Value}  ");
                    statusText.text = sb.ToString().TrimEnd();
                }
            }
        }
    }
}
