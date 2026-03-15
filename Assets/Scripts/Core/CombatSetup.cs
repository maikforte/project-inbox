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
    /// run state. The Inbox screen is the hub — all level encounters are generated
    /// upfront and the player picks which email to fight.
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

        // ── Inbox state ───────────────────────────────────────────────────────

        List<RoomOption> _inbox        = new List<RoomOption>();
        RoomOption       _activeOption;       // combat currently being fought
        RoomOption       _pendingRestOpt;     // rest stop row waiting to be removed
        System.Action    _afterDeckBuilder;   // what to do once deck builder closes

        // ── Lifecycle ─────────────────────────────────────────────────────────

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

            // Victory → Card Reward screen
            combatResultManager.OnVictoryContinued.AddListener(OnVictoryContinued);

            // Card reward screen is retired — drops are now automatic (see CardDropManager).

            // Inbox row clicked → combat or rest stop
            if (FloorMapScreen.Instance != null)
            {
                FloorMapScreen.Instance.OnCombatSelected.AddListener(BeginNextCombat);
                FloorMapScreen.Instance.OnRestStopSelected.AddListener(OnRestStopSelected);
            }

            // Rest Stop done → Inbox
            if (RestStopScreen.Instance != null)
                RestStopScreen.Instance.OnComplete.AddListener(OnRestStopDone);

            // Floor Transition → next level's Inbox or game victory
            if (FloorTransitionScreen.Instance != null)
            {
                FloorTransitionScreen.Instance.OnNextFloorReady.AddListener(OnNextFloorReady);
                FloorTransitionScreen.Instance.OnGameVictory.AddListener(ReturnToMenu);
            }

            // Deck Builder → continues flow after player is done editing
            if (InboxZero.UI.DeckBuilderScreen.Instance != null)
                InboxZero.UI.DeckBuilderScreen.Instance.OnComplete.AddListener(OnDeckBuilderDone);

            // Game Over / Victory → return to main menu
            combatResultManager.OnGameOverRestarted.AddListener(ReturnToMenu);
            combatResultManager.OnGameVictory.AddListener(ReturnToMenu);
        }

        // ── Run start ─────────────────────────────────────────────────────────

        public void StartRun()
        {
            GameManager.Instance.InitRun();
            DeckManager.Instance.InitDeck(new List<CardData>(starterCards));
            if (startingRelic != null)
            {
                GameManager.Instance.ActiveRelics.Add(startingRelic);
                RelicManager.Instance?.OnRelicAcquired(startingRelic);
            }
            BuildInboxForFloor(GameManager.Instance.CurrentFloor);
            ShowInbox();
        }

        // ── Inbox helpers ─────────────────────────────────────────────────────

        void BuildInboxForFloor(int floor)
        {
            var pool = GetPoolForFloor(floor);
            _inbox = FloorMapManager.Instance.GenerateAllLevelEncounters(pool);
        }

        void ShowInbox()
        {
            if (_inbox.Count == 0)
            {
                OnLevelCleared();
                return;
            }
            if (FloorMapScreen.Instance != null)
                FloorMapScreen.Instance.Show(_inbox);
        }

        List<EnemyData> GetPoolForFloor(int floor) => floor switch
        {
            1 => floor1Enemies,
            2 => floor2Enemies,
            3 => floor3Enemies,
            4 => floor4Enemies,
            _ => floor1Enemies,
        };

        // ── Combat lifecycle ──────────────────────────────────────────────────

        void BeginNextCombat(RoomOption opt)
        {
            _activeOption = opt;
            TurnManager.Instance.IsCombatEnded = false;

            if (opt.enemyData == null)
            {
                Debug.LogError("[CombatSetup] RoomOption has null enemyData.");
                return;
            }

            enemyController.Init(opt.enemyData);
            combatResultManager.RegisterEnemy(enemyController);
            TurnManager.Instance.BeginCombat();
        }

        // ── Flow callbacks ────────────────────────────────────────────────────

        void OnVictoryContinued()
        {
            _inbox.Remove(_activeOption);
            GameManager.Instance.CurrentRoom++;
            ShowInbox();
        }

        void OnRestStopSelected(RoomOption opt)
        {
            _pendingRestOpt = opt;
            if (RestStopScreen.Instance != null)
                RestStopScreen.Instance.Show();
        }

        void OnRestStopDone()
        {
            _inbox.Remove(_pendingRestOpt);
            GameManager.Instance.CurrentRoom++;
            ShowDeckBuilderOrContinue(ShowInbox);
        }

        void OnLevelCleared()
        {
            if (FloorTransitionScreen.Instance != null)
                FloorTransitionScreen.Instance.Show();
        }

        void OnNextFloorReady()
        {
            // CurrentFloor and CurrentRoom already advanced by FloorTransitionScreen.OnContinue
            BuildInboxForFloor(GameManager.Instance.CurrentFloor);
            ShowDeckBuilderOrContinue(ShowInbox);
        }

        void OnDeckBuilderDone()
        {
            _afterDeckBuilder?.Invoke();
            _afterDeckBuilder = null;
        }

        /// Shows the deck builder if there are cards in the collection, else calls <paramref name="onContinue"/> directly.
        void ShowDeckBuilderOrContinue(System.Action onContinue)
        {
            if (GameManager.Instance.CardCollection.Count > 0 &&
                InboxZero.UI.DeckBuilderScreen.Instance != null)
            {
                _afterDeckBuilder = onContinue;
                InboxZero.UI.DeckBuilderScreen.Instance.Show();
            }
            else
            {
                onContinue();
            }
        }

        static void ReturnToMenu()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
