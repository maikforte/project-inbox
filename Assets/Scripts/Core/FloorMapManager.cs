using System.Collections.Generic;
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
    }

    /// Generates room options and advances run state when a room is selected.
    public class FloorMapManager : MonoBehaviour
    {
        public static FloorMapManager Instance { get; private set; }

        public const int RoomsPerFloor = 4;

        [Tooltip("How many options to offer (excluding forced Room 4).")]
        [Range(2, 3)] public int optionCount = 2;

        [Tooltip("Probability (0–1) that any given option is a Rest Stop.")]
        [Range(0f, 1f)] public float restStopChance = 0.30f;

        // ── Flavor text tables ────────────────────────────────────────────────

        static readonly string[][] SendersByFloor =
        {
            new[] { "Newsletter Flood", "Calendar Invite", "Mailing List" },
            new[] { "Reply-All Demon", "Auto-CC Manager", "Thread Hijacker" },
            new[] { "Out-of-Office Loop", "Passive-Aggressive Karen" },
            new[] { "The Thread That Never Ends" },
        };

        static readonly string[][] SubjectsByFloor =
        {
            new[] { "Fw: Important Update", "Re: Meeting Tomorrow", "Unsubscribe Confirmation" },
            new[] { "Re: Re: Re: That Thing", "FYI (no action needed)", "Following Up..." },
            new[] { "Per My Last Email", "As Previously Stated", "URGENT: Please Advise" },
            new[] { "Re: Re: Fw: Re: Fw: Re: Friday Lunch?" },
        };

        static readonly string[] PreviewsByFloor =
        {
            "5-7 dmg/turn",
            "8-9 dmg/turn",
            "10-11 dmg/turn  *  REGEN",
            "14 dmg/turn  *  5 HP REGEN  *  FINAL BOSS",
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

        // ── Option generation ─────────────────────────────────────────────────

        /// Returns the options available for the room AFTER the current one.
        public List<RoomOption> GenerateNextOptions()
        {
            int nextRoom = GameManager.Instance.CurrentRoom + 1;
            int floorIdx = Mathf.Clamp(GameManager.Instance.CurrentFloor - 1, 0, 3);

            // Room 4 is always a forced combat — no choice.
            if (nextRoom >= RoomsPerFloor)
                return new List<RoomOption> { MakeCombatOption(floorIdx) };

            var options = new List<RoomOption>();
            bool hasRest = false;

            for (int i = 0; i < optionCount; i++)
            {
                bool offerRest = !hasRest && Random.value < restStopChance;
                options.Add(offerRest ? MakeRestOption() : MakeCombatOption(floorIdx));
                if (offerRest) hasRest = true;
            }

            return options;
        }

        RoomOption MakeCombatOption(int floorIdx)
        {
            var senders  = SendersByFloor[floorIdx];
            var subjects = SubjectsByFloor[floorIdx];
            return new RoomOption
            {
                type        = RoomType.Combat,
                label       = "COMBAT",
                senderName  = senders[Random.Range(0, senders.Length)],
                subjectLine = subjects[Random.Range(0, subjects.Length)],
                previewText = PreviewsByFloor[floorIdx],
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

        // ── Progression ───────────────────────────────────────────────────────

        /// Advances CurrentRoom and returns whether the floor is now complete.
        public bool AdvanceRoom()
        {
            GameManager.Instance.CurrentRoom++;
            return GameManager.Instance.CurrentRoom > RoomsPerFloor;
        }
    }
}
