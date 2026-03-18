using InboxZero.Enemies;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Drives the 5-segment sprite HP bar for the enemy panel.
    /// Each segment = 1/5 of the enemy's max HP.
    public class EnemyHPBar : MonoBehaviour
    {
        [Header("Fill Sprites (left to right)")]
        public Image[] fillSprites = new Image[5];

        [Header("Label")]
        public TextMeshProUGUI hpLabel;

        EnemyController _enemy;

        void Update()
        {
            if (_enemy == null) _enemy = FindObjectOfType<EnemyController>();
            if (_enemy == null || _enemy.Data == null) return;

            int hp    = Mathf.Clamp(_enemy.CurrentHP, 0, _enemy.Data.maxHP);
            int maxHp = _enemy.Data.maxHP;
            int fillCount = hp == 0 ? 0 : Mathf.CeilToInt((float)hp / maxHp * 5f);

            for (int i = 0; i < fillSprites.Length; i++)
            {
                if (fillSprites[i] == null) continue;
                var c = fillSprites[i].color;
                c.a = i < fillCount ? 1f : 0f;
                fillSprites[i].color = c;
            }

            if (hpLabel != null)
                hpLabel.text = $"{hp} / {maxHp}";
        }
    }
}
