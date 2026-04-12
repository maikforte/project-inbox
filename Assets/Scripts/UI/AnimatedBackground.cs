using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Drives the animated background shader on the Combat scene's Background Image.
    /// Toggle Mode in the inspector to compare Plasma vs. NoiseDrift.
    public class AnimatedBackground : MonoBehaviour
    {
        public enum BackgroundMode { Plasma = 0, NoiseDrift = 1, DigitalRain = 2, GraphPaper = 3, PixelFire = 4, MutedRetro = 5, DarkEmbers = 6 }

        [Header("Mode")]
        public BackgroundMode mode = BackgroundMode.Plasma;

        [Header("Shared Settings")]
        [Range(0.05f, 2f)]  public float speed     = 0.4f;
        [Range(1f,   8f)]   public float scale     = 3f;
        [Range(1f,   8f)]   public float pixelSize = 2f;

        Material _mat;

        void Awake()
        {
            var img = GetComponent<Image>();
            if (img == null) return;

            // Instantiate so we never dirty the shared material asset.
            _mat = Instantiate(img.material);
            img.material = _mat;
        }

        void Update()
        {
            if (_mat == null) return;
            _mat.SetInt  ("_Mode",      (int)mode);
            _mat.SetFloat("_Speed",     speed);
            _mat.SetFloat("_Scale",     scale);
            _mat.SetFloat("_PixelSize", pixelSize);
        }
    }
}
