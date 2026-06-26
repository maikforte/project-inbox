using InboxZero.Core;
using InboxZero.Data;
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
        public UnityEvent OnVictoryContinued  = new UnityEvent();
        public UnityEvent OnGameOverRestarted = new UnityEvent();
        public UnityEvent OnGameVictory       = new UnityEvent();

        void Start()
        {
            if (victoryPanel  != null) victoryPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            if (GameManager.Instance != null)
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
            if (InboxZero.UI.HandDisplay.Instance != null) InboxZero.UI.HandDisplay.Instance.HidePreview();

            if (victoryPanel != null) victoryPanel.SetActive(true);

            if (GameManager.Instance.CurrentFloor >= 4)
            {
                var gm = GameManager.Instance;
                if (victoryText != null)
                    victoryText.text = $"INBOX ZERO ACHIEVED\n\nCARDS PLAYED:  {gm.CardsPlayed}\nDAMAGE DEALT:  {gm.DamageDealt}";
            }
            else
            {
                if (victoryText != null) victoryText.text = "INBOX CLEARED\n\nPICK A CARD";
            }
        }

        public void TriggerGameOver()
        {
            if (TurnManager.Instance.IsCombatEnded) return;
            TurnManager.Instance.IsCombatEnded = true;
            if (InboxZero.UI.HandDisplay.Instance != null) InboxZero.UI.HandDisplay.Instance.HidePreview();

            if (gameOverPanel != null) gameOverPanel.SetActive(true);

            int floor = GameManager.Instance.CurrentFloor;
            if (gameOverText != null)
                gameOverText.text = $"RUN TERMINATED\nREACHED FLOOR {floor}";

            // Relabel the restart button to make it clear it goes to main menu.
            var btnLabel = gameOverRestartButton?.GetComponentInChildren<TextMeshProUGUI>();
            if (btnLabel != null) btnLabel.text = "MAIN MENU";
        }

        // ── Button handlers ───────────────────────────────────────────────────

        void OnContinue()
        {
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (GameManager.Instance.CurrentFloor >= 4)
                OnGameVictory.Invoke();
            else
                OnVictoryContinued.Invoke();
        }

        void OnRestart()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            OnGameOverRestarted.Invoke();
        }

    }
}
