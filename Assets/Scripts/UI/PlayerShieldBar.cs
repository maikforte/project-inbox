using InboxZero.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Drives the shield bar using a single filled Image (same approach as PlayerHPBar).
    /// Assign fillBar (Shield.png, Fill Horizontal) and shieldLabel in the inspector.
    public class PlayerShieldBar : MonoBehaviour
    {
        public Image fillBar;
        public TextMeshProUGUI shieldLabel;

        const int MaxShield = 50;

        void Update()
        {
            if (GameManager.Instance == null) return;

            int shield = Mathf.Clamp(GameManager.Instance.CurrentShield, 0, MaxShield);

            if (fillBar != null)
                fillBar.fillAmount = Mathf.Clamp01((float)shield / MaxShield);

            if (shieldLabel != null)
                shieldLabel.text = $"{shield} / {MaxShield}";
        }
    }
}
