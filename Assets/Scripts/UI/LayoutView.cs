using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Serialized child references for the InboxLayout prefab.
    /// FloorMapScreen reads these to wire up navigation and populate dynamic labels.
    public class LayoutView : MonoBehaviour
    {
        [Header("Nav Buttons")]
        public Button inboxButton;
        public Button draftsButton;
        public Button allMailButton;

        [Header("Dynamic Labels")]
        public TextMeshProUGUI floorInfoLabel;  // "FL 1  RM 2" — updated on Show()
        public TextMeshProUGUI draftsLabel;     // "DRAFTS  N" — updated with collection count

        [Header("Content Area")]
        public RectTransform contentArea;       // Pages are instantiated here
    }
}
