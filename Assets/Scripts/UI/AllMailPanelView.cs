using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Holds serialized references to the child elements of the AllMailPanel prefab.
    /// AllMailScreen instantiates this prefab and populates dynamic fields via this component.
    public class AllMailPanelView : MonoBehaviour
    {
        [Header("Dynamic Labels")]
        public TextMeshProUGUI statsLabel;
        public TextMeshProUGUI columnHeaderLabel;

        [Header("Interaction")]
        public Button closeButton;

        [Header("Scroll")]
        public RectTransform scrollContent;
    }
}
