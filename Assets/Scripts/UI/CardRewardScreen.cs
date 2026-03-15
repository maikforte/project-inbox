using System.Collections.Generic;
using InboxZero.Core;
using InboxZero.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace InboxZero.UI
{
    public class CardRewardScreen : MonoBehaviour
    {
        public static CardRewardScreen Instance { get; private set; }

        [Header("Card Pool")]
        [Tooltip("All reward cards. Filtered by rarity at runtime based on current floor.")]
        public List<CardData> rewardPool = new List<CardData>();

        [Header("Sprites")]
        public Sprite cardBackground;

        [Header("Font")]
        public TMP_FontAsset cardFont;

        // Fired after the player picks or skips — wire to room selection (TASK-11).
        public UnityEvent OnComplete = new UnityEvent();

        const int CardCount    = 3;
        const float CardWidth  = 110f;
        const float CardHeight = 150f;
        const float CardSpacing = 20f;
        const float TypeBarHeight = 10f;
        const float Padding = 5f;

        static readonly Color AttackColor  = new Color(0.85f, 0.18f, 0.18f);
        static readonly Color DefendColor  = new Color(0.18f, 0.75f, 0.25f);
        static readonly Color SpecialColor = new Color(0.55f, 0.18f, 0.80f);

        Canvas _canvas;
        GameObject _panel;
        readonly List<GameObject> _cardObjects = new List<GameObject>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _canvas = FindObjectOfType<Canvas>();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Show(int floor)
        {
            var options = DrawOptions(floor);
            BuildPanel(options);
        }

        public void Hide()
        {
            if (_panel != null) Destroy(_panel);
            _panel = null;
            _cardObjects.Clear();
        }

        // ── Pool filtering ────────────────────────────────────────────────────

        List<CardData> DrawOptions(int floor)
        {
            var allowed = AllowedRarities(floor);
            var pool = new List<CardData>();
            foreach (var card in rewardPool)
                if (allowed.Contains(card.rarity))
                    pool.Add(card);

            // Shuffle and pick up to CardCount
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            var result = new List<CardData>();
            for (int i = 0; i < Mathf.Min(CardCount, pool.Count); i++)
                result.Add(pool[i]);
            return result;
        }

        static List<CardRarity> AllowedRarities(int floor) => floor switch
        {
            1 => new List<CardRarity> { CardRarity.Common },
            2 => new List<CardRarity> { CardRarity.Common, CardRarity.Uncommon },
            3 => new List<CardRarity> { CardRarity.Uncommon, CardRarity.Rare },
            _ => new List<CardRarity> { CardRarity.Rare },
        };

        // ── Panel construction ────────────────────────────────────────────────

        void BuildPanel(List<CardData> options)
        {
            // Find the Canvas in the scene to parent to
            var canvas = _canvas;
            if (canvas == null) { Debug.LogError("[CardRewardScreen] No Canvas found."); return; }

            _panel = new GameObject("CardRewardPanel", typeof(RectTransform));
            _panel.layer = 5;
            var panelRt = _panel.GetComponent<RectTransform>();
            panelRt.SetParent(canvas.transform, false);
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            // Dark overlay
            var overlay = _panel.AddComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.85f);

            // Title
            MakeText("Title", panelRt,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -30), new Vector2(300, 20), "CHOOSE A CARD", 14);

            // Card row
            float totalWidth = CardCount * CardWidth + (CardCount - 1) * CardSpacing;
            float startX = -totalWidth / 2f + CardWidth / 2f;

            for (int i = 0; i < options.Count; i++)
            {
                var cardGo = BuildRewardCard(panelRt, options[i]);
                var cardRt = cardGo.GetComponent<RectTransform>();
                cardRt.anchoredPosition = new Vector2(startX + i * (CardWidth + CardSpacing), 0);
                _cardObjects.Add(cardGo);
            }

            // Skip button
            var skipGo = new GameObject("SkipButton", typeof(RectTransform));
            skipGo.layer = 5;
            var skipRt = skipGo.GetComponent<RectTransform>();
            skipRt.SetParent(panelRt, false);
            skipRt.anchorMin = new Vector2(0.5f, 0);
            skipRt.anchorMax = new Vector2(0.5f, 0);
            skipRt.pivot     = new Vector2(0.5f, 0);
            skipRt.anchoredPosition = new Vector2(0, 20);
            skipRt.sizeDelta = new Vector2(80, 20);
            skipGo.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.35f);
            var skipBtn = skipGo.AddComponent<Button>();
            skipBtn.onClick.AddListener(OnSkip);
            var skipLabelGo = MakeText("Label", skipRt,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, "SKIP", 14);
            skipLabelGo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        }

        GameObject BuildRewardCard(RectTransform parent, CardData data)
        {
            var (canAdd, reason) = DeckCompositionChecker.CanAdd(data);

            var go = new GameObject(data.cardName, typeof(RectTransform));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(CardWidth, CardHeight);

            var bgImg = go.AddComponent<Image>();
            if (cardBackground != null) bgImg.sprite = cardBackground;
            bgImg.color = canAdd
                ? new Color(0.18f, 0.18f, 0.28f)
                : new Color(0.12f, 0.12f, 0.12f);   // dim bg when locked
            go.AddComponent<GraphicRaycaster>();

            // Type bar
            var barGo = new GameObject("TypeBar", typeof(RectTransform));
            barGo.layer = 5;
            var barRt = barGo.GetComponent<RectTransform>();
            barRt.SetParent(rt, false);
            barRt.anchorMin = new Vector2(0, 1);
            barRt.anchorMax = new Vector2(1, 1);
            barRt.pivot = new Vector2(0.5f, 1f);
            barRt.sizeDelta = new Vector2(0, TypeBarHeight);
            barRt.anchoredPosition = Vector2.zero;
            var typeBarColor = TypeColor(data.cardType);
            barGo.AddComponent<Image>().color = canAdd
                ? typeBarColor
                : new Color(typeBarColor.r * 0.4f, typeBarColor.g * 0.4f, typeBarColor.b * 0.4f);

            // Cost
            MakeText("Cost", rt,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(Padding, -TypeBarHeight - 1), new Vector2(18, 14), data.apCost.ToString(), 14);

            // Rarity pip (top-right)
            MakeText("Rarity", rt,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-Padding, -TypeBarHeight - 1), new Vector2(40, 10),
                data.rarity.ToString().ToUpper(), 14);

            // Name
            var nameGo = MakeText("Name", rt,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f),
                new Vector2(0, -TypeBarHeight - Padding), new Vector2(0, 18),
                data.cardName.ToUpper(), 14);
            nameGo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            // Effect text — fixed-size rect to avoid TMP word-wrap recursion on first frame
            var effectGo = MakeText("Effect", rt,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(Padding, -(TypeBarHeight + 26)),
                new Vector2(CardWidth - Padding * 2, CardHeight - TypeBarHeight - 36),
                data.effectDescription, 14);
            var effectTmp = effectGo.GetComponent<TextMeshProUGUI>();
            effectTmp.alignment          = TextAlignmentOptions.TopLeft;
            effectTmp.enableWordWrapping = true;

            if (!canAdd)
            {
                // Dark overlay to visually lock the card
                var overlayGo = new GameObject("LockedOverlay", typeof(RectTransform));
                overlayGo.layer = 5;
                var overlayRt = overlayGo.GetComponent<RectTransform>();
                overlayRt.SetParent(rt, false);
                overlayRt.anchorMin = Vector2.zero;
                overlayRt.anchorMax = Vector2.one;
                overlayRt.offsetMin = Vector2.zero;
                overlayRt.offsetMax = Vector2.zero;
                overlayGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

                // Reason label centred on the card
                var reasonGo = MakeText("Reason", rt,
                    new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(0, 28), reason, 14);
                var reasonTmp = reasonGo.GetComponent<TextMeshProUGUI>();
                reasonTmp.alignment         = TextAlignmentOptions.Center;
                reasonTmp.enableWordWrapping = true;
                reasonTmp.color             = new Color(1f, 0.35f, 0.35f);
            }

            // Click handler — only active when card is addable
            var btn = go.AddComponent<Button>();
            btn.interactable = canAdd;
            if (canAdd)
            {
                var captured = data;
                btn.onClick.AddListener(() => OnPick(captured));
            }

            return go;
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        void OnPick(CardData card)
        {
            GameManager.Instance.DrawPile.Add(card);
            Hide();
            OnComplete.Invoke();
        }

        void OnSkip()
        {
            Hide();
            OnComplete.Invoke();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        static Color TypeColor(CardType type) => type switch
        {
            CardType.Attack  => AttackColor,
            CardType.Defend  => DefendColor,
            _                => SpecialColor,
        };

        GameObject MakeText(string name, RectTransform parent,
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
            if (cardFont != null) tmp.font = cardFont;

            return go;
        }
    }
}
