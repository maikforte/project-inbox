using System.Collections.Generic;
using InboxZero.Combat;
using InboxZero.Data;
using InboxZero.Enemies;
using InboxZero.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace InboxZero.Core
{
    /// Bootstraps the Combat scene: wires all UnityEvent chains and initialises
    /// run state. Enemies are drawn from per-floor pools; add EnemyData SOs to
    /// each list in the inspector as floors are implemented.
    public class CombatSetup : MonoBehaviour
    {
        [Header("Enemy Pools — one list per floor")]
        [SerializeField] List<EnemyData> floor1Enemies = new List<EnemyData>();
        [SerializeField] List<EnemyData> floor2Enemies = new List<EnemyData>();
        [SerializeField] List<EnemyData> floor3Enemies = new List<EnemyData>();
        [SerializeField] List<EnemyData> floor4Enemies = new List<EnemyData>();

        [Header("Starter Deck & Relic")]
        [Tooltip("Cards added to the player deck at run start.")]
        [SerializeField] List<CardData> starterCards = new List<CardData>();
        [Tooltip("Relic the player starts every run with (e.g. Paperclip).")]
        [SerializeField] RelicData startingRelic;

        [Header("Scene References")]
        [SerializeField] EnemyController enemyController;
        [SerializeField] CombatResultManager combatResultManager;
        [SerializeField] Button endTurnButton;

        void Start()
        {
            WireEvents();
            if (MainMenuScreen.Instance != null)
            {
                MainMenuScreen.Instance.Init(this);
                MainMenuScreen.Instance.Show();
            }
            else
            {
                StartRun();
            }
        }

        // ── Event wiring ──────────────────────────────────────────────────────

        void WireEvents()
        {
            // End Turn button → TurnManager
            if (endTurnButton != null)
                endTurnButton.onClick.AddListener(TurnManager.Instance.EndPlayerTurn);

            // Victory → Card Reward screen (or Floor Map if reward screen absent)
            combatResultManager.OnVictoryContinued.AddListener(OnVictoryContinued);

            // Card reward done → Floor Map
            if (CardRewardScreen.Instance != null)
                CardRewardScreen.Instance.OnComplete.AddListener(OnRewardDone);

            // Floor Map → room choices
            if (FloorMapScreen.Instance != null)
            {
                FloorMapScreen.Instance.OnCombatSelected.AddListener(BeginNextCombat);
                FloorMapScreen.Instance.OnRestStopSelected.AddListener(OnRestStopSelected);
                FloorMapScreen.Instance.OnFloorComplete.AddListener(OnFloorComplete);
            }

            // Rest Stop → Floor Map
            if (RestStopScreen.Instance != null)
                RestStopScreen.Instance.OnComplete.AddListener(OnRestStopDone);

            // Floor Transition → next floor or game victory
            if (FloorTransitionScreen.Instance != null)
            {
                FloorTransitionScreen.Instance.OnNextFloorReady.AddListener(BeginNextCombat);
                FloorTransitionScreen.Instance.OnGameVictory.AddListener(ReturnToMenu);
            }

            // Game Over / Victory → return to main menu
            combatResultManager.OnGameOverRestarted.AddListener(ReturnToMenu);
            combatResultManager.OnGameVictory.AddListener(ReturnToMenu);
        }

        // ── Combat lifecycle ──────────────────────────────────────────────────

        public void StartRun()
        {
            GameManager.Instance.InitRun();
            DeckManager.Instance.InitDeck(new List<CardData>(starterCards));
            if (startingRelic != null)
            {
                GameManager.Instance.ActiveRelics.Add(startingRelic);
                RelicManager.Instance?.OnRelicAcquired(startingRelic);
            }
            BeginNextCombat();
        }

        void BeginNextCombat()
        {
            TurnManager.Instance.IsCombatEnded = false;
            var enemy = PickEnemyForFloor(GameManager.Instance.CurrentFloor);
            if (enemy == null)
            {
                Debug.LogError($"[CombatSetup] No enemy data for floor {GameManager.Instance.CurrentFloor}. Add entries to the floor pool in the inspector.");
                return;
            }
            enemyController.Init(enemy);
            combatResultManager.RegisterEnemy(enemyController);
            TurnManager.Instance.BeginCombat();
        }

        EnemyData PickEnemyForFloor(int floor)
        {
            var pool = floor switch
            {
                1 => floor1Enemies,
                2 => floor2Enemies,
                3 => floor3Enemies,
                4 => floor4Enemies,
                _ => floor1Enemies,
            };
            if (pool == null || pool.Count == 0) return null;
            return pool[Random.Range(0, pool.Count)];
        }

        // ── Flow callbacks ────────────────────────────────────────────────────

        void OnVictoryContinued()
        {
            if (CardRewardScreen.Instance != null)
                CardRewardScreen.Instance.Show(GameManager.Instance.CurrentFloor);
            else if (FloorMapScreen.Instance != null)
                FloorMapScreen.Instance.Show();
        }

        void OnRewardDone()
        {
            if (FloorMapScreen.Instance != null)
                FloorMapScreen.Instance.Show();
        }

        void OnRestStopSelected()
        {
            if (RestStopScreen.Instance != null)
                RestStopScreen.Instance.Show();
        }

        void OnRestStopDone()
        {
            if (FloorMapScreen.Instance != null)
                FloorMapScreen.Instance.Show();
        }

        void OnFloorComplete()
        {
            if (FloorTransitionScreen.Instance != null)
                FloorTransitionScreen.Instance.Show();
        }

        static void ReturnToMenu()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
