---
name: inbox-zero-ui-panels
description: Workflow for building or modifying INBOX//ZERO's InboxLayout SPA-style UI pages (the run-navigation hub with sidebar tabs that swap content). Use when adding a new sidebar page, modifying DraftsPage/AllMailPanel/InboxPage, or wiring a new Screen class to display content in InboxLayout's ContentArea.
---

# InboxLayout UI Panel Workflow

`InboxLayout.prefab` is a single-page-app shell: persistent chrome (top bar + sidebar) defined by `LayoutView` (`Assets/Scripts/UI/LayoutView.cs`), with a `contentArea` RectTransform that gets a different *page prefab* instantiated into it per sidebar selection.

## Current pages (as of this writing)
| Page prefab | View component | Shown by |
|---|---|---|
| `DraftsPage.prefab` (deck builder) | `DraftsPageView` | `DeckBuilderScreen.ShowInContent` |
| `AllMailPanel.prefab` (card compendium) | `AllMailPanelView` | `AllMailScreen` |
| `InboxPage.prefab` (email/encounter list) | `InboxPageView` (exposes `emailListRoot`) | `FloorMapScreen` (populates dynamic email rows) |

Each page prefab has a builder editor script under `Assets/Editor/` (`DraftsPageBuilder.cs`, `AllMailPrefabBuilder.cs`) that can construct it from scratch via an `InboxZero → Rebuild … Prefab` menu item, and `InboxLayoutBuilder.cs` for the shell itself.

## Pattern to follow for a new page
1. Create the page prefab with a `RectTransform` root sized to fill `ContentArea`.
2. Add a `<PageName>View : MonoBehaviour` script in `Assets/Scripts/UI/` exposing only the serialized child references the runtime logic needs (mirror `LayoutView`/`InboxPageView` style — comment each field with what populates it). Scripts read these refs directly; do not use `GetComponentInChildren` at runtime.
3. Add a `<PageName>Screen` (or extend an existing screen) with a `ShowInContent(RectTransform contentArea)`-style entry point that instantiates the page prefab into `LayoutView.contentArea` and wires the view's refs.
4. If the page needs a builder for repeatable prefab construction, add `Assets/Editor/<PageName>Builder.cs` with an `[MenuItem("InboxZero/Rebuild <PageName> Prefab")]`. **Only run it to create the prefab from scratch** — running it again after the prefab has been hand-customized in the inspector wipes those customizations. Prefer manual prefab edits once it exists.
5. Background/frame on the page or any child panel must reuse the shared panel-frame sprite (`Assets/Sprites/Light/Panel.png`, GUID `67a2062369ab5ca429cb2612549aa3da`, fileID `21300000`) on its `Image` component — see `inbox-zero-conventions` for the full rule. Do not introduce a new panel style.
6. All text uses BetterPixels (sidebar nav item labels use Micro5 instead) — see `inbox-zero-conventions` for GUIDs and the font-size-14 floor.
7. Add the new page's nav entry to `LayoutView.navItems` and route it to your Screen's show method from wherever sidebar nav clicks are dispatched (check existing nav wiring before adding new dispatch logic).

## Verifying
Ctrl+R → check Console for compile errors → Play → click the new sidebar item → confirm the page swaps into `ContentArea` without leaving the previous page's GameObjects behind (each Screen's show method should deactivate/destroy the prior page before instantiating its own).
