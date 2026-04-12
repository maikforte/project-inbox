using InboxZero.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Drives the HP bar using a single filled Image (same approach as TopBarHPWidget).
    /// Assign fillBar (HP.png, Fill Horizontal) and hpLabel in the inspector.
    public class PlayerHPBar : MonoBehaviour
    {
        public Image fillBar;
        public TextMeshProUGUI hpLabel;

        void Update()
        {
            if (GameManager.Instance == null) return;

            int hp = GameManager.Instance.CurrentHP;
            int maxHp = GameManager.Instance.MaxHP;

            if (fillBar != null)
                fillBar.fillAmount = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0f;

            if (hpLabel != null)
                hpLabel.text = $"{hp} / {maxHp}";
        }
    }
}
