using InboxZero.Core;
using InboxZero.Enemies;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace InboxZero.Combat
{
    public class CombatResultManager : MonoBehaviour
    {
        [Header("Victory UI")]
        public GameObject victoryPanel;
        public TextMeshProUGUI victoryText;
        public Button victoryContinueButton;

        [Header("Game Over UI")]
        public GameObject gameOverPanel;
        public TextMeshProUGUI gameOverText;
        public Button gameOverRestartButton;

        // Wired externally (e.g. by a CombatSetup script) after the enemy spawns.
        public EnemyController Enemy { get; set; }

        // Optional callbacks for scene flow (floor map, restart scene, etc.).
        public UnityEvent OnVictoryContinued = new UnityEvent();
        public UnityEvent OnGameOverRestarted = new UnityEvent();

        void Start()
        {
            if (victoryPanel  != null) victoryPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            GameManager.Instance.OnPlayerDeath.AddListener(TriggerGameOver);

            if (victoryContinueButton  != null) victoryContinueButton.onClick.AddListener(OnContinue);
            if (gameOverRestartButton  != null) gameOverRestartButton.onClick.AddListener(OnRestart);
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnPlayerDeath.RemoveListener(TriggerGameOver);
        }

        // Call this after spawning the enemy so we can subscribe to OnDeath.
        public void RegisterEnemy(EnemyController enemy)
        {
            Enemy = enemy;
            enemy.OnDeath.AddListener(TriggerVictory);
        }

        // ── Outcomes ──────────────────────────────────────────────────────────

        public void TriggerVictory()
        {
            if (TurnManager.Instance.IsCombatEnded) return;

            TurnManager.Instance.IsCombatEnded = true;

            if (victoryPanel != null) victoryPanel.SetActive(true);
            if (victoryText  != null) victoryText.text = "INBOX CLEARED";
        }

        public void TriggerGameOver()
        {
            if (TurnManager.Instance.IsCombatEnded) return;

            TurnManager.Instance.IsCombatEnded = true;

            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            if (gameOverText  != null) gameOverText.text = "YOU HAVE BEEN UNSUBSCRIBED";
        }

        // ── Button handlers ───────────────────────────────────────────────────

        void OnContinue() => OnVictoryContinued.Invoke();
        void OnRestart()  => OnGameOverRestarted.Invoke();
    }
}
