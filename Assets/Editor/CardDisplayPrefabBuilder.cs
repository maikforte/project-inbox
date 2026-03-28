using InboxZero.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.Editor
{
    /// Builds the CardDisplay prefab from scratch.
    /// Menu: InboxZero → Rebuild Card Display Prefab
    ///
    /// Run this once to create the prefab, then assign it to AllMailScreen.
    /// Re-run only to reset the prefab to defaults — customizations will be wiped.
    public static class CardDisplayPrefabBuilder
    {
        public const string PrefabPath = "Assets/Prefabs/UI/CardDisplay.prefab";

        const string BetterPixelsGuid    = "af581fb1ba59971408d2278b6bffa1d5";
        const string StarterSpriteGuid   = "d054ef1141d02884884b0cabc2eee9bd";
        const string CommonSpriteGuid    = "5090bc80bb27f614e96ce1de16f56453";
        const string UncommonSpriteGuid  = "8241c7e8fe324f94abf6ae12e8227175";
        const string RareSpriteGuid      = "73541f3d51bfd244fa76dbf9f7f398e5";
        const string LegendarySpriteGuid = "230513f83082f614bbb4ad9ce502dcc9";
        const string PlaceholderIconGuid = "10caa0db958392740bdad915057d8128";

        [MenuItem("InboxZero/Rebuild Card Display Prefab")]
        public static void Build()
        {
            EnsureFolder();
            var font = LoadFont();

            var root   = new GameObject("CardDisplay");
            root.layer = 5;
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.zero;
            rootRt.pivot     = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta = new Vector2(90, 110);

            var view = root.AddComponent<CardDisplayView>();

            // ── BG ────────────────────────────────────────────────────────────
            var bgRt  = MakeRect("BG", rootRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var bgImg = bgRt.gameObject.AddComponent<Image>();
            bgImg.sprite = LoadSprite(StarterSpriteGuid);
            view.background = bgImg;

            view.starterVisuals   = new RarityVisuals { bg = LoadSprite(StarterSpriteGuid) };
            view.commonVisuals    = new RarityVisuals { bg = LoadSprite(CommonSpriteGuid) };
            view.uncommonVisuals  = new RarityVisuals { bg = LoadSprite(UncommonSpriteGuid) };
            view.rareVisuals      = new RarityVisuals { bg = LoadSprite(RareSpriteGuid) };
            view.legendaryVisuals = new RarityVisuals { bg = LoadSprite(LegendarySpriteGuid) };

            // ── CardIcon — fills the top portion of the card ──────────────────
            // Layout: 110px tall card. Bottom 14px = PoolToggle, next 20px = CardName, top 76px = icon.
            var iconRt  = MakeRect("CardIcon", rootRt,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -4f), new Vector2(80f, 72f));
            var iconImg = iconRt.gameObject.AddComponent<Image>();
            iconImg.sprite         = LoadSprite(PlaceholderIconGuid);
            iconImg.preserveAspect = true;
            view.icon = iconImg;

            // ── CardName — sits just above the PoolToggle bar ─────────────────
            view.nameText = MakeText("CardName", rootRt, font,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
                new Vector2(0, 14f), new Vector2(0, 20f),
                "CARD NAME", 14, TextAlignmentOptions.Center, Color.white);

            // CostBadge and EffectText are intentionally omitted from CardDisplay.prefab.
            // Details are shown in the hover preview panel. view.costText and view.effectText remain null.

            // ── DisabledOverlay ────────────────────────────────────────────────
            var overlayRt  = MakeRect("DisabledOverlay", rootRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var overlayImg = overlayRt.gameObject.AddComponent<Image>();
            overlayImg.color         = new Color(0.25f, 0.25f, 0.28f, 0.72f);
            overlayImg.raycastTarget = false;
            overlayRt.gameObject.SetActive(false);
            view.disabledOverlay = overlayRt.gameObject;

            // ── PoolToggle bar ─────────────────────────────────────────────────
            var barRt  = MakeRect("PoolToggle", rootRt,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
                Vector2.zero, new Vector2(0, 14));
            var barImg = barRt.gameObject.AddComponent<Image>();
            barImg.color = new Color(0, 0, 0, 0.65f);

            var btn = barRt.gameObject.AddComponent<Button>();
            btn.targetGraphic = barImg;
            var cb = btn.colors;
            cb.normalColor      = new Color(0,    0,    0,    0.65f);
            cb.highlightedColor = new Color(0.2f, 0.18f,0.25f,0.9f);
            cb.pressedColor     = new Color(0.05f,0.04f,0.08f,0.9f);
            cb.colorMultiplier  = 1f;
            btn.colors = cb;

            var lblTxt = MakeText("Label", barRt, font,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                "ON", 14, TextAlignmentOptions.Center, new Color(1f, 0.88f, 0.55f));
            lblTxt.raycastTarget = false;

            view.poolToggle   = btn;
            view.toggleLabel  = lblTxt;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CardDisplayPrefabBuilder] Saved to " + PrefabPath);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        }

        static TMP_FontAsset LoadFont()
        {
            var path = AssetDatabase.GUIDToAssetPath(BetterPixelsGuid);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("[CardDisplayPrefabBuilder] BetterPixels font not found — text will use TMP default.");
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }

        static Sprite LoadSprite(string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
            go.AddComponent<CanvasRenderer>();
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
