using InboxZero.Data;
using TMPro;
using UnityEngine;

namespace InboxZero.Core
{
    /// Applies passive relic effects at the correct game moments.
    /// Hook call sites:
    ///   TurnManager.BeginCombat      → OnCombatStart()
    ///   TurnManager.OnPlayerTurnStart → (subscribed internally)
    ///   GameManager.TakeDamage        → GetDamageReduction()
    ///   FloorTransitionScreen / CombatSetup → OnRelicAcquired()
    public class RelicManager : MonoBehaviour
    {
        public static RelicManager Instance { get; private set; }

        [Header("UI")]
        [Tooltip("Text label showing active relic names. Wire to a TMP object in the player panel.")]
        public TextMeshProUGUI relicDisplayText;

        bool _firstTurnOfCombat;
        bool _started;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            if (TurnManager.Instance != null)
                TurnManager.Instance.OnPlayerTurnStart.AddListener(OnPlayerTurnStart);
            _started = true;
        }

        void OnEnable()
        {
            if (_started && TurnManager.Instance != null)
                TurnManager.Instance.OnPlayerTurnStart.AddListener(OnPlayerTurnStart);
        }

        void OnDisable()
        {
            if (TurnManager.Instance != null)
                TurnManager.Instance.OnPlayerTurnStart.RemoveListener(OnPlayerTurnStart);
        }

        // ── Hook entry points ─────────────────────────────────────────────────

        /// Call at the start of each combat encounter (TurnManager.BeginCombat).
        public void OnCombatStart()
        {
            _firstTurnOfCombat = true;
            UpdateUI();
        }

        /// Called once when a relic is added to the player's collection.
        /// Applies immediate one-time effects (Work Phone Off: +15 max HP).
        public void OnRelicAcquired(RelicData relic)
        {
            if (relic == null) return;
            if (relic.effectType == RelicEffectType.BonusMaxHP)
            {
                var gm = GameManager.Instance;
                gm.MaxHP += relic.effectValue;
                gm.CurrentHP = Mathf.Min(gm.CurrentHP + relic.effectValue, gm.MaxHP);
            }
            UpdateUI();
        }

        /// Returns flat damage reduction from all active relics (Do Not Disturb).
        /// Called by GameManager.TakeDamage before shield absorption.
        public int GetDamageReduction()
        {
            int reduction = 0;
            foreach (var relic in GameManager.Instance.ActiveRelics)
                if (relic != null && relic.effectType == RelicEffectType.ReduceIncomingDamage)
                    reduction += relic.effectValue;
            return reduction;
        }

        // ── Internal turn-start handler ───────────────────────────────────────

        void OnPlayerTurnStart()
        {
            var gm = GameManager.Instance;

            // Paperclip: +1 AP on the first turn of each combat only.
            if (_firstTurnOfCombat)
            {
                foreach (var relic in gm.ActiveRelics)
                    if (relic != null && relic.effectType == RelicEffectType.BonusAPOnCombatStart)
                        gm.CurrentAP += relic.effectValue;
                _firstTurnOfCombat = false;
            }

            // Cold Coffee: restore HP each turn start.
            foreach (var relic in gm.ActiveRelics)
                if (relic != null && relic.effectType == RelicEffectType.HealOnTurnStart)
                    gm.CurrentHP = Mathf.Min(gm.CurrentHP + relic.effectValue, gm.MaxHP);

            // Mechanical Keyboard: draw 1 extra card per turn.
            // Must refresh HandDisplay after drawing since it already rendered before this listener ran.
            bool drewExtra = false;
            foreach (var relic in gm.ActiveRelics)
                if (relic != null && relic.effectType == RelicEffectType.BonusDrawPerTurn)
                {
                    DeckManager.Instance.DrawCards(relic.effectValue);
                    drewExtra = true;
                }
            if (drewExtra)
                InboxZero.UI.HandDisplay.Instance?.RefreshHand();
        }

        // ── UI ────────────────────────────────────────────────────────────────

        public void UpdateUI()
        {
            if (relicDisplayText == null) return;
            var relics = GameManager.Instance.ActiveRelics;
            if (relics.Count == 0) { relicDisplayText.text = ""; return; }

            var sb = new System.Text.StringBuilder();
            foreach (var relic in relics)
                if (relic != null)
                    sb.Append($"[{relic.relicName.ToUpper()}]  ");
            relicDisplayText.text = sb.ToString().TrimEnd();
        }
    }
}
