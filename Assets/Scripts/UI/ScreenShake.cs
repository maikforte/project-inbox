using System.Collections;
using UnityEngine;

namespace InboxZero.UI
{
    /// Attach to the Main Camera. Call Shake() from anywhere via Instance.
    [RequireComponent(typeof(Camera))]
    public class ScreenShake : MonoBehaviour
    {
        public static ScreenShake Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        /// duration in seconds, magnitude in world units (0.1 ≈ 3 px at PPU 32).
        public void Shake(float duration = 0.18f, float magnitude = 0.10f)
            => StartCoroutine(ShakeRoutine(duration, magnitude));

        IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            var tf     = transform;
            var origin = tf.localPosition;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float decay = 1f - t / duration;
                tf.localPosition = origin + (Vector3)(Random.insideUnitCircle * (magnitude * decay));
                yield return null;
            }
            tf.localPosition = origin;
        }
    }
}
