using InboxZero.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Modal settings panel shown from the main menu.
    /// Builds all UI in code — no prefab required.
    public class SettingsScreen : MonoBehaviour
    {
        public static SettingsScreen Instance { get; private set; }

        [SerializeField] TMP_FontAsset font;

        Canvas     _canvas;
        GameObject _panel;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _canvas  = FindObjectOfType<Canvas>();
        }

        public void Show()
        {
            if (_panel != null) Destroy(_panel);
            _panel = Build();
        }

        public void Hide()
        {
            if (_panel != null) Destroy(_panel);
            _panel = null;
        }

        // ── Panel construction ────────────────────────────────────────────────

        GameObject Build()
        {
            // Fullscreen dark overlay — blocks clicks to the menu behind it
            var root   = MakeGO("SettingsPanel", _canvas.transform);
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            rootRt.SetAsLastSibling();
            root.AddComponent<CanvasRenderer>();
            root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);

            // Centred card
            var card   = MakeGO("Card", root.transform);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin        = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax        = new Vector2(0.5f, 0.5f);
            cardRt.pivot            = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta        = new Vector2(240f, 168f);
            card.AddComponent<CanvasRenderer>();
            card.AddComponent<Image>().color = new Color(0.08f, 0.06f, 0.12f, 1f);

            var vlg = card.AddComponent<VerticalLayoutGroup>();
            vlg.padding              = new RectOffset(12, 12, 10, 10);
            vlg.spacing              = 6f;
            vlg.childAlignment       = TextAnchor.UpperCenter;
            vlg.childControlWidth    = true;
            vlg.childControlHeight   = false;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;

            // Title
            AddLabel(card.transform, "SETTINGS", 18f, new Color(0.88f, 0.82f, 0.95f),
                     TextAlignmentOptions.Center, preferredH: 18f);

            // Divider
            var div = MakeGO("Divider", card.transform);
            div.AddComponent<LayoutElement>().preferredHeight = 1f;
            div.AddComponent<CanvasRenderer>();
            div.AddComponent<Image>().color = new Color(0.25f, 0.22f, 0.32f);

            // ── Audio rows ────────────────────────────────────────────────────
            var am = AudioManager.Instance;

            AddVolumeRow(card.transform, "MASTER",
                am != null ? am.MasterVolume : PlayerPrefs.GetFloat("vol_master", 1f),
                v => { if (am != null) am.MasterVolume = v; });

            AddVolumeRow(card.transform, "MUSIC",
                am != null ? am.MusicVolume : PlayerPrefs.GetFloat("vol_music", 0.5f),
                v => { if (am != null) am.MusicVolume = v; });

            AddVolumeRow(card.transform, "SFX",
                am != null ? am.SfxVolume : PlayerPrefs.GetFloat("vol_sfx", 1f),
                v => { if (am != null) am.SfxVolume = v; });

            // ── Display row ───────────────────────────────────────────────────
            AddFullscreenRow(card.transform);

            // Spacer pushes close button to the bottom
            MakeGO("Spacer", card.transform).AddComponent<LayoutElement>().flexibleHeight = 1f;

            // Close
            var closeGo = MakeSmallButton("CloseButton", card.transform, "CLOSE",
                                          () => Hide());
            closeGo.AddComponent<LayoutElement>().preferredHeight = 22f;

            return root;
        }

        // ── Row builders ──────────────────────────────────────────────────────

        void AddVolumeRow(Transform parent, string rowLabel, float initial, System.Action<float> onChange)
        {
            var row = MakeGO(rowLabel + "Row", parent);
            row.AddComponent<LayoutElement>().preferredHeight = 20f;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing              = 4f;
            hlg.childAlignment       = TextAnchor.MiddleLeft;
            hlg.childControlWidth    = false;
            hlg.childControlHeight   = true;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;

            // Setting label
            var lbl = MakeGO("Label", row.transform);
            lbl.AddComponent<LayoutElement>().preferredWidth = 76f;
            lbl.AddComponent<CanvasRenderer>();
            var lblTmp = lbl.AddComponent<TextMeshProUGUI>();
            SetupTMP(lblTmp, rowLabel, 14f, new Color(0.68f, 0.66f, 0.76f), TextAlignmentOptions.MidlineLeft);

            // Dec button
            var decGo = MakeControlButton("Dec", row.transform, "\u2013");

            // Value label
            float current = initial;
            var valGo = MakeGO("Value", row.transform);
            valGo.AddComponent<LayoutElement>().preferredWidth = 36f;
            valGo.AddComponent<CanvasRenderer>();
            var valTmp = valGo.AddComponent<TextMeshProUGUI>();
            SetupTMP(valTmp, Pct(current), 14f, Color.white, TextAlignmentOptions.Center);

            // Inc button
            var incGo = MakeControlButton("Inc", row.transform, "+");

            // Wire
            decGo.GetComponent<Button>().onClick.AddListener(() =>
            {
                current   = Mathf.Clamp(Mathf.Round((current - 0.1f) * 10f) / 10f, 0f, 1f);
                valTmp.text = Pct(current);
                onChange(current);
            });
            incGo.GetComponent<Button>().onClick.AddListener(() =>
            {
                current   = Mathf.Clamp(Mathf.Round((current + 0.1f) * 10f) / 10f, 0f, 1f);
                valTmp.text = Pct(current);
                onChange(current);
            });
        }

        void AddFullscreenRow(Transform parent)
        {
            var row = MakeGO("FullscreenRow", parent);
            row.AddComponent<LayoutElement>().preferredHeight = 20f;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing              = 4f;
            hlg.childAlignment       = TextAnchor.MiddleLeft;
            hlg.childControlWidth    = false;
            hlg.childControlHeight   = true;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;

            var lbl = MakeGO("Label", row.transform);
            lbl.AddComponent<LayoutElement>().preferredWidth = 76f;
            lbl.AddComponent<CanvasRenderer>();
            var lblTmp = lbl.AddComponent<TextMeshProUGUI>();
            SetupTMP(lblTmp, "FULLSCREEN", 14f, new Color(0.68f, 0.66f, 0.76f), TextAlignmentOptions.MidlineLeft);

            bool fs = PlayerPrefs.GetInt("fullscreen", Screen.fullScreen ? 1 : 0) == 1;

            var toggleGo = MakeGO("Toggle", row.transform);
            toggleGo.AddComponent<LayoutElement>().preferredWidth = 90f;
            toggleGo.AddComponent<CanvasRenderer>();
            var toggleImg = toggleGo.AddComponent<Image>();
            toggleImg.color = new Color(0.14f, 0.11f, 0.22f, 1f);
            var toggleBtn = toggleGo.AddComponent<Button>();
            toggleBtn.targetGraphic = toggleImg;

            var tlbl = MakeGO("Label", toggleGo.transform);
            var tlblRt = tlbl.GetComponent<RectTransform>();
            tlblRt.anchorMin = Vector2.zero; tlblRt.anchorMax = Vector2.one;
            tlblRt.offsetMin = Vector2.zero; tlblRt.offsetMax = Vector2.zero;
            tlbl.AddComponent<CanvasRenderer>();
            var tlblTmp = tlbl.AddComponent<TextMeshProUGUI>();
            SetupTMP(tlblTmp, fs ? "FULLSCREEN" : "WINDOWED", 14f, Color.white, TextAlignmentOptions.Center);

            toggleBtn.onClick.AddListener(() =>
            {
                fs = !fs;
                Screen.fullScreen = fs;
                PlayerPrefs.SetInt("fullscreen", fs ? 1 : 0);
                tlblTmp.text = fs ? "FULLSCREEN" : "WINDOWED";
            });
        }

        // ── Widget helpers ────────────────────────────────────────────────────

        /// Small [–] / [+] button used inside volume rows.
        GameObject MakeControlButton(string name, Transform parent, string label)
        {
            var go = MakeGO(name, parent);
            go.AddComponent<LayoutElement>().preferredWidth = 18f;
            go.AddComponent<CanvasRenderer>();
            var img = go.AddComponent<Image>();
            img.color = new Color(0.18f, 0.15f, 0.28f, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => AudioManager.Instance?.PlayButtonClick());

            var lgo = MakeGO("L", go.transform);
            var lrt = lgo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            lgo.AddComponent<CanvasRenderer>();
            var tmp = lgo.AddComponent<TextMeshProUGUI>();
            SetupTMP(tmp, label, 14f, Color.white, TextAlignmentOptions.Center);
            return go;
        }

        /// Simple labelled button (used for Close).
        GameObject MakeSmallButton(string name, Transform parent, string label, System.Action onClick)
        {
            var go = MakeGO(name, parent);
            go.AddComponent<CanvasRenderer>();
            var img = go.AddComponent<Image>();
            img.color = new Color(0.14f, 0.11f, 0.22f, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => { AudioManager.Instance?.PlayButtonClick(); onClick(); });

            var lgo = MakeGO("Label", go.transform);
            var lrt = lgo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            lgo.AddComponent<CanvasRenderer>();
            var tmp = lgo.AddComponent<TextMeshProUGUI>();
            SetupTMP(tmp, label, 14f, Color.white, TextAlignmentOptions.Center);
            return go;
        }

        void AddLabel(Transform parent, string text, float size, Color color,
                      TextAlignmentOptions align, float preferredH)
        {
            var go = MakeGO("Label_" + text, parent);
            go.AddComponent<LayoutElement>().preferredHeight = preferredH;
            go.AddComponent<CanvasRenderer>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            SetupTMP(tmp, text, size, color, align);
        }

        void SetupTMP(TextMeshProUGUI tmp, string text, float size, Color color,
                      TextAlignmentOptions align)
        {
            tmp.text               = text;
            tmp.font               = font;
            tmp.fontSize           = size;
            tmp.color              = color;
            tmp.alignment          = align;
            tmp.raycastTarget      = false;
            tmp.enableWordWrapping = false;
        }

        static GameObject MakeGO(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI layer
            go.transform.SetParent(parent, false);
            return go;
        }

        static string Pct(float v) => $"{Mathf.RoundToInt(v * 100)}%";
    }
}
