using System;
using System.Collections.Generic;
using System.Linq;
using InboxZero.Core;
using InboxZero.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// All Mail compendium — shows every card in a grid using the actual Card prefab.
    /// Locked cards are dimmed with "???" text. Unlocked non-starter cards have an ON/OFF toggle.
    public class AllMailScreen : MonoBehaviour
    {
        public static AllMailScreen Instance { get; private set; }

        [Header("Prefabs")]
        public GameObject panelPrefab;
        public CardDisplayView cardDisplayPrefab;

        [Header("Back Button Sprites")]
        [Tooltip("Normal sprite — same as MainMenu buttons (Inputs.png normal state).")]
        public Sprite buttonNormalSprite;
        [Tooltip("Pressed sprite — same as MainMenu buttons (Inputs.png pressed state).")]
        public Sprite buttonPressedSprite;

        [Header("Card Registry")]
        [Tooltip("Assign the AllCardsRegistry SO here.")]
        public AllCardsRegistry registry;

        /// Fired when the player clicks Close.
        public UnityEvent OnClose = new UnityEvent();

        Canvas     _canvas;
        GameObject _panel;

        // Hover preview overlay
        GameObject         _preview;
        Image              _previewTypeDot;
        TextMeshProUGUI    _previewName;
        TextMeshProUGUI    _previewMeta;
        TextMeshProUGUI    _previewEffect;
        TextMeshProUGUI    _previewFlavor;

        // Palette
        static readonly Color Amber     = new Color(1.00f, 0.88f, 0.55f);
        static readonly Color TextMid   = new Color(0.78f, 0.78f, 0.82f);
        static readonly Color TextDim   = new Color(0.50f, 0.50f, 0.55f);
        static readonly Color BgPreview = new Color(0.07f, 0.05f, 0.11f, 0.97f);

        // Grid settings
        const float CellW   = 90f;
        const float CellH   = 110f;
        const float SpacingX = 8f;
        const float SpacingY = 8f;
        const int   PadAll   = 8;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _canvas  = FindObjectOfType<Canvas>();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Show()
        {
            if (_panel != null) Destroy(_panel);
            BuildPanel(_canvas != null ? _canvas.transform : null);
        }

        public void ShowInContent(RectTransform contentArea)
        {
            if (_panel != null) Destroy(_panel);
            BuildPanel(contentArea);
        }

        public void Hide()
        {
            if (_panel != null) Destroy(_panel);
            _panel   = null;
            _preview = null;
        }

        // ── Panel construction ────────────────────────────────────────────────

        void BuildPanel(Transform parent)
        {
            if (parent == null)    { Debug.LogError("[AllMailScreen] No parent.");           return; }
            if (registry == null)  { Debug.LogWarning("[AllMailScreen] No AllCardsRegistry."); return; }
            if (panelPrefab == null){ Debug.LogError("[AllMailScreen] panelPrefab missing."); return; }
            if (cardDisplayPrefab == null) { Debug.LogError("[AllMailScreen] cardDisplayPrefab missing."); return; }

            _panel = Instantiate(panelPrefab, parent, false);

            var view = _panel.GetComponent<AllMailPanelView>();
            if (view == null) { Debug.LogError("[AllMailScreen] Missing AllMailPanelView."); return; }

            if (view.closeButton != null)
            {
                view.closeButton.onClick.AddListener(OnCloseClicked);
                // Apply main-menu sprite-swap style if sprites are assigned
                if (buttonNormalSprite != null && buttonPressedSprite != null)
                {
                    var img = view.closeButton.GetComponent<Image>();
                    if (img != null) { img.sprite = buttonNormalSprite; img.type = Image.Type.Sliced; }
                    view.closeButton.transition = Selectable.Transition.SpriteSwap;
                    var ss = view.closeButton.spriteState;
                    ss.highlightedSprite = buttonNormalSprite;
                    ss.pressedSprite     = buttonPressedSprite;
                    ss.selectedSprite    = buttonNormalSprite;
                    ss.disabledSprite    = buttonPressedSprite;
                    view.closeButton.spriteState = ss;
                }
            }

            // ── Stats header ──────────────────────────────────────────────────
            var um = UnlockManager.Instance;
            int total = registry.allCards.Count, unlocked = 0;
            foreach (var c in registry.allCards)
            {
                bool u = um == null || um.IsUnlocked(c);
                if (u) unlocked++;
            }

            if (view.statsLabel != null)
                view.statsLabel.text = $"{unlocked} / {total}";
            if (view.columnHeaderLabel != null)
                view.columnHeaderLabel.text = "ALL CARDS";

            // ── scrollContent: vertical stack of rarity sections ──────────────
            if (view.scrollContent != null)
            {
                var vl = view.scrollContent.GetComponent<VerticalLayoutGroup>();
                if (vl == null) vl = view.scrollContent.gameObject.AddComponent<VerticalLayoutGroup>();
                vl.spacing              = 12f;
                vl.padding              = new RectOffset(PadAll, PadAll, PadAll, PadAll);
                vl.childControlWidth    = true;
                vl.childControlHeight   = true;
                vl.childForceExpandWidth  = true;
                vl.childForceExpandHeight = false;

                var csf = view.scrollContent.GetComponent<ContentSizeFitter>();
                if (csf == null) csf = view.scrollContent.gameObject.AddComponent<ContentSizeFitter>();
                csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
                csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }

            // ── Group cards by rarity, sorted by name within each group ───────
            var byRarity = new Dictionary<CardRarity, List<CardData>>();
            foreach (var c in registry.allCards)
            {
                if (!byRarity.ContainsKey(c.rarity)) byRarity[c.rarity] = new List<CardData>();
                byRarity[c.rarity].Add(c);
            }
            foreach (var list in byRarity.Values)
                list.Sort((a, b) => string.Compare(a.cardName, b.cardName, System.StringComparison.Ordinal));

            // ── One section per rarity in display order ────────────────────────
            int colCount = ComputeColumns(view.scrollContent.rect.width);
            foreach (var rarity in new[] { CardRarity.Starter, CardRarity.Common, CardRarity.Uncommon, CardRarity.Rare, CardRarity.Legendary })
            {
                if (!byRarity.TryGetValue(rarity, out var rarityCards)) continue;

                // Section container
                var section   = MakeGO("Section_" + rarity, view.scrollContent);
                var sectionVl = section.AddComponent<VerticalLayoutGroup>();
                sectionVl.spacing              = 4f;
                sectionVl.childControlWidth    = true;
                sectionVl.childControlHeight   = true;
                sectionVl.childForceExpandWidth  = true;
                sectionVl.childForceExpandHeight = false;
                var sectionCsf = section.AddComponent<ContentSizeFitter>();
                sectionCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // Section header
                BuildSectionHeader(section.transform, rarity, rarityCards, um);

                // Card grid
                var gridGo  = MakeGO("Grid", section.transform);
                var grid    = gridGo.AddComponent<GridLayoutGroup>();
                grid.cellSize        = new Vector2(CellW, CellH);
                grid.spacing         = new Vector2(SpacingX, SpacingY);
                grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = colCount;
                grid.childAlignment  = TextAnchor.UpperLeft;
                var gridCsf = gridGo.AddComponent<ContentSizeFitter>();
                gridCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // Cards
                foreach (var card in rarityCards)
                {
                    bool isUnlocked = um == null || um.IsUnlocked(card);
                    bool isEnabled  = um == null || um.IsEnabled(card);
                    bool isStarter  = card.rarity == CardRarity.Starter;

                    var cdv = Instantiate(cardDisplayPrefab, gridGo.transform, false);
                    cdv.Populate(card);

                    if (!isUnlocked)
                    {
                        cdv.SetLocked(true);
                        cdv.ShowToggle(false, false, null);
                    }
                    else
                    {
                        WireHover(cdv.gameObject, card);
                        cdv.SetDisabled(!isEnabled);
                        bool canToggle = !isStarter;
                        cdv.ShowToggle(canToggle, isEnabled,
                            canToggle ? state => OnToggleCard(card, state, um) : (Action<bool>)null);
                    }
                }
            }

            // Build hover preview panel
            _preview = BuildPreviewPanel(_panel.transform);
        }

        // ── Rarity section header ─────────────────────────────────────────────

        void BuildSectionHeader(Transform parent, CardRarity rarity, List<CardData> cards, UnlockManager um)
        {
            Color rarityCol = RarityColor(rarity);

            var header   = MakeGO("Header", parent);
            var headerLe = header.AddComponent<LayoutElement>();
            headerLe.preferredHeight = 28f;
            headerLe.flexibleWidth   = 1f;

            // Rarity label
            var lbl   = MakeGO("Label", header.transform);
            var lblRt = lbl.GetComponent<RectTransform>();
            lblRt.anchorMin        = new Vector2(0, 0);
            lblRt.anchorMax        = new Vector2(1, 1);
            lblRt.offsetMin        = new Vector2(4, 0);
            lblRt.offsetMax        = new Vector2(-4, 0);
            lbl.AddComponent<CanvasRenderer>();
            var font = _panel.GetComponentInChildren<TextMeshProUGUI>()?.font;
            var tmp  = lbl.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.fontSize           = 20;
            tmp.alignment          = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget      = false;
            tmp.enableWordWrapping = false;

            // Count unlocked/enabled for this rarity
            bool always = rarity == CardRarity.Starter || rarity == CardRarity.Common;
            int total    = cards.Count;
            int unlocked = cards.Count(c => um == null || um.IsUnlocked(c));
            int enabled  = cards.Count(c => um == null || um.IsEnabled(c));

            string countStr = always
                ? $"<color=#{ColorToHex(TextMid)}>{total} CARDS</color>"
                : $"<color=#{ColorToHex(TextMid)}>{unlocked}/{total} UNLOCKED</color>";

            tmp.text  = $"<color=#{ColorToHex(rarityCol)}>{rarity.ToString().ToUpper()}</color>  {countStr}";
            tmp.color = Color.white;

        }

        static Color RarityColor(CardRarity rarity) => rarity switch
        {
            CardRarity.Common    => new Color(0.78f, 0.78f, 0.82f),
            CardRarity.Uncommon  => new Color(0.20f, 0.72f, 0.38f),
            CardRarity.Rare      => new Color(0.62f, 0.28f, 0.90f),
            CardRarity.Legendary => new Color(1.00f, 0.70f, 0.10f),
            _                    => new Color(0.60f, 0.60f, 0.65f), // Starter
        };

        static string ColorToHex(Color c)
        {
            return $"{ToByte(c.r):X2}{ToByte(c.g):X2}{ToByte(c.b):X2}";
        }

        static int ToByte(float v) => Mathf.Clamp(Mathf.RoundToInt(v * 255), 0, 255);

        // ── Hover preview ─────────────────────────────────────────────────────

        void WireHover(GameObject cardGo, CardData card)
        {
            var cdv = cardGo.GetComponent<CardDisplayView>();
            if (cdv != null) cdv.SetHoverCallbacks(() => ShowPreview(card), HidePreview);
        }

        void ShowPreview(CardData card)
        {
            if (_preview == null) return;
            _previewTypeDot.color = TypeColor(card.cardType);
            _previewName.text     = card.cardName.ToUpper();
            _previewMeta.text     = $"{card.cardType.ToString().ToUpper()}  ·  {card.rarity.ToString().ToUpper()}  ·  {card.apCost} AP";
            _previewEffect.text   = card.effectDescription;
            if (_previewFlavor != null)
            {
                bool has = !string.IsNullOrWhiteSpace(card.flavorText);
                _previewFlavor.gameObject.SetActive(has);
                if (has) _previewFlavor.text = card.flavorText;
            }
            _preview.SetActive(true);
        }

        void HidePreview() { if (_preview != null) _preview.SetActive(false); }

        GameObject BuildPreviewPanel(Transform parent)
        {
            var font = _panel.GetComponentInChildren<TextMeshProUGUI>()?.font;

            var root = MakeGO("CardHoverPreview", parent);
            var rt   = root.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1, 0.5f);
            rt.anchorMax        = new Vector2(1, 0.5f);
            rt.pivot            = new Vector2(1, 0.5f);
            rt.anchoredPosition = new Vector2(-6, 0);
            rt.sizeDelta        = new Vector2(172, 152);
            root.AddComponent<CanvasRenderer>();
            var bg = root.AddComponent<Image>();
            bg.color         = BgPreview;
            bg.raycastTarget = false;

            // Type bar
            var barGo = MakeGO("TypeBar", root.transform);
            var barRt = barGo.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 1); barRt.anchorMax = new Vector2(1, 1);
            barRt.pivot     = new Vector2(0.5f, 1);
            barRt.anchoredPosition = Vector2.zero; barRt.sizeDelta = new Vector2(0, 4);
            barGo.AddComponent<CanvasRenderer>();
            _previewTypeDot = barGo.AddComponent<Image>();
            _previewTypeDot.raycastTarget = false;

            const float pad = 8f, w = 172f - pad * 2f;
            _previewName   = MakeLabel(root.transform, "PName",   pad, -10, w,  16, font, Amber);
            _previewMeta   = MakeLabel(root.transform, "PMeta",   pad, -28, w,  14, font, TextDim);
            MakeDivider(root.transform, -44, pad);
            _previewEffect = MakeLabel(root.transform, "PEffect", pad, -52, w,  72, font, TextMid, wrapping: true);
            _previewFlavor = MakeLabel(root.transform, "PFlavor", pad,   6, w,  24, font, TextDim, wrapping: true, fromBottom: true);

            root.SetActive(false);
            return root;
        }

        // ── Runtime UI helpers ────────────────────────────────────────────────

        static GameObject MakeGO(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return go;
        }

        static TextMeshProUGUI MakeLabel(Transform parent, string name, float x, float y, float w, float h,
            TMP_FontAsset font, Color color, bool wrapping = false, bool fromBottom = false)
        {
            var go = MakeGO(name, parent);
            go.AddComponent<CanvasRenderer>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = fromBottom ? new Vector2(0, 0) : new Vector2(0, 1);
            rt.anchorMax = fromBottom ? new Vector2(0, 0) : new Vector2(0, 1);
            rt.pivot     = fromBottom ? new Vector2(0, 0) : new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, fromBottom ? y : y);
            rt.sizeDelta        = new Vector2(w, h);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.fontSize           = 14;
            tmp.color              = color;
            tmp.raycastTarget      = false;
            tmp.enableWordWrapping = wrapping;
            tmp.overflowMode       = TMPro.TextOverflowModes.Ellipsis;
            return tmp;
        }

        static void MakeDivider(Transform parent, float y, float pad)
        {
            var go = MakeGO("Div", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta        = new Vector2(-pad * 2, 1);
            go.AddComponent<CanvasRenderer>();
            var img = go.AddComponent<Image>();
            img.color        = new Color(0.25f, 0.23f, 0.30f);
            img.raycastTarget = false;
        }

        // ── Utility ───────────────────────────────────────────────────────────

        static int ComputeColumns(float availableWidth)
        {
            if (availableWidth <= 0) return 5; // fallback
            int cols = Mathf.FloorToInt((availableWidth - PadAll * 2 + SpacingX) / (CellW + SpacingX));
            return Mathf.Max(1, cols);
        }

        static Color TypeColor(CardType type) => type switch
        {
            CardType.Attack => new Color(0.83f, 0.18f, 0.18f),
            CardType.Defend => new Color(0.20f, 0.66f, 0.32f),
            _               => new Color(0.52f, 0.18f, 0.80f),
        };

        void OnToggleCard(CardData card, bool enable, UnlockManager um)
        {
            if (um == null) return;

            if (!enable)
            {
                // Guard against dropping below pool minimums.
                if (!um.CanDisable(card, registry, out string reason))
                {
                    FlashWarning(reason);
                    // Rebuild so the toggle snaps back to its previous state.
                    if (_panel != null) { var p = _panel.transform.parent; Hide(); BuildPanel(p); }
                    return;
                }
            }

            um.SetEnabled(card, enable);
        }

        void FlashWarning(string message)
        {
            if (_panel == null) return;
            var rt = _panel.GetComponent<RectTransform>() ?? _panel.AddComponent<RectTransform>();
            var warn = new GameObject("Warning", typeof(RectTransform));
            warn.layer = 5;
            var wrt = warn.GetComponent<RectTransform>();
            wrt.SetParent(rt, false);
            wrt.anchorMin        = new Vector2(0.5f, 0f);
            wrt.anchorMax        = new Vector2(0.5f, 0f);
            wrt.pivot            = new Vector2(0.5f, 0f);
            wrt.anchoredPosition = new Vector2(0, 12f);
            wrt.sizeDelta        = new Vector2(260f, 22f);
            warn.AddComponent<CanvasRenderer>();
            warn.AddComponent<Image>().color = new Color(0.55f, 0.10f, 0.10f, 0.92f);
            var font = _panel.GetComponentInChildren<TextMeshProUGUI>()?.font;
            var tmp  = warn.AddComponent<TextMeshProUGUI>();
            tmp.text               = message;
            tmp.fontSize           = 14;
            tmp.color              = Color.white;
            tmp.alignment          = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            if (font != null) tmp.font = font;
            Destroy(warn, 2.5f);
        }

        void OnCloseClicked()
        {
            Hide();
            OnClose.Invoke();
        }
    }
}
