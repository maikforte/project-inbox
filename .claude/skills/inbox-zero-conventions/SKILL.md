---
name: inbox-zero-conventions
description: Reference for INBOX//ZERO (Unity 6, URP 2D, 640x360 pixel art roguelite) project-wide conventions — fonts, GUIDs, sprite placeholders, panel framing, screen ownership, audio, and turn structure. Use whenever touching scenes, prefabs, UI, or scripts in this repo and you need a fact (a GUID, a fileID, a rule) rather than a how-to.
---

# INBOX//ZERO Conventions

Single source of facts for this project. For step-by-step workflows see the sibling skills `inbox-zero-card-creation`, `inbox-zero-ui-panels`, and `inbox-zero-scene-yaml`.

## Fonts
- **BetterPixels** is the default for ALL UI text except sidebar nav labels.
- **Micro5** is used only for `NavItemView` sidebar labels (e.g. `Assets/Prefabs/UI/NavItem.prefab`).
- Both are TMP **bitmap** assets (RASTER_HINTED render mode) — never assign the TMP_SDF shader to them.
- GUIDs:
  | Asset | GUID | Material fileID |
  |---|---|---|
  | BetterPixels font asset | `af581fb1ba59971408d2278b6bffa1d5` | `-7018534552672840379` |
  | Micro5 font asset | `968711493fda13b4789c59de06321a30` | `2536256334939891893` |
  | `TextMeshProUGUI` script (com.unity.ugui) | `f4688fdb7df04437aeb418b961361dc5` | — |
  | TMP Bitmap shader | `128e987d567d4e2c824d754223b3f3b0` | — |
- Font size floor is **14** everywhere: `m_fontSize`, `m_fontSizeBase`, `m_fontSizeMin`, and any programmatic `fontSize` assignment.
- Never put emoji or `\uXXXX` surrogate-pair escapes in `m_text` — Unity's YAML parser rejects them. Use ASCII (`SHIELD 0`, not `[shield] 0`).

## Placeholder art
- Use `Assets/Graybox2D/` sprites for in-game visuals until final art lands. Named by pixel size (`64x128.png`, etc.) — pick the closest size, Unity stretches to fill.
- Exception: the shared panel-frame sprite is **not** in Graybox2D — it's `Assets/Sprites/Light/Panel.png` (GUID `67a2062369ab5ca429cb2612549aa3da`, fileID `21300000`, type 3/Sprite). Every panel/container background (PlayerPanel's own Image, EnemyPanel's frame child, etc.) must reference this exact sprite. Apply it by swapping the `m_Sprite` field on the `Image` component — no layout changes needed. Don't invent a new panel style for new screens.

## Screen ownership
- **Main Menu** = pre-run hub. Everything that persists across runs lives here: card unlocks, card-pool toggles, character select, future meta-progression.
- **InboxLayout** = shown only during an active run. It's the run's navigation hub (no visible map graph). Tabs are path selectors — Inbox is the main combat path, other tabs (Spam, Sent, Promotions, ...) are optional side paths. Any row in any tab can resolve to any encounter type.
- Nothing in InboxLayout should mutate meta-state; nothing in Main Menu should depend on an active run.

## Audio
`AudioManager.Instance` exposes: `PlayCardPlay()`, `PlayCardDraw()`, `PlayDamageHit()`, `PlayShieldBlock()`, `PlayGainShield()`, `PlayRestoreHP()`, `PlayStatusApplied()`, `PlayEnemyDeath()`, `PlayGameOver()`. Always call through the null-safe pattern: `AudioManager.Instance?.Play…()`.

## Turn structure (for anything touching combat logic)
```
START OF TURN → reshuffle discard into draw if draw pile ≤ 2 and discard non-empty → draw 3 → gain 3 AP
              → start-of-turn relics (Cold Coffee) → start-of-turn statuses (Awaiting Reply AP reduction)
YOUR TURN     → play cards, spend AP, played cards go to discard immediately
END OF TURN   → discard unplayed hand → shield resets to 0
ENEMY TURN    → enemy attacks (shield absorbs first) → enemy regen → player status ticks (Guilt, etc.)
```
Hand does not persist between turns. Deck size cap is 15 cards; Uncommon max 4 copies, Rare max 2, Legendary max 1, Starter/Common unlimited.

## Misc gotchas
- Hand-written `.asset`/`.meta` files may get a new GUID on first Unity import — always re-read the `.meta` after Ctrl+R and fix stale scene/prefab references (symptom: null entries in inspector lists, NullReferenceException from list iteration).
- `Card.prefab`'s RectTransform `sizeDelta` is the source of truth for card size — never hardcode width/height in `HandDisplay`; it reads `cardW` from the first card's `rect.width`.
- Never run an `InboxZero → Rebuild … Prefab` MenuItem builder on a prefab that has already been hand-customized in the inspector — it wipes those customizations. Builders are for creating a prefab from scratch only.
