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

        // Fired once at the start of each combat (before the first player turn).
        public UnityEvent OnCombatStart = new UnityEvent();
        // Fired at the start of each player turn (after draw + AP reset).
        public UnityEvent OnPlayerTurnStart = new UnityEvent();
        // Fired when the player ends their turn.
        public UnityEvent OnPlayerTurnEnd = new UnityEvent();
        // Fired at the start of the enemy turn.
        public UnityEvent OnEnemyTurnStart = new UnityEvent();
        // Fired at the end of the enemy turn (returns control to player).
        public UnityEvent OnEnemyTurnEnd = new UnityEvent();

        const int DrawPerTurn    = 3;
        const int ReshuffleBelow = 2;  // reshuffle discard into draw when draw pile <= this

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // Call at the start of each combat encounter.
        public void BeginCombat()
        {
            if (PlayerStatusManager.Instance != null)
                PlayerStatusManager.Instance.ClearAll();
            if (RelicManager.Instance != null)
                RelicManager.Instance.OnCombatStart();
            AudioManager.Instance?.PlayBattleMusic();

            // Discard any cards left in hand from the previous fight,
            // then signal listeners to clear visuals before drawing.
            DeckManager.Instance.DiscardHand();
            OnCombatStart.Invoke();

            StartPlayerTurn();
        }

        // ── Player turn ───────────────────────────────────────────────────────

        void StartPlayerTurn()
        {
            CurrentPhase = TurnPhase.PlayerTurn;

            var gm = GameManager.Instance;
            gm.CurrentShield = 0;
            gm.CurrentAP = gm.MaxAP;

            // Apply player status effects (Guilt damage, AwaitingReply AP reduction).
            if (PlayerStatusManager.Instance != null)
                PlayerStatusManager.Instance.ProcessTurnStart();

            // Reshuffle discard into draw pile if draw pile is running low.
            var dm = DeckManager.Instance;
            if (gm.DrawPile.Count <= ReshuffleBelow && gm.DiscardPile.Count > 0)
                dm.ReshuffleDiscard();

            dm.DrawCards(DrawPerTurn);
            OnPlayerTurnStart.Invoke();
        }

        // Called by the End Turn button.
        public void EndPlayerTurn()
        {
            if (IsCombatEnded) return;
            if (CurrentPhase != TurnPhase.PlayerTurn) return;

            AudioManager.Instance?.PlayTurnEnd();
            // Discard unplayed cards — hand refreshes each turn.
            DeckManager.Instance.DiscardHand();
            OnPlayerTurnEnd.Invoke();

            StartEnemyTurn();
        }

        // ── Enemy turn ────────────────────────────────────────────────────────

        void StartEnemyTurn()
        {
            CurrentPhase = TurnPhase.EnemyTurn;
            OnEnemyTurnStart.Invoke();
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
