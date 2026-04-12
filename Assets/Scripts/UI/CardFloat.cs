using UnityEngine;

namespace InboxZero.UI
{
    /// Applies a gentle vertical floating animation to a card in hand.
    /// Each card gets a random phase so they bob independently.
    /// Runs in LateUpdate so it always wins over HandDisplay's anchoredPosition writes.
    public class CardFloat : MonoBehaviour
    {
        [Tooltip("Vertical travel in pixels (peak-to-peak is 2x this).")]
        public float amplitude = 3f;

        [Tooltip("Full oscillation cycles per second.")]
        public float speed = 0.35f;

        [Tooltip("Max rotation in degrees (peak-to-peak is 2x this). Set to 0 to disable.")]
        public float rotationAmplitude = 1.5f;

        [Tooltip("How quickly the lift animates toward its target (higher = snappier).")]
        public float liftSmoothSpeed = 12f;

        RectTransform _rt;
        float _phase;
        float _liftTarget;
        float _liftCurrent;

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _phase = Random.Range(0f, Mathf.PI * 2f);
        }

        /// Called by HandDisplay to smoothly raise or lower this card.
        public void SetLiftTarget(float pixels) => _liftTarget = pixels;

        void LateUpdate()
        {
            _liftCurrent = Mathf.Lerp(_liftCurrent, _liftTarget, Time.deltaTime * liftSmoothSpeed);

            float t = Time.time * speed * Mathf.PI * 2f + _phase;

            var pos = _rt.anchoredPosition;
            pos.y = Mathf.Sin(t) * amplitude + _liftCurrent;
            _rt.anchoredPosition = pos;

            // Rotation lags the bob by a quarter-cycle so tilt and height feel coupled
            float rot = Mathf.Sin(t - Mathf.PI * 0.5f) * rotationAmplitude;
            _rt.localRotation = Quaternion.Euler(0f, 0f, rot);
        }
    }
}
