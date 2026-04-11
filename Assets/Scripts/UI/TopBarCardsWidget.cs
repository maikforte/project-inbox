using InboxZero.Core;
using TMPro;
using UnityEngine;

namespace InboxZero.UI
{
    /// Displays total deck card count (draw + hand + discard) in the InboxLayout top bar.
    /// Assign countLabel in the inspector.
    public class TopBarCardsWidget : MonoBehaviour
    {
        public TextMeshProUGUI countLabel;

        void Update()
        {
            if (GameManager.Instance == null || countLabel == null) return;
            int total = GameManager.Instance.DrawPile.Count
                      + GameManager.Instance.Hand.Count
                      + GameManager.Instance.DiscardPile.Count;
            countLabel.text = $"Cards : {total}";
        }
    }
}
