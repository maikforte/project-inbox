using UnityEngine;

namespace InboxZero.UI
{
    /// Gently bobs the RectTransform up and down on the Y axis.
    /// Stagger is seeded from the sibling index so rows in a list don't all move in unison.
    public class FloatyAnimation : MonoBehaviour
    {
        [Header("Settings")]
        public float amplitude = 2f;    // pixels of travel each way
        public float speed     = 1.2f;  // cycles per second

        RectTransform _rt;
        float         _baseY;
        float         _phase;

        void Awake()
        {
            _rt    = GetComponent<RectTransform>();
            _phase = transform.GetSiblingIndex() * 0.55f;
        }

        void Start()
        {
            // Captured in Start, not Awake — position is set by BuildRows() after Instantiate,
            // so Awake would always read 0.
            _baseY = _rt.anchoredPosition.y;
        }

        void Update()
        {
            float y    = _baseY + Mathf.Sin((Time.time * speed + _phase) * Mathf.PI * 2f) * amplitude;
            var   pos  = _rt.anchoredPosition;
            pos.y      = y;
            _rt.anchoredPosition = pos;
        }
    }
}
