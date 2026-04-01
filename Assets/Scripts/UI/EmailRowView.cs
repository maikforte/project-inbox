using InboxZero.Core;
using InboxZero.Enemies;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Sits on the EmailRow prefab. Call Bind() to populate a row with room data.
    public class EmailRowView : MonoBehaviour
    {
        [Header("Child References")]
        public Image              unreadDot;
        public TextMeshProUGUI    senderLabel;
        public TextMeshProUGUI    bodyLabel;
        public TextMeshProUGUI    dateLabel;
        public Button             button;

        // Urgent tag colour only — all other label colours come from the prefab.
        static readonly Color C_Esc3 = new Color(1.00f, 0.20f, 0.20f);  // #FF3333

        public void Bind(RoomOption opt, int floorDay, int escalation = 0)
        {
            // totalEscalation = tab-level base + dynamic (per-room) escalation
            int  totalEscalation = opt.baseEscalation + escalation;
            bool isUnread        = opt.type == RoomType.Combat;
            bool isUrgent        = opt.baseEscalation >= 2;

            if (unreadDot != null)
                unreadDot.gameObject.SetActive(isUnread);

            if (senderLabel != null)
            {
                senderLabel.text         = opt.senderName.ToUpper();
                senderLabel.overflowMode = TextOverflowModes.Ellipsis;
            }

            if (bodyLabel != null)
            {
                string preview   = BuildPreview(opt, totalEscalation).ToUpper();
                string urgentTag = isUrgent ? $"<color=#{ColorUtility.ToHtmlStringRGB(C_Esc3)}>!! URGENT  </color>" : "";
                bodyLabel.text        = urgentTag + $"{opt.subjectLine.ToUpper()}  -  {preview}";
                bodyLabel.overflowMode = TextOverflowModes.Ellipsis;
            }

            if (dateLabel != null)
                dateLabel.text = totalEscalation > 0 ? $"+{totalEscalation}" : $"MAR {floorDay}";
        }

        static string BuildPreview(RoomOption opt, int totalEscalation)
        {
            if (opt.enemyData == null || totalEscalation == 0) return opt.previewText;

            int scaledHP  = opt.enemyData.maxHP  + totalEscalation * EnemyController.EscalationHPBonus;
            int scaledDmg = opt.enemyData.damagePerTurn + totalEscalation * EnemyController.EscalationDmgBonus;
            string preview = $"{scaledHP} HP (+{totalEscalation * EnemyController.EscalationHPBonus})" +
                             $"  *  {scaledDmg} DMG/TURN (+{totalEscalation * EnemyController.EscalationDmgBonus})";
            if (opt.enemyData.regenPerTurn > 0) preview += "  *  REGEN";
            return preview;
        }


    }
}
