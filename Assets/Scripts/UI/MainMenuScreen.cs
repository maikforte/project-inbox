using InboxZero.Core;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Fullscreen main menu panel shown at game start and on return from game over / victory.
    /// Singleton. Assign panelPrefab (MainMenuPanel.prefab) in the inspector.
    public class MainMenuScreen : MonoBehaviour
    {
        public static MainMenuScreen Instance { get; private set; }

        [SerializeField] GameObject panelPrefab;

        Canvas      _canvas;
        GameObject  _panel;
        CombatSetup _combatSetup;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _canvas = FindObjectOfType<Canvas>();
        }

        public void Init(CombatSetup setup) => _combatSetup = setup;

        public void Show()
        {
            if (_panel != null) Destroy(_panel);
            if (_canvas == null) _canvas = FindObjectOfType<Canvas>();

            if (panelPrefab == null)
            {
                Debug.LogError("[MainMenuScreen] panelPrefab not assigned.");
                return;
            }
            if (_canvas == null)
            {
                Debug.LogError("[MainMenuScreen] No Canvas found.");
                return;
            }

            _panel = Instantiate(panelPrefab, _canvas.transform);
            var rt = _panel.GetComponent<RectTransform>();
            rt.SetAsLastSibling();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            WireButton("NewGameButton", OnNewGame);
            WireButton("AllMailButton", OnAllMail);
            WireButton("ExitButton",    OnExit);
        }

        public void Hide()
        {
            if (_panel != null) Destroy(_panel);
            _panel = null;
        }

        // ── Actions ──────────────────────────────────────────────────────────

        void OnNewGame()
        {
            Hide();
            _combatSetup?.StartRun();
        }

        void OnAllMail()
        {
            AllMailScreen.Instance?.Show();
        }

        void OnExit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        void WireButton(string childName, UnityEngine.Events.UnityAction callback)
        {
            foreach (var btn in _panel.GetComponentsInChildren<Button>())
            {
                if (btn.name == childName && btn.interactable)
                {
                    btn.onClick.AddListener(callback);
                    return;
                }
            }
        }
    }
}
