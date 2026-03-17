using UnityEngine;

namespace InboxZero.UI
{
    /// Serialized child references for the InboxPage prefab.
    /// FloorMapScreen reads emailListRoot to populate dynamic email rows.
    public class InboxPageView : MonoBehaviour
    {
        [Header("Email List")]
        public RectTransform emailListRoot;  // Dynamic email rows are instantiated here
    }
}
