using System.Collections.Generic;
using InboxZero.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace InboxZero.UI
{
    public class FloorMapScreen : MonoBehaviour
    {
        public static FloorMapScreen Instance { get; private set; }

        [Header("Font")]
        public TMP_FontAsset uiFont;

        // Fires when player picks a Combat room. Caller starts a new combat encounter.
        public UnityEvent OnCombatSelected  = new UnityEvent();
        // Fires when player picks a Rest Stop.
        public UnityEvent OnRestStopSelected = new UnityEvent();
        // Fires after Room 4 is selected — TASK-13 (floor transition) listens here.
        public UnityEvent OnFloorComplete   = new UnityEvent();

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
            bool floorDone = FloorMapManager.Instance.AdvanceRoom();

            if (floorDone)
            {
                OnFloorComplete.Invoke();
                return;
            }

            var options = FloorMapManager.Instance.GenerateNextOptions();
            BuildPanel(options);
        }

        public void Hide()
        {
            if (_panel != null) Destroy(_panel);
            _panel = null;
        }

        // ── Panel construction ────────────────────────────────────────────────

        void BuildPanel(List<RoomOption> options)
        {
            var canvas = _canvas;
            if (canvas == null) { Debug.LogError("[FloorMapScreen] No Canvas found."); return; }

            _panel = new GameObject("FloorMapPanel", typeof(RectTransform));
            _panel.layer = 5;
            var panelRt = _panel.GetComponent<RectTransform>();
            panelRt.SetParent(canvas.transform, false);
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            _panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);

            // Floor / Room header
            var gm = GameManager.Instance;
            MakeLabel("Header", panelRt,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -24), new Vector2(300, 16),
                $"FLOOR {gm.CurrentFloor}  //  ROOM {gm.CurrentRoom}", 14);

            MakeLabel("SubHeader", panelRt,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -44), new Vector2(300, 14),
                "CHOOSE YOUR NEXT ENCOUNTER", 14);

            // Option buttons
            float totalWidth  = options.Count * 120f + (options.Count - 1) * 24f;
            float startX      = -totalWidth / 2f + 60f;

            for (int i = 0; i < options.Count; i++)
            {
                var opt = options[i];
                float xPos = startX + i * 144f;
                BuildOptionButton(panelRt, opt, xPos);
            }
        }

        void BuildOptionButton(RectTransform parent, RoomOption option, float xPos)
        {
            var go = new GameObject(option.label, typeof(RectTransform));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(120f, 80f);
            rt.anchoredPosition = new Vector2(xPos, 0);

            var bg = go.AddComponent<Image>();
            bg.color = option.type == RoomType.Combat
                ? new Color(0.45f, 0.10f, 0.10f)
                : new Color(0.10f, 0.35f, 0.15f);

            // Icon row (placeholder text symbol)
            string icon = option.type == RoomType.Combat ? "[!]" : "[+]";
            var iconGo = MakeLabel("Icon", rt,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -10), new Vector2(80, 24), icon, 14);
            iconGo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            // Label
            var labelGo = MakeLabel("Label", rt,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 10), new Vector2(110, 14), option.label, 14);
            labelGo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            var btn = go.AddComponent<Button>();
            var captured = option;
            btn.onClick.AddListener(() => OnOptionPicked(captured));
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        void OnOptionPicked(RoomOption option)
        {
            Hide();
            if (option.type == RoomType.Combat)
                OnCombatSelected.Invoke();
            else
                OnRestStopSelected.Invoke();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        GameObject MakeLabel(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size, string text, int fontSize)
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
            tmp.text     = text;
            tmp.fontSize = fontSize;
            tmp.color    = Color.white;
            if (uiFont != null) tmp.font = uiFont;

            return go;
        }
    }
}
