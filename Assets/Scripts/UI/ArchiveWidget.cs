using InboxZero.Core;
using TMPro;
using UnityEngine;

namespace InboxZero.UI
{
    /// Displays the discard pile count. Mirrors DeckWidget but for the archive (left side).
    /// HandDisplay reads its world position as the destination for card-discard animations.
    public class ArchiveWidget : MonoBehaviour
    {
        public static ArchiveWidget Instance { get; private set; }

        [SerializeField] TextMeshProUGUI countLabel;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Update()
        {
            if (countLabel == null || GameManager.Instance == null) return;
            countLabel.text = GameManager.Instance.DiscardPile.Count.ToString();
        }
    }
}
