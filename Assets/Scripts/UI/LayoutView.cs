using TMPro;
using UnityEngine;

namespace InboxZero.UI
{
    /// Serialized child references for the InboxLayout prefab.
    /// FloorMapScreen reads these to wire up navigation and populate dynamic labels.
    public class LayoutView : MonoBehaviour
    {
        [Header("Nav Items")]
        public NavItemView[] navItems;          // All sidebar nav buttons, in display order

        [Header("Dynamic Labels")]
        public TextMeshProUGUI floorInfoLabel;  // "FL 1  RM 2" — updated on Show()
        public TextMeshProUGUI draftsLabel;     // "DRAFTS  N" — updated with collection count

        [Header("Content Area")]
        public RectTransform contentArea;       // Pages are instantiated here
    }
}
