using System.Collections;
using TMPro;
using UnityEngine;

namespace InboxZero.UI
{
    /// Spawns a floating damage/heal number that drifts upward and fades out.
    /// Call FloatingText.Spawn() from any damage or heal site.
    public class FloatingText : MonoBehaviour
    {
        static Canvas _canvas;

        /// <summary>
        /// Spawn a floating label near <paramref name="origin"/> in canvas space.
        /// </summary>
        /// <param name="text">Text to display (e.g. "-14").</param>
        /// <param name="origin">RectTransform whose screen position is the spawn anchor.</param>
        /// <param name="color">Text colour.</param>
        public static void Spawn(string text, RectTransform origin, Color color)
        {
            if (_canvas == null) _canvas = FindObjectOfType<Canvas>();
            if (_canvas == null || origin == null) return;

            var go = new GameObject("FloatingText", typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(_canvas.transform, false);
            rt.SetAsLastSibling();
            rt.sizeDelta = new Vector2(70, 22);

            // Convert origin's screen position to canvas-local space.
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)_canvas.transform,
                    origin.position,
                    null,
                    out Vector2 localPos))
            {
                rt.anchoredPosition = localPos + Vector2.up * 16f;
            }

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = 16;
            tmp.color     = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;

            go.AddComponent<FloatingText>();
        }

        IEnumerator Start()
        {
            var rt  = (RectTransform)transform;
            var tmp = GetComponent<TextMeshProUGUI>();
            var startPos = rt.anchoredPosition;
            const float Duration = 0.75f;
            float t = 0f;

            while (t < Duration)
            {
                t += Time.deltaTime;
                float p  = t / Duration;
                rt.anchoredPosition = startPos + Vector2.up * (p * 40f);
                tmp.alpha           = 1f - Mathf.Pow(p, 1.4f);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
