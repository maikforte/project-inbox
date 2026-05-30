using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Drives hit-shake and color-flash effects for the enemy and player panels.
    /// Place on any scene GO and wire the fields in the inspector.
    public class CombatFX : MonoBehaviour
    {
        public static CombatFX Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] RectTransform enemyPanel;
        [SerializeField] RectTransform playerPanel;

        [Header("Hit Flash Targets")]
        [SerializeField] Image enemyFrame;   // EnemyPanel > Frame
        [SerializeField] Image playerFrame;  // PlayerPanel (the panel itself)

        [Header("Tuning")]
        [SerializeField] float panelShakeDuration  = 0.22f;
        [SerializeField] float panelShakeMagnitude = 5f;

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
        }

        // ── Public hit triggers ───────────────────────────────────────────────

        public RectTransform PlayerPanel => playerPanel;

        public void EnemyHit(Image portrait = null)
        {
            if (_enemyShake != null) StopCoroutine(_enemyShake);
            _enemyShake = StartCoroutine(ShakeRoutine(enemyPanel, _enemyHome, portrait != null ? portrait : enemyFrame));
        }

        public void PlayerHit()
        {
            if (_playerShake != null) StopCoroutine(_playerShake);
            _playerShake = StartCoroutine(ShakeRoutine(playerPanel, _playerHome, playerFrame));
            ScreenShake.Instance?.Shake();
        }

        // ── Internals ─────────────────────────────────────────────────────────

        IEnumerator ShakeRoutine(RectTransform panel, Vector2 home, Image target)
        {
            if (target != null)
                StartCoroutine(PlayHitFX(target));

            for (float t = 0f; t < panelShakeDuration; t += Time.deltaTime)
            {
                if (panel == null) yield break;
                float decay = 1f - t / panelShakeDuration;
                panel.anchoredPosition = home + Random.insideUnitCircle * (panelShakeMagnitude * decay);
                yield return null;
            }

            if (panel != null) panel.anchoredPosition = home;
        }

        static readonly Color HitRed = new Color(1f, 0.15f, 0.15f, 1f);

        IEnumerator PlayHitFX(Image target)
        {
            Color original = target.color;

            // Phase 0 — Flash white (0.04 s)
            const float FlashIn = 0.04f;
            for (float t = 0f; t < FlashIn; t += Time.deltaTime)
            {
                target.color = Color.Lerp(original, Color.white, Mathf.Clamp01(t / FlashIn));
                yield return null;
            }
            target.color = Color.white;

            // Phase 1 — White → red (0.10 s)
            const float TintDuration = 0.10f;
            for (float t = 0f; t < TintDuration; t += Time.deltaTime)
            {
                target.color = Color.Lerp(Color.white, HitRed, t / TintDuration);
                yield return null;
            }
            target.color = HitRed;

            // Phase 2 — Red → original (0.25 s)
            const float FadeOut = 0.25f;
            for (float t = 0f; t < FadeOut; t += Time.deltaTime)
            {
                target.color = Color.Lerp(HitRed, original, Mathf.Clamp01(t / FadeOut));
                yield return null;
            }

            target.color = original;
        }
    }
}
