using InboxZero.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.Editor
{
    /// Builds (or rebuilds) the AllMailPanel and CardRow prefabs from scratch.
    /// Menu: InboxZero → Rebuild All Mail Prefabs
    ///
    /// Run this once after the initial setup, then edit the prefabs visually.
    /// Re-run it only if you want to reset the prefabs to their default layout.
    public static class AllMailPrefabBuilder
    {
        const string PrefabFolder    = "Assets/Prefabs/UI";
        const string PanelPrefabPath = "Assets/Prefabs/UI/AllMailPanel.prefab";
        const string RowPrefabPath   = "Assets/Prefabs/UI/CardRow.prefab";
        const string BetterPixelsGuid = "af581fb1ba59971408d2278b6bffa1d5";

        // ── Colour palette (matches AllMailScreen) ────────────────────────────
        static readonly Color BgColor      = new Color(0.961f, 0.961f, 0.961f);
        static readonly Color TopBarColor  = new Color(0.914f, 0.941f, 0.984f);
        static readonly Color ColHeaderBg  = new Color(0.930f, 0.930f, 0.930f);
        static readonly Color DividerColor = new Color(0.855f, 0.855f, 0.855f);
        static readonly Color AccentColor  = new Color(0.102f, 0.451f, 0.910f);
        static readonly Color TextDark     = new Color(0.13f,  0.13f,  0.13f);
        static readonly Color TextMedium   = new Color(0.40f,  0.40f,  0.40f);
        static readonly Color RowBg        = new Color(0.996f, 0.996f, 0.996f);

        // ── Layout constants ──────────────────────────────────────────────────
        const float TopBarH    = 26f;
        const float DividerH   = 1f;
        const float ColHeaderH = 18f;
        const float RowH       = 27f;   // row height including bottom divider
        const float DotSize    = 6f;
        const float NameW      = 104f;
        const float PreviewW   = 220f;
        const float RarityW    = 60f;
        const float CostW      = 18f;
        const float MarginL    = 10f;
        const float MarginR    = 6f;

        [MenuItem("InboxZero/Rebuild All Mail Prefabs")]
        static void Build()
        {
            EnsureFolders();
            var font = LoadFont();
            BuildPanelPrefab(font);
            BuildRowPrefab(font);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AssignToScene();
            Debug.Log("[AllMailPrefabBuilder] Done. Prefabs saved to " + PrefabFolder);
        }

        // ── Folder setup ──────────────────────────────────────────────────────

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        }

        static TMP_FontAsset LoadFont()
        {
            var path = AssetDatabase.GUIDToAssetPath(BetterPixelsGuid);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("[AllMailPrefabBuilder] BetterPixels font not found by GUID — text will use TMP default.");
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }

        // ── Panel prefab ──────────────────────────────────────────────────────

        static void BuildPanelPrefab(TMP_FontAsset font)
        {
            var root   = new GameObject("AllMailPanel");
            root.layer = 5;
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            root.AddComponent<Image>().color = BgColor;

            var view = root.AddComponent<AllMailPanelView>();

            // ── Top bar ───────────────────────────────────────────────────────
            var topBar = MakeRect("TopBar", rootRt,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f),
                new Vector2(0, -TopBarH), new Vector2(0, TopBarH));
            topBar.gameObject.AddComponent<Image>().color = TopBarColor;

            MakeText("Title", topBar, font,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                new Vector2(MarginL, 0), new Vector2(120, 0),
                "< ALL MAIL", 14, TextAlignmentOptions.MidlineLeft, AccentColor);

            var statsLbl = MakeText("Stats", topBar, font,
                new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(200, 0),
                "ALL MAIL  0 / 0", 14, TextAlignmentOptions.Center, TextMedium);
            view.statsLabel = statsLbl;

            // Close button
            var closeBtnGo = new GameObject("CloseButton");
            closeBtnGo.layer = 5;
            var closeBtnRt   = closeBtnGo.AddComponent<RectTransform>();
            closeBtnRt.SetParent(topBar, false);
            closeBtnRt.anchorMin        = new Vector2(1, 0.5f);
            closeBtnRt.anchorMax        = new Vector2(1, 0.5f);
            closeBtnRt.pivot            = new Vector2(1, 0.5f);
            closeBtnRt.anchoredPosition = new Vector2(-MarginR, 0);
            closeBtnRt.sizeDelta        = new Vector2(52, 18);
            closeBtnGo.AddComponent<Image>().color = AccentColor;
            var btn = closeBtnGo.AddComponent<Button>();
            var cb  = btn.colors;
            cb.normalColor      = AccentColor;
            cb.highlightedColor = new Color(0.15f, 0.52f, 0.98f);
            cb.pressedColor     = new Color(0.08f, 0.38f, 0.80f);
            btn.colors = cb;
            MakeText("Label", closeBtnRt, font,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                "CLOSE", 14, TextAlignmentOptions.Center, Color.white);
            view.closeButton = btn;

            // ── Divider under top bar ─────────────────────────────────────────
            MakeRect("HDiv", rootRt,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f),
                new Vector2(0, -(TopBarH + DividerH)), new Vector2(0, DividerH))
                .gameObject.AddComponent<Image>().color = DividerColor;

            // ── Column header ─────────────────────────────────────────────────
            float colHeaderY = -(TopBarH + DividerH + ColHeaderH);
            var hdr = MakeRect("ColHeader", rootRt,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0f, 1f),
                new Vector2(0, colHeaderY), new Vector2(0, ColHeaderH));
            hdr.gameObject.AddComponent<Image>().color = ColHeaderBg;
            var colLbl = MakeText("Label", hdr, font,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f),
                new Vector2(MarginL, 0), Vector2.zero,
                "ALL CARDS  (0)", 14, TextAlignmentOptions.MidlineLeft, TextDark);
            view.columnHeaderLabel = colLbl;

            MakeRect("ColHDiv", rootRt,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f),
                new Vector2(0, -(TopBarH + DividerH + ColHeaderH + DividerH)), new Vector2(0, DividerH))
                .gameObject.AddComponent<Image>().color = DividerColor;

            // ── Scroll area ───────────────────────────────────────────────────
            float listTop = TopBarH + DividerH + ColHeaderH + DividerH;

            var scrollGo = new GameObject("Scroll");
            scrollGo.layer = 5;
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.SetParent(rootRt, false);
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = new Vector2(0, -listTop);
            scrollGo.AddComponent<Image>().color = BgColor;

            var viewportGo = new GameObject("Viewport");
            viewportGo.layer = 5;
            var viewportRt = viewportGo.AddComponent<RectTransform>();
            viewportRt.SetParent(scrollRt, false);
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            viewportGo.AddComponent<RectMask2D>();

            var contentGo = new GameObject("Content");
            contentGo.layer = 5;
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.SetParent(viewportRt, false);
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot     = new Vector2(0, 1);
            contentRt.sizeDelta = Vector2.zero;
            view.scrollContent  = contentRt;

            var sr = scrollGo.AddComponent<ScrollRect>();
            sr.content           = contentRt;
            sr.viewport          = viewportRt;
            sr.horizontal        = false;
            sr.vertical          = true;
            sr.scrollSensitivity = 20f;
            sr.movementType      = ScrollRect.MovementType.Clamped;

            PrefabUtility.SaveAsPrefabAsset(root, PanelPrefabPath);
            Object.DestroyImmediate(root);
        }

        // ── Card row prefab ───────────────────────────────────────────────────

        static void BuildRowPrefab(TMP_FontAsset font)
        {
            var root   = new GameObject("CardRow");
            root.layer = 5;
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0, 1);
            rootRt.anchorMax = new Vector2(1, 1);
            rootRt.pivot     = new Vector2(0, 1);
            rootRt.sizeDelta = new Vector2(0, RowH);
            var bgImg = root.AddComponent<Image>();
            bgImg.color = RowBg;

            var rowView       = root.AddComponent<CardRowView>();
            rowView.background = bgImg;

            // Type-colour dot
            var dotGo = new GameObject("Dot");
            dotGo.layer = 5;
            var dotRt = dotGo.AddComponent<RectTransform>();
            dotRt.SetParent(rootRt, false);
            dotRt.anchorMin        = new Vector2(0, 0.5f);
            dotRt.anchorMax        = new Vector2(0, 0.5f);
            dotRt.pivot            = new Vector2(0, 0.5f);
            dotRt.anchoredPosition = new Vector2(MarginL, 0);
            dotRt.sizeDelta        = new Vector2(DotSize, DotSize);
            var dotImg = dotGo.AddComponent<Image>();
            dotImg.color = Color.white;
            rowView.dot  = dotImg;

            // Labels
            rowView.nameLabel = MakeText("Name", rootRt, font,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                new Vector2(MarginL + DotSize + 6f, 0), new Vector2(NameW, 0),
                "CARD NAME", 14, TextAlignmentOptions.MidlineLeft, TextDark);

            rowView.previewLabel = MakeText("Preview", rootRt, font,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                new Vector2(MarginL + DotSize + 6f + NameW + 6f, 0), new Vector2(PreviewW, 0),
                "Effect description", 14, TextAlignmentOptions.MidlineLeft, new Color(0.60f, 0.60f, 0.60f));

            rowView.rarityLabel = MakeText("Rarity", rootRt, font,
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f),
                new Vector2(-(CostW + MarginR + 4f), 0), new Vector2(RarityW, 0),
                "RARITY", 14, TextAlignmentOptions.MidlineRight, new Color(0.60f, 0.60f, 0.60f));

            rowView.costLabel = MakeText("Cost", rootRt, font,
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f),
                new Vector2(-MarginR, 0), new Vector2(CostW, 0),
                "0", 14, TextAlignmentOptions.MidlineRight, TextMedium);

            // Bottom divider
            var divGo = new GameObject("Divider");
            divGo.layer = 5;
            var divRt = divGo.AddComponent<RectTransform>();
            divRt.SetParent(rootRt, false);
            divRt.anchorMin        = new Vector2(0, 0);
            divRt.anchorMax        = new Vector2(1, 0);
            divRt.pivot            = new Vector2(0, 0);
            divRt.anchoredPosition = Vector2.zero;
            divRt.sizeDelta        = new Vector2(0, DividerH);
            var divImg = divGo.AddComponent<Image>();
            divImg.color    = DividerColor;
            rowView.divider = divImg;

            PrefabUtility.SaveAsPrefabAsset(root, RowPrefabPath);
            Object.DestroyImmediate(root);
        }

        // ── Auto-assign prefabs to AllMailScreen in the open scene ────────────

        static void AssignToScene()
        {
            var screen = Object.FindObjectOfType<AllMailScreen>();
            if (screen == null)
            {
                Debug.LogWarning("[AllMailPrefabBuilder] AllMailScreen not found in open scene — assign prefabs manually.");
                return;
            }

            screen.panelPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
            screen.cardRowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath);
            EditorUtility.SetDirty(screen);
            EditorSceneManager.MarkSceneDirty(screen.gameObject.scene);
            Debug.Log("[AllMailPrefabBuilder] Assigned prefabs to AllMailScreen in scene.");
        }

        // ── Shared helpers ────────────────────────────────────────────────────

        static RectTransform MakeRect(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta)
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
            return rt;
        }

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
