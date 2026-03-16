using TMPro;
using UnityEngine;

namespace InboxZero.UI
{
    /// Serialized child references for the DraftsPage prefab.
    /// DeckBuilderScreen reads these to populate the two-column deck builder.
    public class DraftsPageView : MonoBehaviour
    {
        [Header("Labels")]
        public TextMeshProUGUI statsLabel;
        public TextMeshProUGUI deckHeaderLabel;
        public TextMeshProUGUI collHeaderLabel;

        [Header("Scroll Content")]
        public RectTransform deckContent;   // Content RT — deck rows go here
        public RectTransform collContent;   // Content RT — collection rows go here
    }
}
