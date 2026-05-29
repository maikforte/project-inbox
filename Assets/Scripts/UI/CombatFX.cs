using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Drives hit-shake and pixel-art slash particle effects for the enemy and player panels.
    /// Assign hitFXMaterial (using InboxZero/HitFX shader) in the inspector.
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

        [Header("Hit FX Material (InboxZero/HitFX shader)")]
        [Tooltip("Material using the InboxZero/HitFX shader. A unique instance is created per overlay at runtime.")]
        [SerializeField] Material hitFXMaterial;

        [Header("Tuning")]
        [SerializeField] float panelShakeDuration  = 0.22f;
        [SerializeField] float panelShakeMagnitude = 5f;
        [Tooltip("Duration of the pixel slash animation in seconds.")]
        [SerializeField] float hitFXDuration = 0.65f;

        Vector2 _enemyHome;
        Vector2 _playerHome;

        Material _enemyMat;
        Material _playerMat;

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

            // Create per-overlay material instances so concurrent hits don't share state.
            if (hitFXMaterial != null)
            {
                _enemyMat  = new Material(hitFXMaterial);
                _playerMat = new Material(hitFXMaterial);
            }
        }

        void OnDestroy()
        {
            if (_enemyMat  != null) Destroy(_enemyMat);
            if (_playerMat != null) Destroy(_playerMat);
        }

        // ── Public hit triggers ───────────────────────────────────────────────

        public RectTransform PlayerPanel => playerPanel;

        public void EnemyHit()
        {
            if (_enemyShake != null) StopCoroutine(_enemyShake);
            _enemyShake = StartCoroutine(ShakeRoutine(enemyPanel, _enemyHome, enemyHitOverlay, _enemyMat));
        }

        public void PlayerHit()
        {
            if (_playerShake != null) StopCoroutine(_playerShake);
            _playerShake = StartCoroutine(ShakeRoutine(playerPanel, _playerHome, playerHitOverlay, _playerMat));
            ScreenShake.Instance?.Shake();
        }

        // ── Internals ─────────────────────────────────────────────────────────

        IEnumerator ShakeRoutine(RectTransform panel, Vector2 home, Image overlay, Material mat)
        {
            if (overlay != null)
                StartCoroutine(PlayHitFX(overlay, mat));

            for (float t = 0f; t < panelShakeDuration; t += Time.deltaTime)
            {
                if (panel == null) yield break;
                float decay = 1f - t / panelShakeDuration;
                panel.anchoredPosition = home + Random.insideUnitCircle * (panelShakeMagnitude * decay);
                yield return null;
            }

            if (panel != null) panel.anchoredPosition = home;
        }

        IEnumerator PlayHitFX(Image overlay, Material mat)
        {
            overlay.enabled = true;

            if (mat == null)
            {
                // Fallback: plain white flash when no material assigned.
                var c = overlay.color;
                for (float t = 0f; t < hitFXDuration; t += Time.deltaTime)
                {
                    c.a = 1f - t / hitFXDuration;
                    overlay.color = c;
                    yield return null;
                }
                c.a = 0f;
                overlay.color = c;
                overlay.enabled = false;
                yield break;
            }

            overlay.color = Color.white;
            overlay.material = mat;
            mat.SetFloat("_Progress", 0f);

            for (float t = 0f; t < hitFXDuration; t += Time.deltaTime)
            {
                mat.SetFloat("_Progress", t / hitFXDuration);
                yield return null;
            }

            mat.SetFloat("_Progress", 0f);
            overlay.material = null;  // restore default UI material
            overlay.enabled = false;
        }
    }
}
