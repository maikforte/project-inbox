using System.Collections.Generic;
using InboxZero.Data;
using TMPro;
using UnityEngine;

namespace InboxZero.Core
{
    /// Tracks and processes player-side status effects (Guilt, AwaitingReply).
    /// Call ProcessTurnStart() from TurnManager at the top of each player turn.
    public class PlayerStatusManager : MonoBehaviour
    {
        public static PlayerStatusManager Instance { get; private set; }

        [Header("UI")]
        [Tooltip("Text label on the player panel that shows active statuses.")]
        public TextMeshProUGUI statusText;

        readonly Dictionary<StatusEffectType, int> _statuses = new Dictionary<StatusEffectType, int>();

        const int GuiltDamagePerStack = 2;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// Apply a status to the player. Guilt and AwaitingReply stack additively.
        /// Unread is enemy-only and is silently ignored.
        public void ApplyStatus(StatusEffectType type, int stacks)
        {
            if (type == StatusEffectType.None || type == StatusEffectType.Unread) return;
            if (stacks <= 0) return;

            if (_statuses.ContainsKey(type))
                _statuses[type] += stacks;
            else
                _statuses[type] = stacks;

            AudioManager.Instance?.PlayStatusApplied();
            UpdateUI();
        }

        /// Called by TurnManager.StartPlayerTurn after AP is set to max, before drawing.
        /// Applies status damage/penalties then ticks all durations down.
        public void ProcessTurnStart()
        {
            // Guilt: deal 2 damage per stack.
            if (_statuses.TryGetValue(StatusEffectType.Guilt, out int guilt) && guilt > 0)
                GameManager.Instance.TakeDamage(guilt * GuiltDamagePerStack);

            // AwaitingReply: lose 1 AP per stack (cannot go below 0).
            if (_statuses.TryGetValue(StatusEffectType.AwaitingReply, out int ar) && ar > 0)
            {
                var gm = GameManager.Instance;
                gm.CurrentAP = Mathf.Max(0, gm.CurrentAP - ar);
            }

            TickStatuses();
            UpdateUI();
        }

        /// Remove all statuses (call at the start of a new combat encounter).
        public void ClearAll()
        {
            _statuses.Clear();
            UpdateUI();
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
        }

        void UpdateUI()
        {
            if (statusText == null) return;
            if (_statuses.Count == 0) { statusText.text = ""; return; }

            var sb = new System.Text.StringBuilder();
            foreach (var kv in _statuses)
                if (kv.Value > 0)
                    sb.Append($"{kv.Key.ToString().ToUpper()} {kv.Value}  ");
            statusText.text = sb.ToString().TrimEnd();
        }
    }
}
