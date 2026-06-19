---
name: inbox-zero-scene-yaml
description: Reference for hand-editing Unity .unity scene and .prefab YAML directly (not through the Editor UI) in the INBOX//ZERO project — exact GUIDs/fileIDs for TMP text, fonts, and the panel-frame sprite, plus the gotchas that produce "missing script" or null-reference errors. Use when writing or editing scene/prefab YAML by hand.
---

# Manual Scene/Prefab YAML Reference

Use this when constructing or editing `.unity` / `.prefab` files directly as text rather than through Unity's inspector — the GUIDs below are exact and a transposed digit produces a "missing script" error or a silent null reference.

## TMP text components
Every visible UI label must be a real `TextMeshProUGUI`, not a placeholder GameObject.

```yaml
m_Script: {fileID: 11500000, guid: f4688fdb7df04437aeb418b961361dc5, type: 3}  # TextMeshProUGUI (com.unity.ugui)
```

Font asset reference (default — BetterPixels):
```yaml
m_fontAsset: {fileID: 11400000, guid: af581fb1ba59971408d2278b6bffa1d5, type: 2}
m_fontSharedMaterial: {fileID: -7018534552672840379, guid: af581fb1ba59971408d2278b6bffa1d5, type: 2}
```

Sidebar nav labels only — Micro5 instead:
```yaml
m_fontAsset: {fileID: 11400000, guid: 968711493fda13b4789c59de06321a30, type: 2}
m_fontSharedMaterial: {fileID: 2536256334939891893, guid: 968711493fda13b4789c59de06321a30, type: 2}
```

Both fonts are **bitmap** (RASTER_HINTED) TMP assets. If a shader reference is needed explicitly, use the TMP Bitmap shader (`guid: 128e987d567d4e2c824d754223b3f3b0`) — never the TMP_SDF shader, text will render wrong/invisible.

Font size fields — never below 14:
```yaml
m_fontSize: 14
m_fontSizeBase: 14
m_fontSizeMin: 14
```

`m_text` content rule: plain ASCII only. No emoji, no `\uXXXX` surrogate pairs — Unity's YAML parser rejects them outright (e.g. write `SHIELD 0`, not a shield emoji + `0`).

## Shared panel-frame sprite
Any panel/container background `Image` component should reference:
```yaml
m_Sprite: {fileID: 21300000, guid: 67a2062369ab5ca429cb2612549aa3da, type: 3}  # Assets/Sprites/Light/Panel.png
```
This is the same sprite used by `PlayerPanel`'s own `Image` and `EnemyPanel`'s frame child in `CombatScene.unity`. Don't reference a different Graybox2D rectangle for new panels — consistency is a project rule, not a style preference.

## GUID-regeneration gotcha
When you hand-write a new `.asset` or `.meta` file (rather than letting Unity create it), Unity may assign a **different** GUID on first import than whatever you put in the `.meta`. After Ctrl+R:
1. Re-open the `.meta` file for anything you just wrote.
2. Grep the scene/prefab YAML for the GUID you originally assigned and replace it with the actual one if it changed.
3. Symptoms you're chasing if you skip this: null entries in Inspector list fields, or a `NullReferenceException` thrown from code that iterates that list at runtime.

## Sanity checklist before saving hand-edited YAML
- [ ] Every `TextMeshProUGUI` has both `m_fontAsset` and `m_fontSharedMaterial` set to the *same* font's GUID (mixing BetterPixels asset with Micro5 material, etc. silently breaks rendering).
- [ ] No `m_fontSize*` field below 14.
- [ ] No emoji / surrogate-pair escapes in any `m_text`.
- [ ] Panel-style `Image` components point at the shared frame sprite GUID, not a placeholder.
- [ ] Ctrl+R afterward, then check Console before assuming the YAML was valid.
