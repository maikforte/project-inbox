using System.Collections.Generic;
using InboxZero.Data;
using UnityEngine;

namespace InboxZero.Core
{
    public enum RoomType { Combat, RestStop }

    [System.Serializable]
    public struct RoomOption
    {
        public RoomType type;
        public string label;
        // Inbox display fields
        public string senderName;
        public string subjectLine;
        public string previewText;
        // Enemy reference — null for rest stops
        public EnemyData enemyData;
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

            // Fisher-Yates shuffle
            for (int i = options.Count - 1; i > 0; i--)
            {
                int j   = Random.Range(0, i + 1);
                var tmp = options[i];
                options[i] = options[j];
                options[j] = tmp;
            }

            return options;
        }

        // ── Option factories ─────────────────────────────────────────────────

        RoomOption MakeCombatOption(int floorIdx, EnemyData enemy)
        {
            var subjects = SubjectsByFloor[floorIdx];

            string preview = $"{enemy.maxHP} HP  *  {enemy.damagePerTurn} DMG/TURN";
            if (enemy.regenPerTurn > 0) preview += "  *  REGEN";

            return new RoomOption
            {
                type        = RoomType.Combat,
                label       = "COMBAT",
                senderName  = enemy.enemyName,
                subjectLine = subjects[Random.Range(0, subjects.Length)],
                previewText = preview,
                enemyData   = enemy,
            };
        }

        static RoomOption MakeRestOption() => new RoomOption
        {
            type        = RoomType.RestStop,
            label       = "REST STOP",
            senderName  = "Rest Stop",
            subjectLine = RestSubjects[Random.Range(0, RestSubjects.Length)],
            previewText = "Restore 15 HP.",
        };
    }
}
