using System.Collections.Generic;
using InboxZero.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Gmail-style inbox list that replaces the old button-grid room selector.
    /// Public API is identical to the original FloorMapScreen — CombatSetup
    /// requires no changes.
    public class FloorMapScreen : MonoBehaviour
    {
        public static FloorMapScreen Instance { get; private set; }

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
        static readonly Color C_Bg         = new Color(1.00f, 1.00f, 1.00f);          // white
        static readonly Color C_TopBar     = new Color(0.97f, 0.97f, 0.97f);          // #F8F9FA
        static readonly Color C_Sidebar    = new Color(0.96f, 0.97f, 0.98f);          // #F6F8FC
        static readonly Color C_SbActive   = new Color(0.84f, 0.89f, 0.98f);          // inbox highlight
        static readonly Color C_Accent     = new Color(0.10f, 0.45f, 0.91f);          // #1A73E8 blue
        static readonly Color C_TextDark   = new Color(0.13f, 0.13f, 0.14f);          // #202124
        static readonly Color C_TextMid    = new Color(0.37f, 0.39f, 0.41f);          // #5F6368
        static readonly Color C_TextLight  = new Color(0.62f, 0.64f, 0.67f);
        static readonly Color C_Sep        = new Color(0.88f, 0.88f, 0.88f);
        static readonly Color C_RowHover   = new Color(0.93f, 0.95f, 0.99f);
        static readonly Color C_RowPressed = new Color(0.86f, 0.91f, 0.98f);

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

            // Root — full-screen white panel
            _panel = Stretch("InboxPanel", _canvas.transform, 0, 0, 0, 0);
            _panel.layer = 5;
            _panel.AddComponent<Image>().color = C_Bg;
            var root = RT(_panel);

            // Top chrome bar (28 px tall)
            BuildTopBar(root);

            // Body below chrome (fills remaining height)
            var body = Stretch("Body", root, 0, 28, 0, 0);
            body.layer = 5;
            var bodyRt = RT(body);

            // Sidebar (80 px wide, full height)
            BuildSidebar(bodyRt);

            // Email list (fills right of sidebar)
            var listArea = Stretch("ListArea", bodyRt, 80, 0, 0, 0);
            listArea.layer = 5;
            _listAreaRt  = RT(listArea);
            _lastOptions = options;
            BuildEmailList(_listAreaRt, options);
        }

        // ── Top bar ───────────────────────────────────────────────────────────

        void BuildTopBar(RectTransform parent)
        {
            var bar = TopStrip("TopBar", parent, 28);
            bar.layer = 5;
            bar.AddComponent<Image>().color = C_TopBar;
            var barRt = RT(bar);

            // Bottom border
            var border = BottomLine("TopBorder", barRt);
            border.AddComponent<Image>().color = C_Sep;

            // Logo — left
            Label("Logo", barRt,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(10, 0), new Vector2(130, 14),
                "INBOX // ZERO", C_Accent, TextAlignmentOptions.MidlineLeft);

            // Search bar — decorative centre
            var srBg = Fixed("SearchBg", barRt,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(200, 16));
            srBg.AddComponent<Image>().color = new Color(0.93f, 0.94f, 0.96f);
            Label("SearchTxt", RT(srBg),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                "Search mail", C_TextLight, TextAlignmentOptions.Center);

            // Floor / room info — right
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

            // Right border
            var border = new GameObject("SbBorder", typeof(RectTransform));
            border.layer = 5;
            var bRt = border.GetComponent<RectTransform>();
            bRt.SetParent(sbRt, false);
            bRt.anchorMin = new Vector2(1, 0);
            bRt.anchorMax = new Vector2(1, 1);
            bRt.offsetMin = new Vector2(-1, 0);
            bRt.offsetMax = new Vector2(0, 0);
            border.AddComponent<Image>().color = C_Sep;

            const int DraftsIndex  = 5;
            const int AllMailIndex = 6;

            for (int i = 0; i < SbLabels.Length; i++)
            {
                float y         = -8 - i * 22f;
                bool  active    = i == 0;
                bool  isDrafts  = i == DraftsIndex;
                bool  isAllMail = i == AllMailIndex;

                if (active)
                {
                    var hl = Fixed($"SbHl{i}", sbRt,
                        new Vector2(0, 1), new Vector2(1, 1),
                        new Vector2(0, y - 1), new Vector2(0, 20));
                    hl.AddComponent<Image>().color = C_SbActive;
                }

                // DRAFTS: show live CardCollection count; other items use static counts
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

                var lbl = Label($"Sb{i}", sbRt,
                    new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1),
                    new Vector2(8, y), new Vector2(-8, 14),
                    txt, labelColor,
                    TextAlignmentOptions.MidlineLeft);

                // Make DRAFTS and ALL MAIL clickable buttons
                if (isDrafts || isAllMail)
                {
                    // Full-width hit area over the label row.
                    // Uses a near-invisible Image (0.01 alpha) — Color.clear can
                    // silently fail to block raycasts in some Unity 6 configurations.
                    string btnName = isDrafts ? "SbDraftsBtn" : "SbAllMailBtn";
                    var hitGo = new GameObject(btnName, typeof(RectTransform));
                    hitGo.layer = 5;
                    var hitRt   = hitGo.GetComponent<RectTransform>();
                    hitRt.SetParent(sbRt, false);
                    hitRt.anchorMin        = new Vector2(0, 1);
                    hitRt.anchorMax        = new Vector2(1, 1);
                    hitRt.pivot            = new Vector2(0, 1);
                    hitRt.anchoredPosition = new Vector2(0, y - 1);
                    hitRt.sizeDelta        = new Vector2(0, 20);

                    var hitImg            = hitGo.AddComponent<Image>();
                    hitImg.color          = new Color(1f, 1f, 1f, 0.01f);
                    hitImg.raycastTarget  = true;

                    var btn = hitGo.AddComponent<Button>();
                    var cb  = btn.colors;
                    cb.normalColor      = Color.white;
                    cb.highlightedColor = C_SbActive;
                    cb.pressedColor     = new Color(C_SbActive.r * 0.9f, C_SbActive.g * 0.9f, C_SbActive.b * 0.9f);
                    btn.colors          = cb;
                    btn.onClick.AddListener(isDrafts ? (UnityEngine.Events.UnityAction)OnDraftsClicked : OnAllMailClicked);
                }
            }
        }

        // ── Email list ────────────────────────────────────────────────────────

        void BuildEmailList(RectTransform parent, List<RoomOption> options)
        {
            // Tab bar (20 px)
            BuildTabBar(parent);

            // Separator under tabs
            var tabSep = TopStrip("TabSep", parent, 1);
            tabSep.layer = 5;
            tabSep.AddComponent<Image>().color = C_Sep;
            var tabSepRt = RT(tabSep);
            tabSepRt.anchoredPosition = new Vector2(0, -20);

            // Email rows (stacked below tabs + separator)
            float rowY = -21f;
            for (int i = 0; i < options.Count; i++)
            {
                BuildEmailRow(parent, options[i], rowY);
                rowY -= 32f;
                // Row separator
                var rs = TopStrip($"RowSep{i}", parent, 1);
                rs.layer = 5;
                rs.AddComponent<Image>().color = C_Sep;
                RT(rs).anchoredPosition = new Vector2(0, rowY);
            }
        }

        void BuildTabBar(RectTransform parent)
        {
            var bar = TopStrip("TabBar", parent, 20);
            bar.layer = 5;
            var barRt = RT(bar);

            string[] tabs   = { "PRIMARY", "PROMOTIONS", "SOCIAL", "FORUMS" };
            float    xOff   = 10f;
            float[]  widths = { 70f, 90f, 68f, 64f };

            for (int i = 0; i < tabs.Length; i++)
            {
                bool   active  = i == 0;
                var    lbl     = Label($"Tab{i}", barRt,
                    new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(xOff, -3), new Vector2(widths[i], 14),
                    tabs[i], active ? C_Accent : C_TextMid,
                    TextAlignmentOptions.MidlineLeft);

                if (active)
                {
                    // Blue underline
                    var ul = Fixed("Underline", RT(lbl),
                        new Vector2(0, 0), new Vector2(1, 0),
                        new Vector2(0, -1), new Vector2(0, 2));
                    ul.AddComponent<Image>().color = C_Accent;
                }
                xOff += widths[i] + 6f;
            }
        }

        void BuildEmailRow(RectTransform parent, RoomOption opt, float yOffset)
        {
            bool isUnread = opt.type == RoomType.Combat;

            // Row background (button)
            var row = TopStrip($"Row_{opt.type}_{yOffset}", parent, 32);
            row.layer = 5;
            RT(row).anchoredPosition = new Vector2(0, yOffset);
            var rowImg = row.AddComponent<Image>();
            rowImg.color = Color.white;
            var rowRt = RT(row);

            // Unread blue dot
            if (isUnread)
            {
                var dot = Fixed("Dot", rowRt,
                    new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                    new Vector2(7, 0), new Vector2(5, 5));
                dot.AddComponent<Image>().color = C_Accent;
            }

            // Sender name (fixed 110 px column)
            Color senderCol = isUnread ? C_TextDark : C_TextMid;
            var senderLbl = Label("Sender", rowRt,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(17, 0), new Vector2(110, 14),
                opt.senderName, senderCol, TextAlignmentOptions.MidlineLeft);
            senderLbl.GetComponent<TextMeshProUGUI>().overflowMode = TextOverflowModes.Ellipsis;

            // Subject + preview (rich text, horizontal stretch)
            string subjectHex = ColorUtility.ToHtmlStringRGB(senderCol);
            string previewHex = ColorUtility.ToHtmlStringRGB(C_TextMid);
            string bodyText   = $"<color=#{subjectHex}>{opt.subjectLine}</color>" +
                                $"<color=#{previewHex}>  -  {opt.previewText}</color>";
            var subjectGo  = new GameObject("Body", typeof(RectTransform));
            subjectGo.layer = 5;
            var subjectRt  = subjectGo.GetComponent<RectTransform>();
            subjectRt.SetParent(rowRt, false);
            subjectRt.anchorMin = Vector2.zero;
            subjectRt.anchorMax = Vector2.one;
            subjectRt.offsetMin = new Vector2(134, 5);
            subjectRt.offsetMax = new Vector2(-62, -5);
            var subjectTmp = subjectGo.AddComponent<TextMeshProUGUI>();
            subjectTmp.text               = bodyText;
            subjectTmp.fontSize           = 14;
            subjectTmp.color              = C_TextMid;
            subjectTmp.alignment          = TextAlignmentOptions.MidlineLeft;
            subjectTmp.enableWordWrapping = false;
            subjectTmp.overflowMode       = TextOverflowModes.Ellipsis;
            if (uiFont != null) subjectTmp.font = uiFont;

            // Date (right-aligned, 50 px)
            var gm       = GameManager.Instance;
            int day      = 14 - (4 - gm.CurrentFloor) * 2 - gm.CurrentRoom;
            string date  = $"Mar {day}";
            Label("Date", rowRt,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-8, 0), new Vector2(50, 14),
                date, isUnread ? C_TextDark : C_TextLight,
                TextAlignmentOptions.MidlineRight);

            // Clickable button
            var btn    = row.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor    = Color.white;
            colors.highlightedColor = C_RowHover;
            colors.pressedColor   = C_RowPressed;
            colors.selectedColor  = Color.white;
            btn.colors = colors;

            var captured = opt;
            btn.onClick.AddListener(() => OnRowClicked(captured));
        }

        void OnRowClicked(RoomOption opt)
        {
            Hide();
            if (opt.type == RoomType.Combat)
                OnCombatSelected.Invoke(opt);
            else
                OnRestStopSelected.Invoke(opt);
        }

        void OnDraftsClicked()
        {
            Hide();
            OnDraftsSelected.Invoke();
        }

        void OnAllMailClicked()
        {
            if (_listAreaRt == null || AllMailScreen.Instance == null) return;

            // Clear the email list, keep the top bar and sidebar
            foreach (Transform child in _listAreaRt)
                Destroy(child.gameObject);

            AllMailScreen.Instance.OnClose.AddListener(RestoreEmailList);
            AllMailScreen.Instance.ShowInContent(_listAreaRt);
        }

        void RestoreEmailList()
        {
            AllMailScreen.Instance.OnClose.RemoveListener(RestoreEmailList);
            if (_listAreaRt == null || _lastOptions == null) return;

            foreach (Transform child in _listAreaRt)
                Destroy(child.gameObject);

            BuildEmailList(_listAreaRt, _lastOptions);
        }

        // ── Layout helpers ────────────────────────────────────────────────────

        /// Full-stretch rect inset by pixel offsets from each edge.
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

        /// Full-width strip anchored to the top of parent, with a fixed pixel height.
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

        /// Fixed-size rect anchored at a specific point.
        static GameObject Fixed(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = anchorMin;      // pivot matches anchorMin for simplicity
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;
            return go;
        }

        /// 1-px horizontal line at the bottom of parent.
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

        /// TMP label with explicit anchor/pivot/position/size.
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
