---
name: unity-best-practices
description: Compliance checklist sourced directly from Unity's official Manual (docs.unity3d.com) — programming best practices, Canvas/UI rules, and reserved/special folder behavior — checked against this project's actual code and folder layout. Use when planning or doing a refactor, reviewing scripts for code smells, or making architectural/folder decisions in this Unity project.
---

# Unity Best Practices — Official Manual Compliance

Every rule below is sourced verbatim or near-verbatim from Unity's own Manual (docs.unity3d.com), not from blogs, community style guides, or my own synthesis — the user explicitly chose the official manual as the standard to comply with. Each rule has a **Status** for this project so refactor work has a concrete compliance checklist to work from, not opinion. This skill does not perform any refactor itself.

For project-specific conventions that already override general practice (fonts, GUIDs, panel framing, screen ownership), see the `inbox-zero-conventions` skill — don't contradict those.

## A. Programming best practices
Source: [Unity Manual — Unity programming best practices](https://docs.unity3d.com/6000.3/Documentation/Manual/programming-best-practices.html)

| Rule (from the manual) | Status in this project |
|---|---|
| Use `obj == null` for destroyed Unity objects, not `ReferenceEquals` (Unity's overloaded null check catches "fake null") | Not yet audited — check during refactor pass, especially anywhere caching enemy/card references across turns. |
| Use `Object.Destroy` at runtime; `DestroyImmediate` is Editor-only | `CardEffectResolver`/`HandDisplay` discard flows should be checked for any `DestroyImmediate` calls outside Editor scripts. |
| Never use C# finalizers in runtime code | Not observed in current scripts; no action needed unless one is found. |
| Avoid per-frame allocations — cache/reuse lists, avoid LINQ in hot paths, avoid repeated string concatenation | `HandDisplay.RefreshHand()` instantiates GameObjects every turn (per CLAUDE.md) — worth checking whether the surrounding code allocates new collections each call rather than reusing buffers. |
| Perform `GetComponent` in `Awake`/`Start`, never in `Update` | Not yet audited — grep candidate for refactor: any `GetComponent` calls inside `Update()` methods across `Assets/Scripts/`. |
| Cache `WaitForSeconds`/yield instructions in coroutines instead of allocating new ones per call | Relevant if any combat/animation timing uses coroutines with `WaitForSeconds` inside a loop. |
| Minimize active `Update()` functions — many MonoBehaviours each running their own `Update` adds cumulative per-instance overhead; consider a centralized update manager | `AudioManager` and `DeckWidget` are documented singletons (CLAUDE.md) — check whether either runs an `Update()` it doesn't need, and avoid adding more independent `Update()` loops for new systems where one shared driver would do. |
| UnityEngine APIs are main-thread only — never reference GameObjects/Transforms/Components from background threads | Not yet audited. |
| Never call `Task.Result`/`Task.Wait()` on the main thread (deadlock risk); prefer Unity's `Awaitable` over `Task` | Not yet audited — relevant if any future async loading/networking code is added. |
| Use Assembly Definitions to isolate Editor-only and platform-specific code; use `#if UNITY_EDITOR` for conditional compilation | Project has an `Assets/Editor/` folder for editor scripts (correct placement per Special Folders, section C below) but no `.asmdef` files currently split out runtime vs. editor code — worth considering once the script count grows. |
| Use Project Auditor and the Profiler to identify performance issues; consider custom Roslyn analyzers for project-specific standards | Not currently used in this project — a candidate tool to introduce before a performance-focused refactor pass. |

## B. Canvas / UI rules
Source: [Unity Manual — Canvas (uGUI)](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/UICanvas.html)

| Rule (from the manual) | Status in this project |
|---|---|
| All UI elements must be children of a GameObject with a Canvas component | Confirmed by existing CLAUDE.md conventions (Canvas render mode is documented for `CombatScene`); should hold for `InboxLayout` and other UI scenes too — verify during refactor. |
| To reorder which elements draw on top, reorder them in the Hierarchy (or use `SetAsFirstSibling`/`SetAsLastSibling`/`SetSiblingIndex` programmatically) — this is the documented way to control draw order, not z-position tricks | Worth checking `HandDisplay`'s fan layout and any card-overlap logic uses sibling order rather than manual z/sorting hacks. |
| Render mode choice matters: **Screen Space - Overlay** auto-resizes to screen and ignores camera; **Screen Space - Camera** is for when camera settings (e.g. perspective, distance) should affect UI appearance; **World Space** is for UI that's literally part of the 3D/2D world | `CombatScene`'s Canvas is documented as **Screen Space - Camera** using the same camera as the Pixel Perfect Camera (per project memory) — this is the correct mode for a Pixel Perfect Camera setup, since Overlay mode would bypass the pixel-perfect upscaling. |
| Keep **Additional Shader Channels** limited to only the vertex attributes actually needed — extra channels add unnecessary overhead | Not yet audited on the project's UI Canvas components. |
| If using a custom UI shader with gamma color space conversion enabled, you must handle that conversion manually | Only relevant if/when a custom UI shader is introduced; not applicable to current TMP/Image-based UI. |

## C. Special / reserved folders
Source: [Unity Manual — Special folder names](https://docs.unity3d.com/6000.3/Documentation/Manual/SpecialFolders.html)

| Folder | Manual's rule | Status in this project |
|---|---|---|
| `Editor` | Editor-only scripts, stripped from Player builds; can exist anywhere in `Assets`, unlimited instances | `Assets/Editor/` exists and correctly contains only editor tooling (`CardBatchCreator.cs`, `InboxLayoutBuilder.cs`, `AllMailPrefabBuilder.cs`, etc., per CLAUDE.md's documented builder scripts) — **compliant**. |
| `Editor Default Resources` | Assets loaded on-demand via `EditorGUIUtility.Load`; must be at `Assets` root, only one allowed | Not present in this project — no action needed unless this loading pattern is introduced. |
| `Gizmos` | Scene-view icon images for `Gizmos.DrawIcon`; one folder only, at `Assets` root | Not present — no action needed. |
| `Resources` | Runtime-loadable via `Resources.Load` without scene references; can exist anywhere, unlimited instances; **increases Player build size** for everything inside | `Assets/Resources/` exists at root and is currently **empty** — compliant, and worth keeping it that way; anything added here later should be deliberate (build-size cost), not a dumping ground. |
| `Plugins` | Third-party plugins / native code libraries; platform-specific placement rules apply | `Assets/Plugins/` exists — should be audited to confirm only genuine third-party/native plugin content lives there, not project scripts. |
| `StreamingAssets` | Files kept in original format for runtime streaming; one folder only, at `Assets` root | Not present — no action needed unless raw streamed files are introduced. |
| Hidden assets | Files/folders starting with `.`, ending with `~`, named `cvs`, or with `.tmp` extension are auto-ignored by Unity's importer | No action needed, but useful to know if a folder mysteriously isn't importing. |

**Observation outside the special-folders list:** `Assets/_Recovery/` currently contains two autosave/crash-recovery scene files (`0.unity`, `0 (1).unity`). This isn't a Unity-reserved folder — it's leftover Editor crash-recovery output. Not a manual violation, but worth a deliberate decision (keep, gitignore, or delete) during the refactor rather than leaving it as untracked clutter. `TextMesh Pro/` (space in the name) is also present at root — this is the stock folder name created by Unity's own TMP Essentials importer, not a violation to "fix," just an expected exception to general no-spaces naming advice.

## How to use this during the refactor

When reviewing a script or folder against this skill: find the matching row, check the **Status** column, and either confirm compliance or flag the gap as a discrete refactor task — don't make broader stylistic changes the manual doesn't actually call for. If a question comes up that isn't covered by one of the three source pages above, fetch the relevant Unity Manual page directly rather than relying on community blogs, per the user's stated preference for the official manual as the source of truth.
