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

            // Room 4 is always a forced combat — no choice.
            if (nextRoom >= RoomsPerFloor)
                return new List<RoomOption> { MakeOption(RoomType.Combat) };

            var options = new List<RoomOption>();
            bool hasRest = false;

            for (int i = 0; i < optionCount; i++)
            {
                // Ensure at most one rest stop in the list.
                bool offerRest = !hasRest && Random.value < restStopChance;
                var opt = MakeOption(offerRest ? RoomType.RestStop : RoomType.Combat);
                options.Add(opt);
                if (offerRest) hasRest = true;
            }

            return options;
        }

        static RoomOption MakeOption(RoomType type) => new RoomOption
        {
            type  = type,
            label = type == RoomType.Combat ? "COMBAT" : "REST STOP",
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
