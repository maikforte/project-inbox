using UnityEngine;
using UnityEngine.Events;

namespace InboxZero.Core
{
    public enum TurnPhase { PlayerTurn, EnemyTurn }

    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }

        public TurnPhase CurrentPhase { get; private set; }

        // Set by CombatResultManager on victory or game over. Blocks all further input.
        public bool IsCombatEnded { get; set; }

        // Fired at the start of each player turn (after draw + AP reset).
        public UnityEvent OnPlayerTurnStart = new UnityEvent();
        // Fired when the player ends their turn.
        public UnityEvent OnPlayerTurnEnd = new UnityEvent();
        // Fired at the start of the enemy turn.
        public UnityEvent OnEnemyTurnStart = new UnityEvent();
        // Fired at the end of the enemy turn (returns control to player).
        public UnityEvent OnEnemyTurnEnd = new UnityEvent();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // Call at the start of combat to kick off the first player turn.
        public void BeginCombat()
        {
            if (PlayerStatusManager.Instance != null)
                PlayerStatusManager.Instance.ClearAll();
            if (RelicManager.Instance != null)
                RelicManager.Instance.OnCombatStart();
            StartPlayerTurn();
        }

        // ── Player turn ───────────────────────────────────────────────────────

        void StartPlayerTurn()
        {
            CurrentPhase = TurnPhase.PlayerTurn;

            var gm = GameManager.Instance;
            gm.CurrentShield = 0;   // shield resets at the start of each new turn
            gm.CurrentAP = gm.MaxAP;

            // Apply player status effects (Guilt damage, AwaitingReply AP reduction).
            if (PlayerStatusManager.Instance != null)
                PlayerStatusManager.Instance.ProcessTurnStart();

            DeckManager.Instance.DrawCards(gm.HandSize);

            OnPlayerTurnStart.Invoke();
        }

        // Called by the End Turn button.
        public void EndPlayerTurn()
        {
            if (IsCombatEnded) return;
            if (CurrentPhase != TurnPhase.PlayerTurn) return;

            DeckManager.Instance.DiscardHand();
            OnPlayerTurnEnd.Invoke();

            StartEnemyTurn();
        }

        // ── Enemy turn ────────────────────────────────────────────────────────

        void StartEnemyTurn()
        {
            CurrentPhase = TurnPhase.EnemyTurn;
            OnEnemyTurnStart.Invoke();
            // EnemyManager (future task) will call EndEnemyTurn() when done.
        }

        // Called by EnemyManager after the enemy has acted.
        public void EndEnemyTurn()
        {
            if (CurrentPhase != TurnPhase.EnemyTurn) return;

            OnEnemyTurnEnd.Invoke();
            StartPlayerTurn();
        }

        // ── AP helpers ────────────────────────────────────────────────────────

        // Returns true and deducts AP if affordable; false if not enough AP.
        public bool TrySpendAP(int amount)
        {
            if (GameManager.Instance.CurrentAP < amount) return false;
            GameManager.Instance.CurrentAP -= amount;
            return true;
        }

        public void RefundAP(int amount)
        {
            var gm = GameManager.Instance;
            gm.CurrentAP += amount;
        }

        // Returns true if a card costing `apCost` can be played right now.
        public bool CanPlayCard(int apCost)
        {
            return !IsCombatEnded
                && CurrentPhase == TurnPhase.PlayerTurn
                && GameManager.Instance.CurrentAP >= apCost;
        }
    }
}
