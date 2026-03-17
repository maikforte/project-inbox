using InboxZero.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Drives the 5-segment sprite Shield bar.
    /// Each sprite covers 10 shield. Max shield: 50.
    /// Assign the 5 fill Image components (left → right) in the inspector.
    /// Container sprites are always visible and are not managed here.
    public class PlayerShieldBar : MonoBehaviour
    {
        [Header("Fill Sprites (left to right)")]
        public Image[] fillSprites = new Image[5];

        [Header("Label")]
        public TextMeshProUGUI shieldLabel;

        const int MaxShield = 50;
        const int ShieldPerSprite = 10;

        void Update()
        {
            if (GameManager.Instance == null) return;

            int shield = Mathf.Clamp(GameManager.Instance.CurrentShield, 0, MaxShield);
            // Each sprite covers 10 shield.
            // shield=0 → 0, shield=1-10 → 1, shield=11-20 → 2 ... shield=41-50 → 5
            int fillCount = shield == 0 ? 0 : Mathf.CeilToInt(shield / (float)ShieldPerSprite);

            for (int i = 0; i < fillSprites.Length; i++)
            {
                if (fillSprites[i] == null) continue;
                var c = fillSprites[i].color;
                c.a = i < fillCount ? 1f : 0f;
                fillSprites[i].color = c;
            }

            if (shieldLabel != null)
                shieldLabel.text = $"{shield} / {MaxShield}";
        }
    }
}
