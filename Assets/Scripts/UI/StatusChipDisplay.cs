using System.Collections.Generic;
using InboxZero.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Renders Gmail-style coloured label chips for status effects.
    /// Place on a HorizontalLayoutGroup container; call Refresh() whenever statuses change.
    public class StatusChipDisplay : MonoBehaviour
    {
        [SerializeField] TMP_FontAsset chipFont;

        static readonly Color UnreadColor        = new Color(0.85f, 0.19f, 0.15f); // Gmail red
        static readonly Color GuiltColor         = new Color(0.95f, 0.60f, 0.00f); // amber
        static readonly Color AwaitingReplyColor = new Color(0.10f, 0.45f, 0.91f); // Gmail blue

        readonly List<GameObject> _chips = new List<GameObject>();

        public void Refresh(Dictionary<StatusEffectType, int> statuses)
        {
            foreach (var chip in _chips)
                if (chip != null) Destroy(chip);
            _chips.Clear();

            if (statuses == null) return;

            foreach (var kv in statuses)
            {
                if (kv.Value <= 0) continue;
                _chips.Add(BuildChip(kv.Key, kv.Value));
            }
        }

        GameObject BuildChip(StatusEffectType type, int stacks)
        {
            string label = type switch
            {
                StatusEffectType.Unread        => "UNREAD",
                StatusEffectType.Guilt         => "GUILT",
                StatusEffectType.AwaitingReply => "AWAIT",
                _                              => type.ToString().ToUpper()
            };

            string text = $"{label} {stacks}";

            Color bg = type switch
            {
                StatusEffectType.Unread        => UnreadColor,
                StatusEffectType.Guilt         => GuiltColor,
                StatusEffectType.AwaitingReply => AwaitingReplyColor,
                _                              => Color.gray
            };

            var chip = new GameObject($"Chip_{type}", typeof(RectTransform));
            chip.layer = 5;
            chip.transform.SetParent(transform, false);

            var chipRt = (RectTransform)chip.transform;
            chipRt.anchorMin = new Vector2(0, 0.5f);
            chipRt.anchorMax = new Vector2(0, 0.5f);
            chipRt.pivot     = new Vector2(0, 0.5f);
            chipRt.sizeDelta = new Vector2(text.Length * 5f + 8f, 12f);

            chip.AddComponent<Image>().color = bg;

            var lblGo = new GameObject("Label", typeof(RectTransform));
            lblGo.layer = 5;
            var lblRt = (RectTransform)lblGo.transform;
            lblRt.SetParent(chipRt, false);
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = Vector2.one;
            lblRt.offsetMin = new Vector2(2, 1);
            lblRt.offsetMax = new Vector2(-2, -1);

            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            tmp.text               = text;
            tmp.fontSize           = 9;
            tmp.color              = Color.white;
            tmp.alignment          = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode       = TextOverflowModes.Overflow;
            if (chipFont != null) tmp.font = chipFont;

            return chip;
        }
    }
}
