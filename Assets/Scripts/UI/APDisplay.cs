using InboxZero.Core;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Drives the 3-pip AP display on the PlayerPanel.
    /// Each pip represents 1 AP. Assign the 3 pip Image components in the inspector.
    public class APDisplay : MonoBehaviour
    {
        [Header("Pip Images (left to right)")]
        public Image[] pips = new Image[3];

        void Update()
        {
            if (GameManager.Instance == null) return;

            int ap = Mathf.Clamp(GameManager.Instance.CurrentAP, 0, pips.Length);

            for (int i = 0; i < pips.Length; i++)
            {
                if (pips[i] == null) continue;
                var c = pips[i].color;
                c.a = i < ap ? 1f : 0.2f;
                pips[i].color = c;
            }
        }
    }
}
