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
        CombatSetup _combatSetup;

        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

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
            _panel.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.10f, 1f);

            MakeLabel("Title", rt,
                Center, Center, Center,
                new Vector2(0, 70), new Vector2(340, 32),
                "INBOX // ZERO", 24, TextAlignmentOptions.Center);

            MakeLabel("Tagline", rt,
                Center, Center, Center,
                new Vector2(0, 36), new Vector2(300, 16),
                "THE INBOX IS A DUNGEON.", 14, TextAlignmentOptions.Center);

            MakeLabel("Flavour", rt,
                Center, Center, Center,
                new Vector2(0, 16), new Vector2(280, 14),
                "EVERY UNREAD IS A MONSTER.", 14, TextAlignmentOptions.Center);

            // Start Run button
            var btnGo = new GameObject("StartRunButton", typeof(RectTransform));
            btnGo.layer = 5;
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.SetParent(rt, false);
            btnRt.anchorMin = Center;
            btnRt.anchorMax = Center;
            btnRt.sizeDelta = new Vector2(140, 28);
            btnRt.anchoredPosition = new Vector2(0, -28);
            btnGo.AddComponent<Image>().color = new Color(0.12f, 0.30f, 0.12f);
            btnGo.AddComponent<Button>().onClick.AddListener(OnStartRun);
            MakeLabel("Label", btnRt,
                Vector2.zero, Vector2.one, Center,
                Vector2.zero, Vector2.zero, "START RUN", 14, TextAlignmentOptions.Center)
                .GetComponent<TextMeshProUGUI>().enableWordWrapping = false;

            MakeLabel("Version", rt,
                Center, Center, Center,
                new Vector2(0, -70), new Vector2(200, 12),
                "v0.1 -- PROTOTYPE", 14, TextAlignmentOptions.Center);
        }

        public void Hide()
        {
            if (_panel != null) Destroy(_panel);
            _panel = null;
        }

        void OnStartRun()
        {
            Hide();
            _combatSetup?.StartRun();
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
