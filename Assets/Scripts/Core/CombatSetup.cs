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

        // ── Inbox / tab state ─────────────────────────────────────────────────

        // Per-tab encounter lists. _inbox is the primary path; others are optional.
        List<RoomOption> _inbox     = new List<RoomOption>();
        List<RoomOption> _spam      = new List<RoomOption>();
        List<RoomOption> _starred   = new List<RoomOption>();
        List<RoomOption> _snoozed   = new List<RoomOption>();
        List<RoomOption> _important = new List<RoomOption>();
        List<RoomOption> _sent      = new List<RoomOption>();
        List<RoomOption> _drafts    = new List<RoomOption>();

        RoomOption           _activeOption;
        // id → escalation level; rooms across all tabs share the same dict (ids are globally unique)
        Dictionary<int, int> _escalation = new Dictionary<int, int>();

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
                FloorMapScreen.Instance.OnTabSelected.AddListener(OnSideTabSelected);
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

        List<RoomOption> GetTabList(InboxTab tab) => tab switch
        {
            InboxTab.Spam      => _spam,
            InboxTab.Starred   => _starred,
            InboxTab.Snoozed   => _snoozed,
            InboxTab.Important => _important,
            InboxTab.Sent      => _sent,
            InboxTab.Drafts    => _drafts,
            _                  => _inbox,
        };

        void BuildInboxForFloor(int floor)
        {
            var fmm      = FloorMapManager.Instance;
            var pool     = GetPoolForFloor(floor);
            var nextPool = GetPoolForFloor(floor + 1);

            _inbox     = fmm.GenerateAllLevelEncounters(pool);
            _spam      = fmm.GenerateTabEncounters(pool, InboxTab.Spam);
            _starred   = fmm.GenerateTabEncounters(pool, InboxTab.Starred, nextPool);
            _snoozed   = fmm.GenerateTabEncounters(pool, InboxTab.Snoozed);
            _important = fmm.GenerateTabEncounters(pool, InboxTab.Important);
            _sent      = fmm.GenerateTabEncounters(pool, InboxTab.Sent);
            _drafts    = fmm.GenerateTabEncounters(pool, InboxTab.Drafts);

            _escalation.Clear();
            foreach (var tab in System.Enum.GetValues(typeof(InboxTab)))
                foreach (var opt in GetTabList((InboxTab)tab))
                    _escalation[opt.id] = 0;
        }

        // After an encounter, only rooms in the same tab escalate.
        void StepEscalation(InboxTab tab)
        {
            foreach (var opt in GetTabList(tab))
                _escalation[opt.id] = _escalation.TryGetValue(opt.id, out int cur) ? cur + 1 : 1;
        }

        int GetEscalation(RoomOption opt) =>
            opt.baseEscalation + (_escalation.TryGetValue(opt.id, out int level) ? level : 0);

        void ShowInbox()
        {
            if (_inbox.Count == 0)
            {
                OnLevelCleared();
                return;
            }
            if (FloorMapScreen.Instance != null)
                FloorMapScreen.Instance.Show(_inbox, _escalation);
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

            enemyController.Init(opt.enemyData, GetEscalation(opt));
            combatResultManager.RegisterEnemy(enemyController);
            TurnManager.Instance.BeginCombat();
        }

        // ── Flow callbacks ────────────────────────────────────────────────────

        void OnVictoryContinued()
        {
            // Tab-level rewardTierOverride takes priority over the enemy's own reward tier.
            var tier = _activeOption.rewardTierOverride
                ?? (_activeOption.enemyData != null ? _activeOption.enemyData.rewardTier : RewardTier.Common);
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
            _escalation.Remove(_activeOption.id);
            GetTabList(_activeOption.tab).Remove(_activeOption);
            StepEscalation(_activeOption.tab);
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
            _escalation.Remove(_pendingRestOpt.id);
            GetTabList(_pendingRestOpt.tab).Remove(_pendingRestOpt);
            StepEscalation(_pendingRestOpt.tab);
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

        void OnSideTabSelected(InboxTab tab)
        {
            var list = GetTabList(tab);
            FloorMapScreen.Instance?.ShowContent(list, _escalation);
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
