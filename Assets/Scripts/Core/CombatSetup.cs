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

        [Header("Card Registry")]
        [Tooltip("All cards in the game — used for rarity unlock triggers.")]
        [SerializeField] AllCardsRegistry allCardsRegistry;

        [Header("Scene References")]
        [SerializeField] EnemyController enemyController;
        [SerializeField] CombatResultManager combatResultManager;
        [SerializeField] Button endTurnButton;

        // ── Inbox state ───────────────────────────────────────────────────────

        List<RoomOption> _inbox       = new List<RoomOption>();
        RoomOption       _activeOption;  // combat currently being fought

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

            // Victory panel Continue → card reward screen
            combatResultManager.OnVictoryContinued.AddListener(OnVictoryContinued);

            // Card reward screen done → return to inbox
            if (CardRewardScreen.Instance != null)
                CardRewardScreen.Instance.OnComplete.AddListener(OnCardRewardDone);

            // Inbox row clicked → combat or rest stop
            if (FloorMapScreen.Instance != null)
            {
                FloorMapScreen.Instance.OnCombatSelected.AddListener(BeginNextCombat);
                FloorMapScreen.Instance.OnRestStopSelected.AddListener(OnRestStopSelected);
                FloorMapScreen.Instance.OnAllMailSelected.AddListener(OnAllMailSelected);
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

            // Game Over → return to main menu
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
            // Show guaranteed 3-card reward based on the defeated enemy's reward tier.
            var tier = _activeOption.enemyData != null ? _activeOption.enemyData.rewardTier : RewardTier.Common;
            if (CardRewardScreen.Instance != null)
                CardRewardScreen.Instance.Show(tier);
            else
                FinishCombat();  // fallback if reward screen not in scene
        }

        void OnCardRewardDone()
        {
            FinishCombat();
        }

        void FinishCombat()
        {
            TriggerUnlocks(_activeOption.enemyData);
            _inbox.Remove(_activeOption);
            GameManager.Instance.CurrentRoom++;
            ShowInbox();
        }

        void TriggerUnlocks(EnemyData enemy)
        {
            if (enemy == null || UnlockManager.Instance == null) return;
            var um = UnlockManager.Instance;

            // Rarity-tier unlocks — every enemy of that tier unlocks the whole rarity.
            switch (enemy.rewardTier)
            {
                case RewardTier.Uncommon:
                    um.UnlockRarity(CardRarity.Uncommon, allCardsRegistry);
                    break;
                case RewardTier.Rare:
                    um.UnlockRarity(CardRarity.Uncommon, allCardsRegistry);
                    um.UnlockRarity(CardRarity.Rare, allCardsRegistry);
                    break;
                case RewardTier.Boss:
                    um.UnlockRarity(CardRarity.Uncommon, allCardsRegistry);
                    um.UnlockRarity(CardRarity.Rare, allCardsRegistry);
                    um.UnlockRarity(CardRarity.Legendary, allCardsRegistry);
                    break;
            }

            // Signature unlock — specific card tied to this enemy's first kill.
            if (enemy.signatureUnlockCard != null)
                um.Unlock(enemy.signatureUnlockCard);
        }

        void OnRestStopSelected(RoomOption opt)
        {
            _pendingRestOpt = opt;
            if (RestStopScreen.Instance != null)
                RestStopScreen.Instance.Show();
        }

        RoomOption _pendingRestOpt;

        void OnRestStopDone()
        {
            _inbox.Remove(_pendingRestOpt);
            GameManager.Instance.CurrentRoom++;
            ShowInbox();
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
            ShowInbox();
        }

        void OnAllMailSelected()
        {
            var am = InboxZero.UI.AllMailScreen.Instance;
            if (am == null)
            {
                Debug.LogWarning("[CombatSetup] AllMailScreen not found. Add it to the scene.");
                ShowInbox();
                return;
            }
            am.OnClose.AddListener(OnAllMailClosedDuringRun);
            am.Show();
        }

        void OnAllMailClosedDuringRun()
        {
            var am = InboxZero.UI.AllMailScreen.Instance;
            if (am != null) am.OnClose.RemoveListener(OnAllMailClosedDuringRun);
            ShowInbox();
        }

        static void ReturnToMenu()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
