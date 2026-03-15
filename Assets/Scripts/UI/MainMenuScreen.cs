using InboxZero.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Fullscreen main menu panel shown at game start and on return from game over / victory.
    /// Singleton. Placed in the scene; wire uiFont in the inspector.
    public class MainMenuScreen : MonoBehaviour
    {
        public static MainMenuScreen Instance { get; private set; }

        [Header("Font")]
        public TMP_FontAsset uiFont;

        Canvas _canvas;
        GameObject _panel;
        GameObject _optionsPanel;
        CombatSetup _combatSetup;

        // Colors
        static readonly Color C_Bg       = new Color(0.04f, 0.04f, 0.10f, 1f);
        static readonly Color C_BtnNew   = new Color(0.10f, 0.26f, 0.10f, 1f);
        static readonly Color C_BtnGray  = new Color(0.20f, 0.20f, 0.20f, 1f);
        static readonly Color C_BtnExit  = new Color(0.26f, 0.08f, 0.08f, 1f);
        static readonly Color C_BtnHover = new Color(0.30f, 0.30f, 0.30f, 1f);
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
            if (_canvas == null) { Debug.LogError("[MainMenuScreen] No Canvas found."); return; }

            _panel = new GameObject("MainMenuPanel", typeof(RectTransform));
            _panel.layer = 5;
            var rt = _panel.GetComponent<RectTransform>();
            rt.SetParent(_canvas.transform, false);
            rt.SetAsLastSibling();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = C_Bg;

            // ── Title block ─────────────────────────────────────────────────
            MakeLabel("Title", rt,
                Half, Half, Half,
                new Vector2(0, 80), new Vector2(360, 36),
                "INBOX // ZERO", 24, TextAlignmentOptions.Center);

            MakeLabel("Tagline", rt,
                Half, Half, Half,
                new Vector2(0, 52), new Vector2(300, 16),
                "THE INBOX IS A DUNGEON.", 14, TextAlignmentOptions.Center)
                .GetComponent<TextMeshProUGUI>().color = C_TextDim;

            // ── Buttons (centered column) ───────────────────────────────────
            // Continue (greyed — no save system)
            MakeButton("Continue", rt, new Vector2(0, 8),  C_BtnGray, "CONTINUE", dim: true);

            // New Game
            MakeButton("NewGame", rt, new Vector2(0, -26), C_BtnNew,  "NEW GAME",  onClick: OnNewGame);

            // Options
            MakeButton("Options", rt, new Vector2(0, -60), C_BtnGray, "OPTIONS",   onClick: OnOptions);

            // Exit
            MakeButton("Exit",    rt, new Vector2(0, -94), C_BtnExit, "EXIT",      onClick: OnExit);

            // ── Version tag ─────────────────────────────────────────────────
            MakeLabel("Version", rt,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-8, 8), new Vector2(120, 14),
                "v0.1 PROTOTYPE", 14, TextAlignmentOptions.Right)
                .GetComponent<TextMeshProUGUI>().color = C_TextDim;
        }

        public void Hide()
        {
            if (_panel != null) Destroy(_panel);
            _panel = null;
            CloseOptions();
        }

        // ── Button helpers ───────────────────────────────────────────────────

        void MakeButton(string name, RectTransform parent, Vector2 anchoredPos,
            Color bgColor, string label, UnityEngine.Events.UnityAction onClick = null, bool dim = false)
        {
            var go = new GameObject(name + "Button", typeof(RectTransform));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Half;
            rt.anchorMax = Half;
            rt.pivot = Half;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(160, 26);

            go.AddComponent<Image>().color = bgColor;

            var btn = go.AddComponent<Button>();
            if (onClick != null)
                btn.onClick.AddListener(onClick);

            if (dim)
            {
                var colors = btn.colors;
                colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 1f);
                btn.colors = colors;
                btn.interactable = false;
            }

            var lbl = MakeLabel("Label", rt,
                Vector2.zero, Vector2.one, Half,
                Vector2.zero, Vector2.zero,
                label, 14, TextAlignmentOptions.Center);
            lbl.GetComponent<TextMeshProUGUI>().enableWordWrapping = false;
            if (dim) lbl.GetComponent<TextMeshProUGUI>().color = C_TextDim;
        }

        // ── Options overlay ──────────────────────────────────────────────────

        void OnOptions()
        {
            if (_optionsPanel != null) return;

            // Dark scrim
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

            // Card panel
            var card = new GameObject("OptionsCard", typeof(RectTransform));
            card.layer = 5;
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.SetParent(rt, false);
            cardRt.anchorMin = Half;
            cardRt.anchorMax = Half;
            cardRt.pivot = Half;
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta = new Vector2(220, 120);
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

            // Close button
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

        // ── Label factory ─────────────────────────────────────────────────────

        GameObject MakeLabel(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size, string text, int fontSize,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = alignment;
            if (uiFont != null) tmp.font = uiFont;

            return go;
        }
    }
}
