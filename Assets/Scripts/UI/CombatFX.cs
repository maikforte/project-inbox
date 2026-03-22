using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Drives hit-shake and animated slice effects for the enemy and player panels.
    /// Place on any scene GO and wire the fields in the inspector.
    public class CombatFX : MonoBehaviour
    {
        public static CombatFX Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] RectTransform enemyPanel;
        [SerializeField] RectTransform playerPanel;

        [Header("Hit Overlays (Image component on each panel)")]
        [SerializeField] Image enemyHitOverlay;
        [SerializeField] Image playerHitOverlay;

        [Header("Animation Frames (optional — 6 sprites sliced from your spritesheet)")]
        [Tooltip("6-frame hit animation for the enemy panel.")]
        [SerializeField] Sprite[] enemyHitFrames;
        [Tooltip("6-frame hit animation for the player panel.")]
        [SerializeField] Sprite[] playerHitFrames;

        [Header("Tuning")]
        [SerializeField] float panelShakeDuration  = 0.22f;
        [SerializeField] float panelShakeMagnitude = 5f;
        [Tooltip("Frames per second for the hit sprite animation.")]
        [SerializeField] float hitAnimFPS = 18f;

        Vector2 _enemyHome;
        Vector2 _playerHome;

        Coroutine _enemyShake;
        Coroutine _playerShake;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void Start()
        {
            if (enemyPanel  != null) _enemyHome  = enemyPanel.anchoredPosition;
            if (playerPanel != null) _playerHome = playerPanel.anchoredPosition;

            if (enemyHitOverlay  != null) enemyHitOverlay.enabled  = false;
            if (playerHitOverlay != null) playerHitOverlay.enabled = false;
        }

        // ── Public hit triggers ───────────────────────────────────────────────

        public void EnemyHit()
        {
            if (_enemyShake != null) StopCoroutine(_enemyShake);
            _enemyShake = StartCoroutine(ShakeRoutine(enemyPanel, _enemyHome, enemyHitOverlay, enemyHitFrames));
        }

        public void PlayerHit()
        {
            if (_playerShake != null) StopCoroutine(_playerShake);
            _playerShake = StartCoroutine(ShakeRoutine(playerPanel, _playerHome, playerHitOverlay, playerHitFrames));
            ScreenShake.Instance?.Shake();
        }

        // ── Internals ─────────────────────────────────────────────────────────

        IEnumerator ShakeRoutine(RectTransform panel, Vector2 home, Image overlay, Sprite[] frames)
        {
            if (overlay != null)
                StartCoroutine(PlayFrames(overlay, frames));

            for (float t = 0f; t < panelShakeDuration; t += Time.deltaTime)
            {
                if (panel == null) yield break;
                float decay = 1f - t / panelShakeDuration;
                panel.anchoredPosition = home + Random.insideUnitCircle * (panelShakeMagnitude * decay);
                yield return null;
            }

            if (panel != null) panel.anchoredPosition = home;
        }

        IEnumerator PlayFrames(Image overlay, Sprite[] frames)
        {
            overlay.enabled = true;
            var c = overlay.color;

            // If no frames assigned, fall back to a plain white flash
            if (frames == null || frames.Length == 0)
            {
                float flashDur = 6f / hitAnimFPS; // same duration as 6 frames would take
                for (float t = 0f; t < flashDur; t += Time.deltaTime)
                {
                    c.a = 1f - t / flashDur;
                    overlay.color = c;
                    yield return null;
                }
                overlay.enabled = false;
                yield break;
            }

            // Play each frame for one tick at the chosen FPS, then hide
            float secondsPerFrame = 1f / hitAnimFPS;
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null) overlay.sprite = frames[i];

                // Fade alpha across the full animation so the last frame is transparent
                c.a = 1f - (float)i / frames.Length;
                overlay.color = c;

                yield return new WaitForSeconds(secondsPerFrame);
            }

            overlay.enabled = false;
        }
    }
}
