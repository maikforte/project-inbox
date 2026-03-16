using InboxZero.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.Editor
{
    /// Builds (or rebuilds) the DraftsPage prefab from scratch.
    /// Menu: InboxZero → Rebuild Drafts Page Prefab
    ///
    /// Run once after initial setup. Re-run only to reset to default layout.
    public static class DraftsPageBuilder
    {
        const string PrefabFolder   = "Assets/Prefabs/UI";
        const string PagePrefabPath = "Assets/Prefabs/UI/DraftsPage.prefab";
        const string Micro5Guid     = "968711493fda13b4789c59de06321a30";

        static readonly Color BgColor      = new Color(0.961f, 0.961f, 0.961f);
        static readonly Color ColHeaderBg  = new Color(0.930f, 0.930f, 0.930f);
        static readonly Color DividerColor = new Color(0.855f, 0.855f, 0.855f);
        static readonly Color TextDark     = new Color(0.13f,  0.13f,  0.13f);
        static readonly Color TextMedium   = new Color(0.40f,  0.40f,  0.40f);

        const float HeaderH    = 24f;
        const float DividerH   = 1f;
        const float ColHeaderH = 18f;
        const float MarginL    = 10f;

        [MenuItem("InboxZero/Rebuild Drafts Page Prefab")]
        static void Build()
        {
            EnsureFolders();
            var font = LoadFont();
            BuildPagePrefab(font);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AssignToScene();
            Debug.Log("[DraftsPageBuilder] Done. Prefab saved to " + PagePrefabPath);
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
                Debug.LogWarning("[DraftsPageBuilder] Micro5-Regular not found by GUID — text will use TMP default.");
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }

        static void BuildPagePrefab(TMP_FontAsset font)
        {
            var root   = new GameObject("DraftsPage");
            root.layer = 5;
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            root.AddComponent<Image>().color = BgColor;

            var view = root.AddComponent<DraftsPageView>();

            float hdrBottom = HeaderH + DividerH + ColHeaderH + DividerH;

            // ── Stats header ──────────────────────────────────────────────────
            var hdrRt = MakeRect("Header", rootRt,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1),
                Vector2.zero, new Vector2(0, HeaderH));
            hdrRt.gameObject.AddComponent<Image>().color = ColHeaderBg;
            view.statsLabel = MakeText("Stats", hdrRt, font,
                new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(260, 0),
                "DECK 0/15  UC 0/4  RARE 0/2", 14, TextAlignmentOptions.Center, TextMedium);

            MakeRect("HDiv", rootRt,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1),
                new Vector2(0, -HeaderH), new Vector2(0, DividerH))
                .gameObject.AddComponent<Image>().color = DividerColor;

            // ── Column headers ────────────────────────────────────────────────
            float colHdrY = -(HeaderH + DividerH);

            var deckHdrRt = MakeRect("DeckHeader", rootRt,
                new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(0, 1),
                new Vector2(0, colHdrY), new Vector2(0, ColHeaderH));
            deckHdrRt.gameObject.AddComponent<Image>().color = ColHeaderBg;
            view.deckHeaderLabel = MakeText("Label", deckHdrRt, font,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f),
                new Vector2(MarginL, 0), Vector2.zero,
                "ACTIVE DECK  (0)", 14, TextAlignmentOptions.MidlineLeft, TextDark);

            var collHdrRt = MakeRect("CollHeader", rootRt,
                new Vector2(0.5f, 1), new Vector2(1, 1), new Vector2(0, 1),
                new Vector2(0, colHdrY), new Vector2(0, ColHeaderH));
            collHdrRt.gameObject.AddComponent<Image>().color = ColHeaderBg;
            view.collHeaderLabel = MakeText("Label", collHdrRt, font,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f),
                new Vector2(MarginL, 0), Vector2.zero,
                "COLLECTION  (0)", 14, TextAlignmentOptions.MidlineLeft, TextMedium);

            MakeRect("ColHDiv", rootRt,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1),
                new Vector2(0, -(HeaderH + DividerH + ColHeaderH)), new Vector2(0, DividerH))
                .gameObject.AddComponent<Image>().color = DividerColor;

            // ── Vertical centre divider ───────────────────────────────────────
            MakeRect("VDiv", rootRt,
                new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(DividerH, 0))
                .gameObject.AddComponent<Image>().color = DividerColor;

            // ── Two scroll areas ──────────────────────────────────────────────
            view.deckContent = BuildScrollArea("DeckScroll", rootRt,
                new Vector2(0,    0), new Vector2(0.5f, 1),
                new Vector2(0, 0), new Vector2(0, -hdrBottom));

            view.collContent = BuildScrollArea("CollScroll", rootRt,
                new Vector2(0.5f, 0), new Vector2(1, 1),
                new Vector2(0, 0), new Vector2(0, -hdrBottom));

            PrefabUtility.SaveAsPrefabAsset(root, PagePrefabPath);
            Object.DestroyImmediate(root);
        }

        // Sprite GUIDs copied from AllMailPanel.prefab
        const string TrackSpriteGuid  = "e1a0c6b2962745b4b9b45dcafa6a4610";
        const string HandleSpriteGuid = "0581721460943934fba8a8d1e49d3914";
        const float  ScrollbarW       = 5f;
        const float  ScrollbarGap     = 3f; // gap between viewport and scrollbar

        static RectTransform BuildScrollArea(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.layer = 5;
            var scrollRt = go.AddComponent<RectTransform>();
            scrollRt.SetParent(parent, false);
            scrollRt.anchorMin = anchorMin;
            scrollRt.anchorMax = anchorMax;
            scrollRt.offsetMin = offsetMin;
            scrollRt.offsetMax = offsetMax;
            go.AddComponent<Image>().color = new Color(0.961f, 0.961f, 0.961f);

            // Viewport — inset right to make room for the scrollbar
            var viewGo = new GameObject("Viewport");
            viewGo.layer = 5;
            var viewRt = viewGo.AddComponent<RectTransform>();
            viewRt.SetParent(scrollRt, false);
            viewRt.anchorMin = Vector2.zero;
            viewRt.anchorMax = Vector2.one;
            viewRt.offsetMin = Vector2.zero;
            viewRt.offsetMax = new Vector2(-(ScrollbarW + ScrollbarGap), 0);
            viewGo.AddComponent<RectMask2D>();

            // Content
            var contentGo = new GameObject("Content");
            contentGo.layer = 5;
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.SetParent(viewRt, false);
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot     = new Vector2(0, 1);
            contentRt.sizeDelta = Vector2.zero;

            // Scrollbar track
            var sbGo = new GameObject("Scrollbar");
            sbGo.layer = 5;
            var sbRt = sbGo.AddComponent<RectTransform>();
            sbRt.SetParent(scrollRt, false);
            sbRt.anchorMin        = new Vector2(1, 0);
            sbRt.anchorMax        = new Vector2(1, 1);
            sbRt.pivot            = new Vector2(1, 0.5f);
            sbRt.anchoredPosition = Vector2.zero;
            sbRt.sizeDelta        = new Vector2(ScrollbarW, 0);
            var trackImg = sbGo.AddComponent<Image>();
            trackImg.color = Color.white;
            LoadTiledSprite(trackImg, TrackSpriteGuid);

            // Sliding Area (bounds for handle movement)
            var slidingGo = new GameObject("Sliding Area");
            slidingGo.layer = 5;
            var slidingRt = slidingGo.AddComponent<RectTransform>();
            slidingRt.SetParent(sbRt, false);
            slidingRt.anchorMin = Vector2.zero;
            slidingRt.anchorMax = Vector2.one;
            slidingRt.offsetMin = Vector2.zero;
            slidingRt.offsetMax = Vector2.zero;

            // Handle
            var handleGo = new GameObject("Handle");
            handleGo.layer = 5;
            var handleRt = handleGo.AddComponent<RectTransform>();
            handleRt.SetParent(slidingRt, false);
            handleRt.anchorMin = Vector2.zero;
            handleRt.anchorMax = Vector2.zero;
            handleRt.sizeDelta = new Vector2(ScrollbarW, ScrollbarW);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = Color.white;
            LoadTiledSprite(handleImg, HandleSpriteGuid);

            // Scrollbar component
            var scrollbar = sbGo.AddComponent<Scrollbar>();
            scrollbar.targetGraphic = trackImg;
            scrollbar.handleRect    = handleRt;
            scrollbar.direction     = Scrollbar.Direction.BottomToTop;

            // ScrollRect
            var sr = go.AddComponent<ScrollRect>();
            sr.content                   = contentRt;
            sr.viewport                  = viewRt;
            sr.horizontal                = false;
            sr.vertical                  = true;
            sr.scrollSensitivity         = 20f;
            sr.movementType              = ScrollRect.MovementType.Clamped;
            sr.verticalScrollbar         = scrollbar;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            return contentRt;
        }

        static void LoadTiledSprite(Image img, string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) return;
            img.sprite = sprite;
            img.type   = Image.Type.Tiled;
        }

        static void AssignToScene()
        {
            var db = Object.FindObjectOfType<DeckBuilderScreen>();
            if (db == null)
            {
                Debug.LogWarning("[DraftsPageBuilder] DeckBuilderScreen not found in open scene — assign prefab manually.");
                return;
            }
            db.pagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PagePrefabPath);
            EditorUtility.SetDirty(db);
            EditorSceneManager.MarkSceneDirty(db.gameObject.scene);
            Debug.Log("[DraftsPageBuilder] Assigned DraftsPage.prefab to DeckBuilderScreen in scene.");
        }

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
