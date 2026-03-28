using UnityEngine;

namespace InboxZero.Core
{
    /// Replaced by the guaranteed 3-card reward screen (TASK-38).
    /// Kept as a stub so existing scene references don't break.
    public class CardDropManager : MonoBehaviour
    {
        public static CardDropManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }
    }
}
