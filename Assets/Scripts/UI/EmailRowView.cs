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

        // ── Palette (matches FloorMapScreen / InboxScreen) ────────────────────
        static readonly Color C_TextDark  = new Color(1.00f, 1.00f, 1.00f);
        static readonly Color C_TextMid   = new Color(0.75f, 0.75f, 0.78f);
        static readonly Color C_TextLight = new Color(0.50f, 0.50f, 0.55f);
        static readonly Color C_Accent    = new Color(0.10f, 0.45f, 0.91f);

        // Escalation threat colours: +1 yellow → +2 orange → +3 red
        static readonly Color C_Esc1 = new Color(1.00f, 0.80f, 0.27f);  // #FFCC44
        static readonly Color C_Esc2 = new Color(1.00f, 0.53f, 0.20f);  // #FF8833
        static readonly Color C_Esc3 = new Color(1.00f, 0.20f, 0.20f);  // #FF3333

        public void Bind(RoomOption opt, int floorDay, int escalation = 0)
        {
            // totalEscalation = tab-level base + dynamic (per-room) escalation
            int  totalEscalation = opt.baseEscalation + escalation;
            bool isUnread        = opt.type == RoomType.Combat;
            bool isUrgent        = opt.baseEscalation >= 2;

            // Dot — escalated combat rows shift from blue to threat colour.
            Color dotColor    = ThreatColor(totalEscalation, C_Accent);
            Color senderColor = isUnread ? C_TextDark : C_TextMid;
            if (totalEscalation > 0 && isUnread) senderColor = dotColor;

            if (unreadDot != null)
            {
                unreadDot.gameObject.SetActive(isUnread);
                unreadDot.color = dotColor;
            }

            if (senderLabel != null)
            {
                senderLabel.text         = opt.senderName;
                senderLabel.color        = senderColor;
                senderLabel.overflowMode = TextOverflowModes.Ellipsis;
            }

            if (bodyLabel != null)
            {
                string subjectHex = ColorUtility.ToHtmlStringRGB(senderColor);
                string previewHex = ColorUtility.ToHtmlStringRGB(C_TextMid);
                string preview    = BuildPreview(opt, totalEscalation);
                string urgentTag  = isUrgent ? $"<color=#{ColorUtility.ToHtmlStringRGB(C_Esc3)}>!! URGENT  </color>" : "";
                bodyLabel.text        = urgentTag +
                                        $"<color=#{subjectHex}>{opt.subjectLine}</color>" +
                                        $"<color=#{previewHex}>  -  {preview}</color>";
                bodyLabel.overflowMode = TextOverflowModes.Ellipsis;
            }

            string dateStr = totalEscalation > 0 ? $"+{totalEscalation}" : $"Mar {floorDay}";
            Color  dateCol = totalEscalation > 0 ? dotColor
                           : (isUnread ? C_TextDark : C_TextLight);
            if (dateLabel != null)
            {
                dateLabel.text  = dateStr;
                dateLabel.color = dateCol;
            }
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

        static Color ThreatColor(int escalation, Color baseColor) => escalation switch
        {
            0 => baseColor,
            1 => C_Esc1,
            2 => C_Esc2,
            _ => C_Esc3,
        };
    }
}
