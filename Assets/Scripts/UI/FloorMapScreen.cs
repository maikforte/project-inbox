using System.Collections.Generic;
using InboxZero.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Gmail-style inbox chrome (top bar + sidebar + content area).
    /// Delegates email-list rendering to InboxScreen, deck builder to DeckBuilderScreen,
    /// and card compendium to AllMailScreen — all swapped into the ContentArea.
    public class FloorMapScreen : MonoBehaviour
    {
        public static FloorMapScreen Instance { get; private set; }

        [Header("Prefab")]
        public GameObject layoutPrefab;  // InboxLayout.prefab

        [Header("Font")]
        public TMP_FontAsset uiFont;

        public UnityEvent<RoomOption> OnCombatSelected   = new UnityEvent<RoomOption>();
        public UnityEvent<RoomOption> OnRestStopSelected = new UnityEvent<RoomOption>();
        public UnityEvent             OnFloorComplete    = new UnityEvent();
        public UnityEvent             OnDraftsSelected   = new UnityEvent();
        public UnityEvent             OnAllMailSelected  = new UnityEvent();

        Canvas           _canvas;
        GameObject       _panel;
        RectTransform    _listAreaRt;
        List<RoomOption> _lastOptions;

        // ── Gmail colour palette ──────────────────────────────────────────────
        static readonly Color C_Bg        = new Color(1.00f, 1.00f, 1.00f);
        static readonly Color C_TopBar    = new Color(0.97f, 0.97f, 0.97f);
        static readonly Color C_Sidebar   = new Color(0.96f, 0.97f, 0.98f);
        static readonly Color C_SbActive  = new Color(0.84f, 0.89f, 0.98f);
        static readonly Color C_Accent    = new Color(0.10f, 0.45f, 0.91f);
        static readonly Color C_TextMid   = new Color(0.37f, 0.39f, 0.41f);
        static readonly Color C_TextLight = new Color(0.62f, 0.64f, 0.67f);
        static readonly Color C_Sep       = new Color(0.88f, 0.88f, 0.88f);

        // ── Sidebar items ─────────────────────────────────────────────────────
        static readonly string[] SbLabels = { "INBOX", "STARRED", "SNOOZED", "IMPORTANT", "SENT", "DRAFTS", "ALL MAIL", "SPAM" };
        static readonly string[] SbCounts = { "22",    "",        "",         "",          "",     "",        "",         "5921" };

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _canvas  = FindObjectOfType<Canvas>();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Show(List<RoomOption> options)
        {
            if (_panel != null) Destroy(_panel);
            Build(options);
        }

        public void Hide()
        {
            if (_panel != null) Destroy(_panel);
            _panel = null;
        }

        // ── Layout construction ───────────────────────────────────────────────

        void Build(List<RoomOption> options)
        {
            if (_canvas == null) { Debug.LogError("[FloorMapScreen] No Canvas."); return; }

            if (layoutPrefab != null)
                BuildFromPrefab(options);
            else
                BuildProgrammatic(options);
        }

        void BuildFromPrefab(List<RoomOption> options)
        {
            _panel = Instantiate(layoutPrefab, _canvas.transform, false);
            var view = _panel.GetComponent<LayoutView>();
            if (view == null) { Debug.LogError("[FloorMapScreen] layoutPrefab missing LayoutView."); return; }

            // Wire nav buttons
            if (view.inboxButton   != null) view.inboxButton.onClick.AddListener(OnInboxClicked);
            if (view.draftsButton  != null) view.draftsButton.onClick.AddListener(OnDraftsClicked);
            if (view.allMailButton != null) view.allMailButton.onClick.AddListener(OnAllMailClicked);

            // Update dynamic labels
            if (view.floorInfoLabel != null)
            {
                var gm = GameManager.Instance;
                view.floorInfoLabel.text = $"FL {gm.CurrentFloor}  RM {gm.CurrentRoom + 1}";
            }
            if (view.draftsLabel != null)
            {
                int n = GameManager.Instance?.CardCollection.Count ?? 0;
                view.draftsLabel.text = n > 0 ? $"DRAFTS  {n}" : "DRAFTS";
            }

            _listAreaRt  = view.contentArea;
            _lastOptions = options;

            // Wire InboxScreen events → FloorMapScreen events
            var inbox = InboxScreen.Instance;
            if (inbox != null)
            {
                inbox.OnCombatSelected.RemoveAllListeners();
                inbox.OnRestStopSelected.RemoveAllListeners();
                inbox.OnCombatSelected.AddListener(OnInboxCombatSelected);
                inbox.OnRestStopSelected.AddListener(OnInboxRestStopSelected);
            }

            ShowInboxPage(options);
        }

        void BuildProgrammatic(List<RoomOption> options)
        {
            // Fallback: build entirely in code (no prefab assigned)
            _panel = Stretch("InboxPanel", _canvas.transform, 0, 0, 0, 0);
            _panel.layer = 5;
            _panel.AddComponent<Image>().color = C_Bg;
            var root = RT(_panel);

            BuildTopBar(root);

            var body = Stretch("Body", root, 0, 28, 0, 0);
            body.layer = 5;
            var bodyRt = RT(body);

            BuildSidebar(bodyRt);

            var listArea = Stretch("ListArea", bodyRt, 80, 0, 0, 0);
            listArea.layer = 5;
            _listAreaRt  = RT(listArea);
            _lastOptions = options;

            var inbox = InboxScreen.Instance;
            if (inbox != null)
            {
                inbox.OnCombatSelected.RemoveAllListeners();
                inbox.OnRestStopSelected.RemoveAllListeners();
                inbox.OnCombatSelected.AddListener(OnInboxCombatSelected);
                inbox.OnRestStopSelected.AddListener(OnInboxRestStopSelected);
            }

            ShowInboxPage(options);
        }

        // ── Top bar ───────────────────────────────────────────────────────────

        void BuildTopBar(RectTransform parent)
        {
            var bar = TopStrip("TopBar", parent, 28);
            bar.layer = 5;
            bar.AddComponent<Image>().color = C_TopBar;
            var barRt = RT(bar);

            var border = BottomLine("TopBorder", barRt);
            border.AddComponent<Image>().color = C_Sep;

            Label("Logo", barRt,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(10, 0), new Vector2(130, 14),
                "INBOX // ZERO", C_Accent, TextAlignmentOptions.MidlineLeft);

            var srBg = Fixed("SearchBg", barRt,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(200, 16));
            srBg.AddComponent<Image>().color = new Color(0.93f, 0.94f, 0.96f);
            Label("SearchTxt", RT(srBg),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                "Search mail", C_TextLight, TextAlignmentOptions.Center);

            var gm = GameManager.Instance;
            Label("FloorInfo", barRt,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-10, 0), new Vector2(100, 14),
                $"FL {gm.CurrentFloor}  RM {gm.CurrentRoom + 1}", C_TextMid,
                TextAlignmentOptions.MidlineRight);
        }

        // ── Sidebar ───────────────────────────────────────────────────────────

        void BuildSidebar(RectTransform parent)
        {
            var sb = Fixed("Sidebar", parent,
                new Vector2(0, 0), new Vector2(0, 1),
                Vector2.zero, new Vector2(80, 0));
            sb.AddComponent<Image>().color = C_Sidebar;
            var sbRt = RT(sb);

            var border = new GameObject("SbBorder", typeof(RectTransform));
            border.layer = 5;
            var bRt = border.GetComponent<RectTransform>();
            bRt.SetParent(sbRt, false);
            bRt.anchorMin = new Vector2(1, 0);
            bRt.anchorMax = new Vector2(1, 1);
            bRt.offsetMin = new Vector2(-1, 0);
            bRt.offsetMax = new Vector2(0, 0);
            border.AddComponent<Image>().color = C_Sep;

            const int InboxIndex   = 0;
            const int DraftsIndex  = 5;
            const int AllMailIndex = 6;

            for (int i = 0; i < SbLabels.Length; i++)
            {
                float y         = -8 - i * 22f;
                bool  active    = i == 0;
                bool  isInbox   = i == InboxIndex;
                bool  isDrafts  = i == DraftsIndex;
                bool  isAllMail = i == AllMailIndex;

                if (active)
                {
                    var hl = Fixed($"SbHl{i}", sbRt,
                        new Vector2(0, 1), new Vector2(1, 1),
                        new Vector2(0, y - 1), new Vector2(0, 20));
                    hl.AddComponent<Image>().color = C_SbActive;
                }

                string txt;
                if (isDrafts)
                {
                    int n = GameManager.Instance != null
                        ? GameManager.Instance.CardCollection.Count
                        : 0;
                    txt = n > 0 ? $"DRAFTS  {n}" : "DRAFTS";
                }
                else
                {
                    txt = SbLabels[i];
                    if (!string.IsNullOrEmpty(SbCounts[i])) txt += $"  {SbCounts[i]}";
                }

                Color labelColor = (isDrafts || isAllMail) ? C_Accent : (active ? C_Accent : C_TextMid);

                Label($"Sb{i}", sbRt,
                    new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1),
                    new Vector2(8, y), new Vector2(-8, 14),
                    txt, labelColor, TextAlignmentOptions.MidlineLeft);

                if (isInbox || isDrafts || isAllMail)
                {
                    string btnName = isInbox ? "SbInboxBtn" : isDrafts ? "SbDraftsBtn" : "SbAllMailBtn";
                    var hitGo = new GameObject(btnName, typeof(RectTransform));
                    hitGo.layer = 5;
                    var hitRt   = hitGo.GetComponent<RectTransform>();
                    hitRt.SetParent(sbRt, false);
                    hitRt.anchorMin        = new Vector2(0, 1);
                    hitRt.anchorMax        = new Vector2(1, 1);
                    hitRt.pivot            = new Vector2(0, 1);
                    hitRt.anchoredPosition = new Vector2(0, y - 1);
                    hitRt.sizeDelta        = new Vector2(0, 20);

                    var hitImg           = hitGo.AddComponent<Image>();
                    hitImg.color         = new Color(1f, 1f, 1f, 0.01f);
                    hitImg.raycastTarget = true;

                    var btn = hitGo.AddComponent<Button>();
                    var cb  = btn.colors;
                    cb.normalColor      = Color.white;
                    cb.highlightedColor = C_SbActive;
                    cb.pressedColor     = new Color(C_SbActive.r * 0.9f, C_SbActive.g * 0.9f, C_SbActive.b * 0.9f);
                    btn.colors          = cb;
                    UnityEngine.Events.UnityAction handler = isInbox ? OnInboxClicked : isDrafts ? (UnityEngine.Events.UnityAction)OnDraftsClicked : OnAllMailClicked;
                    btn.onClick.AddListener(handler);
                }
            }
        }

        // ── Page switching ────────────────────────────────────────────────────

        void ShowInboxPage(List<RoomOption> options)
        {
            foreach (Transform child in _listAreaRt)
                Destroy(child.gameObject);

            InboxScreen.Instance?.ShowInContent(_listAreaRt, options);
        }

        void OnInboxClicked()
        {
            DeckBuilderScreen.Instance?.OnComplete.RemoveListener(OnDraftsDone);
            AllMailScreen.Instance?.OnClose.RemoveListener(RestoreInbox);
            if (_lastOptions != null) ShowInboxPage(_lastOptions);
        }

        void OnDraftsClicked()
        {
            var db = DeckBuilderScreen.Instance;
            if (db == null || _listAreaRt == null) return;

            AllMailScreen.Instance?.OnClose.RemoveListener(RestoreInbox);
            InboxScreen.Instance?.Hide();

            foreach (Transform child in _listAreaRt)
                Destroy(child.gameObject);

            db.OnComplete.AddListener(OnDraftsDone);
            db.ShowInContent(_listAreaRt);
        }

        void OnAllMailClicked()
        {
            if (_listAreaRt == null || AllMailScreen.Instance == null) return;

            InboxScreen.Instance?.Hide();

            foreach (Transform child in _listAreaRt)
                Destroy(child.gameObject);

            AllMailScreen.Instance.OnClose.AddListener(RestoreInbox);
            AllMailScreen.Instance.ShowInContent(_listAreaRt);
        }

        void OnDraftsDone()
        {
            DeckBuilderScreen.Instance?.OnComplete.RemoveListener(OnDraftsDone);
            if (_listAreaRt == null || _lastOptions == null) return;
            ShowInboxPage(_lastOptions);
        }

        void RestoreInbox()
        {
            AllMailScreen.Instance?.OnClose.RemoveListener(RestoreInbox);
            if (_listAreaRt == null || _lastOptions == null) return;
            ShowInboxPage(_lastOptions);
        }

        // ── InboxScreen event forwarding ──────────────────────────────────────

        void OnInboxCombatSelected(RoomOption opt)
        {
            Hide();
            OnCombatSelected.Invoke(opt);
        }

        void OnInboxRestStopSelected(RoomOption opt)
        {
            Hide();
            OnRestStopSelected.Invoke(opt);
        }

        // ── Layout helpers ────────────────────────────────────────────────────

        static GameObject Stretch(string name, Transform parent,
            float left, float top, float right, float bottom)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return go;
        }

        static GameObject TopStrip(string name, RectTransform parent, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin        = new Vector2(0, 1);
            rt.anchorMax        = new Vector2(1, 1);
            rt.pivot            = new Vector2(0, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(0, height);
            return go;
        }

        static GameObject Fixed(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = anchorMin;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;
            return go;
        }

        static GameObject BottomLine(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(0, 1);
            return go;
        }

        GameObject Label(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size, string text,
            Color color, TextAlignmentOptions align)
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
            tmp.text               = text;
            tmp.fontSize           = 14;
            tmp.color              = color;
            tmp.alignment          = align;
            tmp.enableWordWrapping = false;
            tmp.overflowMode       = TextOverflowModes.Overflow;
            if (uiFont != null) tmp.font = uiFont;
            return go;
        }

        static RectTransform RT(GameObject go) => go.GetComponent<RectTransform>();
    }
}
