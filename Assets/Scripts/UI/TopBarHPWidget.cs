using InboxZero.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Compact HP display for the InboxLayout top bar.
    /// Assign hpLabel and fillBar in the inspector.
    public class TopBarHPWidget : MonoBehaviour
    {
        public TextMeshProUGUI hpLabel;
        public Image fillBar;

        void Update()
        {
            if (GameManager.Instance == null) return;
            int hp = GameManager.Instance.CurrentHP;
            int maxHp = GameManager.Instance.MaxHP;
            if (hpLabel != null)
                hpLabel.text = $"HP: {hp} / {maxHp}";
            if (fillBar != null)
                fillBar.fillAmount = maxHp > 0 ? (float)hp / maxHp : 0f;
        }
    }
}
