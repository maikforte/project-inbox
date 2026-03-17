using InboxZero.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Drives the 5-segment sprite HP bar.
    /// Assign the 5 fill Image components (left → right) in the inspector.
    /// Container sprites are always visible and are not managed here.
    public class PlayerHPBar : MonoBehaviour
    {
        [Header("Fill Sprites (left to right)")]
        public Image[] fillSprites = new Image[5];

        [Header("Label")]
        public TextMeshProUGUI hpLabel;

        void Update()
        {
            if (GameManager.Instance == null) return;

            int hp = Mathf.Clamp(GameManager.Instance.CurrentHP, 0, 100);
            // Each sprite covers 20 HP. Show sprites from left up to the count needed.
            // hp=0 → 0, hp=1-20 → 1, hp=21-40 → 2 ... hp=81-100 → 5
            int fillCount = hp == 0 ? 0 : Mathf.CeilToInt(hp / 20f);

            for (int i = 0; i < fillSprites.Length; i++)
            {
                if (fillSprites[i] == null) continue;
                var c = fillSprites[i].color;
                c.a = i < fillCount ? 1f : 0f;
                fillSprites[i].color = c;
            }

            if (hpLabel != null)
                hpLabel.text = $"{hp} / {GameManager.Instance.MaxHP}";
        }
    }
}
