using InboxZero.Core;
using TMPro;
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

        Canvas _canvas;
        GameObject _panel;
        GameObject _optionsPanel;
        CombatSetup _combatSetup;

        // Colors used by the dynamic options overlay (still built in code)
        static readonly Color C_BtnGray  = new Color(0.20f, 0.20f, 0.20f, 1f);
        static readonly Color C_TextDim  = new Color(0.45f, 0.45f, 0.45f, 1f);
        static readonly Color C_OverlayBg= new Color(0f, 0f, 0f, 0.72f);

        static readonly Vector2 Half = new Vector2(0.5f, 0.5f);

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
            rt.anchorMin  = Vector2.zero;
            rt.anchorMax  = Vector2.one;
            rt.offsetMin  = Vector2.zero;
            rt.offsetMax  = Vector2.zero;

            // Wire button callbacks by child name
            WireButton("NewGameButton", OnNewGame);
            WireButton("OptionsButton", OnOptions);
            WireButton("ExitButton",    OnExit);
        }

        public void Hide()
        {
            if (_panel != null) Destroy(_panel);
            _panel = null;
            CloseOptions();
        }

        // ── Options overlay (still built dynamically) ────────────────────────

        void OnOptions()
        {
            if (_optionsPanel != null) return;

            _optionsPanel = new GameObject("OptionsOverlay", typeof(RectTransform));
            _optionsPanel.layer = 5;
            var rt = _optionsPanel.GetComponent<RectTransform>();
            rt.SetParent(_canvas.transform, false);
            rt.SetAsLastSibling();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _optionsPanel.AddComponent<Image>().color = C_OverlayBg;

            var card = new GameObject("OptionsCard", typeof(RectTransform));
            card.layer = 5;
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.SetParent(rt, false);
            cardRt.anchorMin        = Half;
            cardRt.anchorMax        = Half;
            cardRt.pivot            = Half;
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta        = new Vector2(220, 120);
            card.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.15f, 1f);

            MakeLabel("Title", cardRt,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -18), new Vector2(200, 16),
                "OPTIONS", 14, TextAlignmentOptions.Center);

            MakeLabel("Placeholder", cardRt,
                Half, Half, Half,
                new Vector2(0, 10), new Vector2(190, 14),
                "NO OPTIONS YET.", 14, TextAlignmentOptions.Center)
                .GetComponent<TextMeshProUGUI>().color = C_TextDim;

            MakeButton("Close", cardRt, new Vector2(0, -38), C_BtnGray, "CLOSE", onClick: CloseOptions);
        }

        void CloseOptions()
        {
            if (_optionsPanel != null) Destroy(_optionsPanel);
            _optionsPanel = null;
        }

        // ── Actions ──────────────────────────────────────────────────────────

        void OnNewGame()
        {
            Hide();
            _combatSetup?.StartRun();
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
            var child = _panel.transform.Find(childName);
            if (child == null) return;
            var btn = child.GetComponent<Button>();
            if (btn != null && btn.interactable) btn.onClick.AddListener(callback);
        }

        void MakeButton(string name, RectTransform parent, Vector2 anchoredPos,
            Color bgColor, string label, UnityEngine.Events.UnityAction onClick = null)
        {
            var go = new GameObject(name + "Button", typeof(RectTransform));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin        = Half;
            rt.anchorMax        = Half;
            rt.pivot            = Half;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = new Vector2(160, 26);
            go.AddComponent<Image>().color = bgColor;
            var btn = go.AddComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(onClick);
            MakeLabel("Label", rt, Vector2.zero, Vector2.one, Half, Vector2.zero, Vector2.zero,
                label, 14, TextAlignmentOptions.Center);
        }

        GameObject MakeLabel(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size, string text, int fontSize,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.color     = Color.white;
            tmp.alignment = alignment;
            return go;
        }
    }
}
