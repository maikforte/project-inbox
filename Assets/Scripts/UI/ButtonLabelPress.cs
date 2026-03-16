using UnityEngine;
using UnityEngine.EventSystems;

namespace InboxZero.UI
{
    /// Shifts a child label by a small offset when the button is pressed,
    /// giving the illusion that the label moves with the button sprite.
    /// Add this to any Button GameObject and assign the label RectTransform.
    public class ButtonLabelPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Tooltip("The label RectTransform to shift on press.")]
        public RectTransform label;

        [Tooltip("How far to shift the label when pressed. Typically (0, -1) for 1px down.")]
        public Vector2 pressOffset = new Vector2(0, -1);

        Vector2 _restPosition;

        void Awake()
        {
            if (label != null)
                _restPosition = label.anchoredPosition;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (label != null)
                label.anchoredPosition = _restPosition + pressOffset;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (label != null)
                label.anchoredPosition = _restPosition;
        }
    }
}
