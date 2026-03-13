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
    /// <summary>
    /// Bootstraps the Combat scene: wires all UnityEvent chains and initialises
    /// run state. For TASK-13B this uses hard-wired test content; later tasks
    /// supply enemy-selection logic per floor.
    /// </summary>
    public class CombatSetup : MonoBehaviour
    {
        [Header("Test Content")]
        [Tooltip("Enemy used for this encounter.")]
        [SerializeField] EnemyData testEnemyData;

        [Tooltip("Cards added to the player deck at run start.")]
        [SerializeField] List<CardData> starterCards = new List<CardData>();

        [Header("Scene References")]
        [SerializeField] EnemyController enemyController;
        [SerializeField] CombatResultManager combatResultManager;
        [SerializeField] Button endTurnButton;

        void Start()
        {
            WireEvents();
            StartCombat(testEnemyData);
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
                FloorTransitionScreen.Instance.OnGameVictory.AddListener(OnGameVictory);
            }

            // Game Over restart
            combatResultManager.OnGameOverRestarted.AddListener(RestartRun);
        }

        // ── Combat lifecycle ──────────────────────────────────────────────────

        void StartCombat(EnemyData data)
        {
            GameManager.Instance.InitRun();
            DeckManager.Instance.InitDeck(new List<CardData>(starterCards));
            enemyController.Init(data);
            combatResultManager.RegisterEnemy(enemyController);
            TurnManager.Instance.BeginCombat();
        }

        // Begins a fresh combat encounter (reuses testEnemyData for TASK-13B).
        void BeginNextCombat()
        {
            TurnManager.Instance.IsCombatEnded = false;
            enemyController.Init(testEnemyData);
            combatResultManager.RegisterEnemy(enemyController);
            TurnManager.Instance.BeginCombat();
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

        static void OnGameVictory()
        {
            // Placeholder: reload the scene. A proper Victory screen replaces this in TASK-24.
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void RestartRun()
        {
            TurnManager.Instance.IsCombatEnded = false;
            StartCombat(testEnemyData);
        }
    }
}
