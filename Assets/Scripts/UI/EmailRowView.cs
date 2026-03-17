using InboxZero.Core;
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
        static readonly Color C_TextDark  = new Color(0.13f, 0.13f, 0.14f);
        static readonly Color C_TextMid   = new Color(0.37f, 0.39f, 0.41f);
        static readonly Color C_TextLight = new Color(0.62f, 0.64f, 0.67f);
        static readonly Color C_Accent    = new Color(0.10f, 0.45f, 0.91f);

        public void Bind(RoomOption opt, int floorDay)
        {
            bool isUnread = opt.type == RoomType.Combat;

            if (unreadDot != null)
            {
                unreadDot.gameObject.SetActive(isUnread);
                unreadDot.color = C_Accent;
            }

            Color senderCol = isUnread ? C_TextDark : C_TextMid;

            if (senderLabel != null)
            {
                senderLabel.text         = opt.senderName;
                senderLabel.color        = senderCol;
                senderLabel.overflowMode = TextOverflowModes.Ellipsis;
            }

            if (bodyLabel != null)
            {
                string subjectHex = ColorUtility.ToHtmlStringRGB(senderCol);
                string previewHex = ColorUtility.ToHtmlStringRGB(C_TextMid);
                bodyLabel.text        = $"<color=#{subjectHex}>{opt.subjectLine}</color>" +
                                        $"<color=#{previewHex}>  -  {opt.previewText}</color>";
                bodyLabel.overflowMode = TextOverflowModes.Ellipsis;
            }

            if (dateLabel != null)
            {
                dateLabel.text  = $"Mar {floorDay}";
                dateLabel.color = isUnread ? C_TextDark : C_TextLight;
            }
        }
    }
}
