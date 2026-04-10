using InboxZero.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Which page/tab a nav item opens.
    public enum NavTarget { Inbox, Important, Sent, Spam, Starred, Drafts, Snoozed }

    /// View script for the reusable NavItem prefab.
    /// Exposes serialized child refs so FloorMapScreen can wire clicks and swap sprites
    /// without any GetComponentInChildren or name-based Find calls.
    public class NavItemView : MonoBehaviour
    {
        [Header("References")]
        public Image             icon;        // optional — leave null if no icon
        public TextMeshProUGUI   label;
        public Button            button;
        public NavTabButton      navTabButton;

        [Header("Config")]
        public NavTarget target;

        void Start()
        {
            button?.onClick.AddListener(() => AudioManager.Instance?.PlayButtonClick());
        }
    }
}
