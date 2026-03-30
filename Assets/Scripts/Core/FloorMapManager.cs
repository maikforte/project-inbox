using System.Collections.Generic;
using InboxZero.Data;
using UnityEngine;

namespace InboxZero.Core
{
    public enum RoomType  { Combat, RestStop }
    public enum InboxTab  { Inbox, Spam, Starred, Snoozed, Important, Sent, Drafts }

    [System.Serializable]
    public struct RoomOption
    {
        public int      id;          // unique per run — used as escalation dict key
        public InboxTab tab;         // which sidebar tab this room belongs to
        public RoomType type;
        public string   label;
        // Inbox display fields
        public string senderName;
        public string subjectLine;
        public string previewText;
        // Enemy reference — null for rest stops
        public EnemyData enemyData;
        // Tab-level escalation bonus (baked in at generation time)
        public int baseEscalation;
        // Override the reward tier offered after this encounter (null = use enemy's own tier)
        public RewardTier? rewardTierOverride;
    }

    /// Generates all encounters for a level upfront (inbox-as-hub model).
    public class FloorMapManager : MonoBehaviour
    {
        public static FloorMapManager Instance { get; private set; }

        [Tooltip("Probability (0–1) that a rest stop row is added per enemy in the pool.")]
        [Range(0f, 1f)] public float restStopChance = 0.30f;

        // ── Flavor text tables ────────────────────────────────────────────────

        static readonly string[][] SubjectsByFloor =
        {
            new[] { "Fw: Important Update", "Re: Meeting Tomorrow", "Unsubscribe Confirmation" },
            new[] { "Re: Re: Re: That Thing", "FYI (no action needed)", "Following Up..." },
            new[] { "Per My Last Email", "As Previously Stated", "URGENT: Please Advise" },
            new[] { "Re: Re: Fw: Re: Fw: Re: Friday Lunch?" },
        };

        static readonly string[] RestSubjects =
        {
            "You have a moment. Take it.",
            "Nothing in your calendar right now.",
            "Out of meetings until 3pm.",
            "Reminder: take a break.",
        };

        int _nextId;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Level encounter generation ────────────────────────────────────────

        /// Returns all encounters for the current level — one row per enemy in
        /// the pool, plus rest stops sprinkled in based on restStopChance.
        public List<RoomOption> GenerateAllLevelEncounters(List<EnemyData> enemyPool)
        {
            int floorIdx = Mathf.Clamp(GameManager.Instance.CurrentFloor - 1, 0, 3);
            var options  = new List<RoomOption>();

            foreach (var enemy in enemyPool)
                options.Add(MakeCombatOption(floorIdx, enemy));

            // Sprinkle rest stops proportionally to pool size
            int restCount = Mathf.RoundToInt(enemyPool.Count * restStopChance);
            for (int i = 0; i < restCount; i++)
                options.Add(MakeRestOption());

            Shuffle(options);
            return options;
        }

        // ── Option factories ─────────────────────────────────────────────────

        RoomOption MakeCombatOption(int floorIdx, EnemyData enemy, InboxTab tab = InboxTab.Inbox,
                                    int baseEscalation = 0, RewardTier? rewardTierOverride = null)
        {
            var subjects = SubjectsByFloor[floorIdx];

            string preview = $"{enemy.maxHP} HP  *  {enemy.damagePerTurn} DMG/TURN";
            if (enemy.regenPerTurn > 0) preview += "  *  REGEN";

            return new RoomOption
            {
                id                 = _nextId++,
                tab                = tab,
                type               = RoomType.Combat,
                label              = "COMBAT",
                senderName         = enemy.enemyName,
                subjectLine        = subjects[Random.Range(0, subjects.Length)],
                previewText        = preview,
                enemyData          = enemy,
                baseEscalation     = baseEscalation,
                rewardTierOverride = rewardTierOverride,
            };
        }

        RoomOption MakeRestOption(InboxTab tab = InboxTab.Inbox) => new RoomOption
        {
            id          = _nextId++,
            tab         = tab,
            type        = RoomType.RestStop,
            label       = "REST STOP",
            senderName  = "Rest Stop",
            subjectLine = RestSubjects[Random.Range(0, RestSubjects.Length)],
            previewText = "Restore 15 HP.",
        };

        // ── Tab encounter generation ──────────────────────────────────────────

        /// Generates encounters for a sidebar tab.
        /// <paramref name="nextPool"/> is used by the Starred tab to source elite encounters
        /// from the next floor's enemy pool.
        public List<RoomOption> GenerateTabEncounters(List<EnemyData> pool, InboxTab tab,
                                                      List<EnemyData> nextPool = null)
        {
            int floorIdx = Mathf.Clamp(GameManager.Instance.CurrentFloor - 1, 0, 3);

            switch (tab)
            {
                case InboxTab.Spam:
                    return GenerateSpamEncounters(pool, floorIdx);
                case InboxTab.Starred:
                    return GenerateStarredEncounters(nextPool ?? pool, floorIdx);
                case InboxTab.Important:
                    return GenerateImportantEncounters(pool, floorIdx);
                default:
                    return new List<RoomOption>();  // other tabs stubbed
            }
        }

        // Spam — ambush fights: full pool, pre-escalated +1, reward tier bumped up one.
        List<RoomOption> GenerateSpamEncounters(List<EnemyData> pool, int floorIdx)
        {
            var options = new List<RoomOption>();
            foreach (var enemy in pool)
                options.Add(MakeCombatOption(floorIdx, enemy, InboxTab.Spam,
                    baseEscalation: 1, rewardTierOverride: BumpTier(enemy.rewardTier)));

            Shuffle(options);
            return options;
        }

        // Starred — elite fights: 2 encounters from the next floor's pool, Rare reward.
        List<RoomOption> GenerateStarredEncounters(List<EnemyData> pool, int floorIdx)
        {
            var shuffled = new List<EnemyData>(pool);
            Shuffle(shuffled);

            var options = new List<RoomOption>();
            int count   = Mathf.Min(2, shuffled.Count);
            for (int i = 0; i < count; i++)
                options.Add(MakeCombatOption(floorIdx, shuffled[i], InboxTab.Starred,
                    baseEscalation: 0, rewardTierOverride: RewardTier.Rare));

            return options;
        }

        // Important — urgent fights: 2 encounters from current pool, pre-escalated +2, reward bumped.
        List<RoomOption> GenerateImportantEncounters(List<EnemyData> pool, int floorIdx)
        {
            var shuffled = new List<EnemyData>(pool);
            Shuffle(shuffled);

            var options = new List<RoomOption>();
            int count   = Mathf.Min(2, shuffled.Count);
            for (int i = 0; i < count; i++)
                options.Add(MakeCombatOption(floorIdx, shuffled[i], InboxTab.Important,
                    baseEscalation: 2, rewardTierOverride: BumpTier(shuffled[i].rewardTier)));

            return options;
        }

        // Bumps a reward tier up by one step (caps at Boss).
        static RewardTier BumpTier(RewardTier tier) => tier switch
        {
            RewardTier.Common   => RewardTier.Uncommon,
            RewardTier.Uncommon => RewardTier.Rare,
            _                   => RewardTier.Boss,
        };

        static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j    = Random.Range(0, i + 1);
                T   tmp  = list[i];
                list[i]  = list[j];
                list[j]  = tmp;
            }
        }
    }
}
