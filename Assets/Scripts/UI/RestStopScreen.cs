using InboxZero.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace InboxZero.UI
{
    public class RestStopScreen : MonoBehaviour
    {
        public static RestStopScreen Instance { get; private set; }

        [Header("Settings")]
        public int healAmount = 15;

        [Header("Flavour texts — picked at random")]
        public string[] flavourLines = new[]
        {
            "You close a few tabs. Not all of them. But some.",
            "The break room is empty. You eat someone else's yoghurt.",
            "You set your status to Away. You are not away.",
            "Out of office: mentally. Back: never.",
            "You stare at the ceiling for exactly four minutes.",
        };

        [Header("Font")]
        public TMP_FontAsset uiFont;

        // Fires when the player clicks Continue — wire to FloorMapScreen.Show().
        public UnityEvent OnComplete = new UnityEvent();

        Canvas _canvas;
        GameObject _panel;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _canvas = FindObjectOfType<Canvas>();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Show()
        {
            var gm = GameManager.Instance;
            int before = gm.CurrentHP;
            gm.CurrentHP = Mathf.Min(gm.CurrentHP + healAmount, gm.MaxHP);
            int healed = gm.CurrentHP - before;

            BuildPanel(healed);
        }

        public void Hide()
        {
            if (_panel != null) Destroy(_panel);
            _panel = null;
        }

        // ── Panel construction ────────────────────────────────────────────────

        void BuildPanel(int healed)
        {
            var canvas = _canvas;
            if (canvas == null) { Debug.LogError("[RestStopScreen] No Canvas found."); return; }

            _panel = new GameObject("RestStopPanel", typeof(RectTransform));
            _panel.layer = 5;
            var rt = _panel.GetComponent<RectTransform>();
            rt.SetParent(canvas.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _panel.AddComponent<Image>().color = new Color(0.05f, 0.12f, 0.08f, 0.92f);

            // Header
            MakeLabel("Header", rt,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 60), new Vector2(320, 18),
                "// REST STOP //", 14, TextAlignmentOptions.Center);

            // Flavour text
            string line = flavourLines.Length > 0
                ? flavourLines[Random.Range(0, flavourLines.Length)]
                : "";
            var flavourGo = MakeLabel("Flavour", rt,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 20), new Vector2(300, 40), line, 14, TextAlignmentOptions.Center);
            flavourGo.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;

            // Heal notice
            string healMsg = healed > 0
                ? $"RESTORED {healed} HP  ({GameManager.Instance.CurrentHP} / {GameManager.Instance.MaxHP})"
                : $"HP FULL  ({GameManager.Instance.MaxHP} / {GameManager.Instance.MaxHP})";
            MakeLabel("HealNotice", rt,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -16), new Vector2(260, 14),
                healMsg, 14, TextAlignmentOptions.Center);

            // Continue button
            var btnGo = new GameObject("ContinueButton", typeof(RectTransform));
            btnGo.layer = 5;
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.SetParent(rt, false);
            btnRt.anchorMin        = new Vector2(0.5f, 0.5f);
            btnRt.anchorMax        = new Vector2(0.5f, 0.5f);
            btnRt.sizeDelta        = new Vector2(100, 22);
            btnRt.anchoredPosition = new Vector2(0, -50);
            btnGo.AddComponent<Image>().color = new Color(0.12f, 0.35f, 0.18f);
            btnGo.AddComponent<Button>().onClick.AddListener(OnContinue);

            var lblGo = MakeLabel("Label", btnRt,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, "CONTINUE", 14, TextAlignmentOptions.Center);
            lblGo.GetComponent<TextMeshProUGUI>().enableWordWrapping = false;
        }

        void OnContinue()
        {
            Hide();
            OnComplete.Invoke();
        }

        // ── Helper ────────────────────────────────────────────────────────────

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
            if (uiFont != null) tmp.font = uiFont;

            return go;
        }
    }
}
