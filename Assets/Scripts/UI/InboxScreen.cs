using System.Collections.Generic;
using InboxZero.Core;
using UnityEngine;
using UnityEngine.Events;

namespace InboxZero.UI
{
    /// Gmail-style inbox email list — shows the current level's encounters as clickable rows.
    /// Follows the same SPA pattern as AllMailScreen and DeckBuilderScreen:
    /// FloorMapScreen calls ShowInContent() to swap this page into the ContentArea.
    public class InboxScreen : MonoBehaviour
    {
        public static InboxScreen Instance { get; private set; }

        [Header("Prefabs")]
        public GameObject pagePrefab;      // InboxPage.prefab
        public GameObject emailRowPrefab;  // EmailRow.prefab

        public UnityEvent<RoomOption> OnCombatSelected   = new UnityEvent<RoomOption>();
        public UnityEvent<RoomOption> OnRestStopSelected = new UnityEvent<RoomOption>();

        GameObject _page;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// Show the email list inside an existing content area (SPA style).
        public void ShowInContent(RectTransform contentArea, List<RoomOption> options)
        {
            Hide();
            if (pagePrefab == null || contentArea == null) return;

            _page = Instantiate(pagePrefab, contentArea, false);
            var view = _page.GetComponent<InboxPageView>();
            if (view?.emailListRoot != null)
                BuildRows(view.emailListRoot, options);
        }

        public void Hide()
        {
            if (_page != null) Destroy(_page);
            _page = null;
        }

        // ── Row building ──────────────────────────────────────────────────────

        void BuildRows(RectTransform parent, List<RoomOption> options)
        {
            if (emailRowPrefab == null)
            {
                Debug.LogError("[InboxScreen] emailRowPrefab not assigned.");
                return;
            }

            float rowH    = emailRowPrefab.GetComponent<RectTransform>().sizeDelta.y;
            var   gm      = GameManager.Instance;
            int   floorDay = 14 - (4 - gm.CurrentFloor) * 2 - gm.CurrentRoom;

            for (int i = 0; i < options.Count; i++)
            {
                var rowGo = Instantiate(emailRowPrefab, parent, false);
                rowGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -i * rowH);

                var rowView = rowGo.GetComponent<EmailRowView>();
                if (rowView == null) continue;

                rowView.Bind(options[i], floorDay);

                var captured = options[i];
                rowView.button?.onClick.AddListener(() => OnRowClicked(captured));
            }

            parent.sizeDelta = new Vector2(0, options.Count * rowH);
        }

        void OnRowClicked(RoomOption opt)
        {
            if (opt.type == RoomType.Combat)
                OnCombatSelected.Invoke(opt);
            else
                OnRestStopSelected.Invoke(opt);
        }
    }
}
