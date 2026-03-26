using InboxZero.Core;
using TMPro;
using UnityEngine;

namespace InboxZero.UI
{
    /// Displays the draw pile as a physical deck with a card-count label.
    /// Position this anywhere on the combat canvas — HandDisplay reads its
    /// world position as the origin for card-draw animations.
    public class DeckWidget : MonoBehaviour
    {
        public static DeckWidget Instance { get; private set; }

        [SerializeField] TextMeshProUGUI countLabel;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Update()
        {
            if (countLabel == null || GameManager.Instance == null) return;
            countLabel.text = GameManager.Instance.DrawPile.Count.ToString();
        }
    }
}
