using InboxZero.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    public class EnemyIntentWidget : MonoBehaviour
    {
        public static EnemyIntentWidget Instance { get; private set; }

        [SerializeField] Image typeBar;
        [SerializeField] TextMeshProUGUI cardNameText;
        [SerializeField] TextMeshProUGUI descText;

        static readonly Color ColorAttack  = new Color(1f,    0.25f, 0.25f);
        static readonly Color ColorDefend  = new Color(0.25f, 0.75f, 0.35f);
        static readonly Color ColorSpecial = new Color(0.6f,  0.35f, 0.9f);
        static readonly Color ColorFrozen  = new Color(0.4f,  0.8f,  1f);

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void ShowIntent(CardData card)
        {
            if (card == null) { Hide(); return; }
            gameObject.SetActive(true);
            cardNameText.text = card.cardName.ToUpper();
            descText.text     = card.effectDescription;
            typeBar.color = card.cardType switch
            {
                CardType.Attack => ColorAttack,
                CardType.Defend => ColorDefend,
                _               => ColorSpecial,
            };
        }

        public void ShowFrozen()
        {
            gameObject.SetActive(true);
            cardNameText.text = "FROZEN";
            descText.text     = "Skipping attack.";
            typeBar.color     = ColorFrozen;
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
