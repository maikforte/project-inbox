using InboxZero.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.Editor
{
    /// Builds (or rebuilds) the InboxLayout prefab from scratch.
    /// Menu: InboxZero → Rebuild Inbox Layout Prefab
    ///
    /// Run once after initial setup. Re-run only to reset to default layout.
    public static class InboxLayoutBuilder
    {
        const string PrefabFolder      = "Assets/Prefabs/UI";
        const string LayoutPrefabPath  = "Assets/Prefabs/UI/InboxLayout.prefab";
        const string Micro5Guid        = "968711493fda13b4789c59de06321a30";

        // ── Colour palette (matches FloorMapScreen) ───────────────────────────
        static readonly Color C_Bg        = new Color(1.00f, 1.00f, 1.00f);
        static readonly Color C_TopBar    = new Color(0.97f, 0.97f, 0.97f);
        static readonly Color C_Sidebar   = new Color(0.96f, 0.97f, 0.98f);
        static readonly Color C_SbActive  = new Color(0.84f, 0.89f, 0.98f);
        static readonly Color C_Accent    = new Color(0.10f, 0.45f, 0.91f);
        static readonly Color C_TextDark  = new Color(0.13f, 0.13f, 0.14f);
        static readonly Color C_TextMid   = new Color(0.37f, 0.39f, 0.41f);
        static readonly Color C_TextLight = new Color(0.62f, 0.64f, 0.67f);
        static readonly Color C_Sep       = new Color(0.88f, 0.88f, 0.88f);
        static readonly Color C_SrBg      = new Color(0.93f, 0.94f, 0.96f);

        // ── Layout constants ──────────────────────────────────────────────────
        const float TopBarH    = 28f;
        const float SidebarW   = 80f;
        const float NavItemH   = 20f;
        const float NavItemGap = 22f;   // stride between nav items
        const float NavTopPad  = 8f;    // padding from top of sidebar

        // ── Sidebar labels ────────────────────────────────────────────────────
        static readonly string[] NavLabels = { "INBOX  22", "STARRED", "SNOOZED", "IMPORTANT", "SENT", "DRAFTS", "ALL MAIL", "SPAM  5921" };
        const int InboxIndex   = 0;
        const int DraftsIndex  = 5;
        const int AllMailIndex = 6;

        [MenuItem("InboxZero/Rebuild Inbox Layout Prefab")]
        static void Build()
        {
            EnsureFolders();
            var font = LoadFont();
            BuildLayoutPrefab(font);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AssignToScene();
            Debug.Log("[InboxLayoutBuilder] Done. Prefab saved to " + LayoutPrefabPath);
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        }

        static TMP_FontAsset LoadFont()
        {
            var path = AssetDatabase.GUIDToAssetPath(Micro5Guid);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("[InboxLayoutBuilder] Micro5-Regular not found by GUID — text will use TMP default.");
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }

        static void BuildLayoutPrefab(TMP_FontAsset font)
        {
            var root   = new GameObject("InboxLayout");
            root.layer = 5;
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            root.AddComponent<Image>().color = C_Bg;

            var view = root.AddComponent<LayoutView>();

            // ── Top bar ───────────────────────────────────────────────────────
            BuildTopBar(rootRt, font, view);

            // ── Body (fills below top bar) ────────────────────────────────────
            var bodyGo = new GameObject("Body");
            bodyGo.layer = 5;
            var bodyRt = bodyGo.AddComponent<RectTransform>();
            bodyRt.SetParent(rootRt, false);
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = Vector2.zero;
            bodyRt.offsetMax = new Vector2(0, -TopBarH);

            // ── Sidebar ───────────────────────────────────────────────────────
            BuildSidebar(bodyRt, font, view);

            // ── Content area (fills right of sidebar) ─────────────────────────
            var contentGo = new GameObject("ContentArea");
            contentGo.layer = 5;
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.SetParent(bodyRt, false);
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.offsetMin = new Vector2(SidebarW, 0);
            contentRt.offsetMax = Vector2.zero;
            view.contentArea = contentRt;

            PrefabUtility.SaveAsPrefabAsset(root, LayoutPrefabPath);
            Object.DestroyImmediate(root);
        }

        // ── Top bar ───────────────────────────────────────────────────────────

        static void BuildTopBar(RectTransform parent, TMP_FontAsset font, LayoutView view)
        {
            var barGo = new GameObject("TopBar");
            barGo.layer = 5;
            var barRt = barGo.AddComponent<RectTransform>();
            barRt.SetParent(parent, false);
            barRt.anchorMin        = new Vector2(0, 1);
            barRt.anchorMax        = new Vector2(1, 1);
            barRt.pivot            = new Vector2(0.5f, 1);
            barRt.anchoredPosition = Vector2.zero;
            barRt.sizeDelta        = new Vector2(0, TopBarH);
            barGo.AddComponent<Image>().color = C_TopBar;

            // Bottom border
            var borderGo = new GameObject("BottomBorder");
            borderGo.layer = 5;
            var borderRt = borderGo.AddComponent<RectTransform>();
            borderRt.SetParent(barRt, false);
            borderRt.anchorMin = new Vector2(0, 0);
            borderRt.anchorMax = new Vector2(1, 0);
            borderRt.offsetMin = new Vector2(0, 0);
            borderRt.offsetMax = new Vector2(0, 1);
            borderGo.AddComponent<Image>().color = C_Sep;

            // Logo
            MakeText("Logo", barRt, font,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(10, 0), new Vector2(130, 14),
                "INBOX // ZERO", 14, TextAlignmentOptions.MidlineLeft, C_Accent);

            // Search bar background
            var srGo = new GameObject("SearchBg");
            srGo.layer = 5;
            var srRt = srGo.AddComponent<RectTransform>();
            srRt.SetParent(barRt, false);
            srRt.anchorMin        = new Vector2(0.5f, 0.5f);
            srRt.anchorMax        = new Vector2(0.5f, 0.5f);
            srRt.pivot            = new Vector2(0.5f, 0.5f);
            srRt.anchoredPosition = Vector2.zero;
            srRt.sizeDelta        = new Vector2(200, 16);
            srGo.AddComponent<Image>().color = C_SrBg;
            MakeText("SearchTxt", srRt, font,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                "Search mail", 14, TextAlignmentOptions.Center, C_TextLight);

            // Floor / room info (updated at runtime)
            view.floorInfoLabel = MakeText("FloorInfo", barRt, font,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-10, 0), new Vector2(100, 14),
                "FL 1  RM 1", 14, TextAlignmentOptions.MidlineRight, C_TextMid);
        }

        // ── Sidebar ───────────────────────────────────────────────────────────

        static void BuildSidebar(RectTransform parent, TMP_FontAsset font, LayoutView view)
        {
            var sbGo = new GameObject("Sidebar");
            sbGo.layer = 5;
            var sbRt = sbGo.AddComponent<RectTransform>();
            sbRt.SetParent(parent, false);
            sbRt.anchorMin = new Vector2(0, 0);
            sbRt.anchorMax = new Vector2(0, 1);
            sbRt.pivot     = new Vector2(0, 0.5f);
            sbRt.offsetMin = Vector2.zero;
            sbRt.offsetMax = Vector2.zero;
            sbRt.sizeDelta = new Vector2(SidebarW, 0);
            sbGo.AddComponent<Image>().color = C_Sidebar;

            // Right border
            var borderGo = new GameObject("SbBorder");
            borderGo.layer = 5;
            var bRt = borderGo.AddComponent<RectTransform>();
            bRt.SetParent(sbRt, false);
            bRt.anchorMin = new Vector2(1, 0);
            bRt.anchorMax = new Vector2(1, 1);
            bRt.offsetMin = new Vector2(-1, 0);
            bRt.offsetMax = new Vector2(0, 0);
            borderGo.AddComponent<Image>().color = C_Sep;

            // Nav items
            for (int i = 0; i < NavLabels.Length; i++)
            {
                float  y         = -NavTopPad - i * NavItemGap;
                bool   isInbox   = i == InboxIndex;
                bool   isDrafts  = i == DraftsIndex;
                bool   isAllMail = i == AllMailIndex;
                bool   isActive  = isInbox;
                bool   isAccent  = isDrafts || isAllMail;
                Color  labelCol  = isActive || isAccent ? C_Accent : C_TextMid;

                var rowGo = new GameObject($"Nav_{NavLabels[i].Split(' ')[0]}");
                rowGo.layer = 5;
                var rowRt = rowGo.AddComponent<RectTransform>();
                rowRt.SetParent(sbRt, false);
                rowRt.anchorMin        = new Vector2(0, 1);
                rowRt.anchorMax        = new Vector2(1, 1);
                rowRt.pivot            = new Vector2(0, 1);
                rowRt.anchoredPosition = new Vector2(0, y);
                rowRt.sizeDelta        = new Vector2(0, NavItemH);

                // Active highlight background (INBOX is highlighted by default)
                var bgImg = rowGo.AddComponent<Image>();
                bgImg.color = isActive ? C_SbActive : Color.clear;

                // Label
                var lbl = MakeText("Label", rowRt, font,
                    new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f),
                    new Vector2(8, 0), new Vector2(-8, 0),
                    NavLabels[i], 14, TextAlignmentOptions.MidlineLeft, labelCol);

                // Button (all interactive items)
                if (isInbox || isDrafts || isAllMail)
                {
                    var btn = rowGo.AddComponent<Button>();
                    var cb  = btn.colors;
                    cb.normalColor      = Color.clear;
                    cb.highlightedColor = C_SbActive;
                    cb.pressedColor     = new Color(C_SbActive.r * 0.9f, C_SbActive.g * 0.9f, C_SbActive.b * 0.9f);
                    btn.colors = cb;

                    if (isInbox)        { view.inboxButton   = btn; }
                    else if (isDrafts)  { view.draftsButton  = btn; view.draftsLabel = lbl; }
                    else                { view.allMailButton = btn; }
                }
            }
        }

        // ── Auto-assign to scene ──────────────────────────────────────────────

        static void AssignToScene()
        {
            var screen = Object.FindObjectOfType<FloorMapScreen>();
            if (screen == null)
            {
                Debug.LogWarning("[InboxLayoutBuilder] FloorMapScreen not found in open scene — assign prefab manually.");
                return;
            }
            screen.layoutPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LayoutPrefabPath);
            EditorUtility.SetDirty(screen);
            EditorSceneManager.MarkSceneDirty(screen.gameObject.scene);
            Debug.Log("[InboxLayoutBuilder] Assigned InboxLayout.prefab to FloorMapScreen in scene.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        static TextMeshProUGUI MakeText(string name, RectTransform parent, TMP_FontAsset font,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta,
            string text, int fontSize, TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject(name);
            go.layer = 5;
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = sizeDelta;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text               = text;
            tmp.fontSize           = fontSize;
            tmp.color              = color;
            tmp.alignment          = alignment;
            tmp.enableWordWrapping = false;
            if (font != null) tmp.font = font;
            return tmp;
        }
    }
}
