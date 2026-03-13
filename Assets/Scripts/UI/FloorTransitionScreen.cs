using System.Collections.Generic;
using InboxZero.Core;
using InboxZero.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace InboxZero.UI
{
    public class FloorTransitionScreen : MonoBehaviour
    {
        public static FloorTransitionScreen Instance { get; private set; }

        [Header("Relic Pool")]
        [Tooltip("All acquirable relics. One random unowned relic is awarded on floors 2 and 3.")]
        public List<RelicData> relicPool = new List<RelicData>();

        [Header("Font")]
        public TMP_FontAsset uiFont;

        // Fires after the player continues past the floor — resets combat for next floor.
        public UnityEvent OnNextFloorReady = new UnityEvent();
        // Fires when Floor 4 is cleared — game is won.
        public UnityEvent OnGameVictory = new UnityEvent();

        static readonly string[] FloorNames =
        {
            "", "THE INBOX", "THE THREADS", "THE ESCALATIONS", "THE FINAL THREAD"
        };

        Canvas _canvas;
        GameObject _panel;
        RelicData  _pendingRelic;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _canvas = FindObjectOfType<Canvas>();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// Called by FloorMapScreen.OnFloorComplete.
        public void Show()
        {
            int clearedFloor = GameManager.Instance.CurrentFloor;

            if (clearedFloor >= 4)
            {
                OnGameVictory.Invoke();
                return;
            }

            _pendingRelic = clearedFloor >= 2 ? PickRelic() : null;
            BuildPanel(clearedFloor);
        }

        public void Hide()
        {
            if (_panel != null) Destroy(_panel);
            _panel = null;
            _pendingRelic = null;
        }

        // ── Relic selection ───────────────────────────────────────────────────

        RelicData PickRelic()
        {
            var owned = new HashSet<RelicData>(GameManager.Instance.ActiveRelics);
            var available = new List<RelicData>();
            foreach (var r in relicPool)
                if (!owned.Contains(r)) available.Add(r);

            if (available.Count == 0) return null;
            return available[Random.Range(0, available.Count)];
        }

        // ── Panel construction ────────────────────────────────────────────────

        void BuildPanel(int clearedFloor)
        {
            var canvas = _canvas;
            if (canvas == null) { Debug.LogError("[FloorTransitionScreen] No Canvas found."); return; }

            _panel = new GameObject("FloorTransitionPanel", typeof(RectTransform));
            _panel.layer = 5;
            var rt = _panel.GetComponent<RectTransform>();
            rt.SetParent(canvas.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _panel.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.10f, 0.94f);

            string floorName = clearedFloor < FloorNames.Length ? FloorNames[clearedFloor] : "";
            MakeLabel("ClearedTitle", rt,
                Center, Center, Center,
                new Vector2(0, 70), new Vector2(320, 18),
                $"FLOOR {clearedFloor} CLEARED", 14, TextAlignmentOptions.Center);

            MakeLabel("FloorName", rt,
                Center, Center, Center,
                new Vector2(0, 46), new Vector2(320, 14),
                $"// {floorName} //", 14, TextAlignmentOptions.Center);

            string nextFloorName = (clearedFloor + 1) < FloorNames.Length
                ? FloorNames[clearedFloor + 1] : "";
            MakeLabel("NextFloor", rt,
                Center, Center, Center,
                new Vector2(0, 24), new Vector2(320, 12),
                $"ENTERING FLOOR {clearedFloor + 1}: {nextFloorName}", 14,
                TextAlignmentOptions.Center);

            if (_pendingRelic != null)
                BuildRelicBlock(rt);
            else
                BuildContinueButton(rt, new Vector2(0, -30));
        }

        void BuildRelicBlock(RectTransform parent)
        {
            // Relic card panel
            var relicGo = new GameObject("RelicCard", typeof(RectTransform));
            relicGo.layer = 5;
            var relicRt = relicGo.GetComponent<RectTransform>();
            relicRt.SetParent(parent, false);
            relicRt.anchorMin        = Center;
            relicRt.anchorMax        = Center;
            relicRt.sizeDelta        = new Vector2(220, 70);
            relicRt.anchoredPosition = new Vector2(0, -28);
            relicGo.AddComponent<Image>().color = new Color(0.20f, 0.16f, 0.08f);

            MakeLabel("RelicHeader", relicRt,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -6), new Vector2(200, 12),
                "RELIC ACQUIRED", 14, TextAlignmentOptions.Center);

            MakeLabel("RelicName", relicRt,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -20), new Vector2(200, 14),
                _pendingRelic.relicName.ToUpper(), 14, TextAlignmentOptions.Center);

            var effectGo = MakeLabel("RelicEffect", relicRt,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 8), new Vector2(200, 22),
                _pendingRelic.effectDescription, 14, TextAlignmentOptions.Center);
            effectGo.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;

            BuildContinueButton(parent, new Vector2(0, -110));
        }

        void BuildContinueButton(RectTransform parent, Vector2 pos)
        {
            var go = new GameObject("ContinueButton", typeof(RectTransform));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin        = Center;
            rt.anchorMax        = Center;
            rt.sizeDelta        = new Vector2(120, 24);
            rt.anchoredPosition = pos;
            go.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.30f);
            go.AddComponent<Button>().onClick.AddListener(OnContinue);

            var lbl = MakeLabel("Label", rt,
                Vector2.zero, Vector2.one, Center,
                Vector2.zero, Vector2.zero, "CONTINUE", 14, TextAlignmentOptions.Center);
            lbl.GetComponent<TextMeshProUGUI>().enableWordWrapping = false;
        }

        // ── Handler ───────────────────────────────────────────────────────────

        void OnContinue()
        {
            // Award relic before advancing
            if (_pendingRelic != null)
            {
                GameManager.Instance.ActiveRelics.Add(_pendingRelic);
                if (RelicManager.Instance != null)
                    RelicManager.Instance.OnRelicAcquired(_pendingRelic);
            }

            // Advance to next floor, reset room counter
            GameManager.Instance.CurrentFloor++;
            GameManager.Instance.CurrentRoom = 1;

            // Reset combat-ended flag for new floor
            if (TurnManager.Instance != null)
                TurnManager.Instance.IsCombatEnded = false;

            Hide();
            OnNextFloorReady.Invoke();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

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
