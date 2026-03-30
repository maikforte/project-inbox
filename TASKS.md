# INBOX//ZERO — Task List

Tasks are ordered by priority. Complete the game loop first before content and polish.
Each task is sized to be a single prompt session.

**Status legend:** `[ ]` todo · `[x]` done · `[~]` in progress · `[!]` superseded

---

## MILESTONE 1 — Core Combat Loop ✓
- [x] TASK-01 · CardData, EnemyData, RelicData ScriptableObjects
- [x] TASK-02 · GameManager singleton & run state
- [x] TASK-03 · Combat scene UI layout (enemy panel, player panel, hand area, end turn button)
- [x] TASK-04 · DeckManager (shuffle, draw, discard, reshuffle)
- [x] TASK-05 · TurnManager (player/enemy turn flow, AP system)
- [x] TASK-06 · Card UI & playing cards (click to play, hover preview)
- [x] TASK-07 · Basic card effects (DealDamage, GainShield, DrawCards, GainAP)
- [x] TASK-08 · EnemyController (stats display, attack, regen, death)
- [x] TASK-09 · Combat win/lose states

## MILESTONE 2 — Run Structure ✓
- [x] TASK-10 · Card reward screen
- [x] TASK-11 · Floor map & room selection
- [x] TASK-12 · Rest stop room
- [x] TASK-13 · Floor transition & relic award
- [x] TASK-13B · Scene wiring & first playtest

## MILESTONE 3 — Status Effects & Starter Content ✓
- [x] TASK-14 · Status effects (Unread, Guilt, Awaiting Reply) + UI chips
- [x] TASK-15 · All 9 starter deck cards
- [x] TASK-16 · Floor 1 enemies (Newsletter Flood, Calendar Invite)
- [x] TASK-17 · Floor 2 enemies + Uncommon cards
- [x] TASK-18 · Floor 3 enemies + Rare cards
- [x] TASK-19 · Floor 4 boss (The Thread That Never Ends)

## MILESTONE 4 — Relics ✓
- [x] TASK-20 · RelicManager + Paperclip relic
- [x] TASK-21 · All 5 acquirable relics

## MILESTONE 5 — Game Screens ✓
- [x] TASK-22 · Main menu (Start Run)
- [x] TASK-23 · Game Over screen
- [x] TASK-24 · Victory screen
- [x] TASK-29 · Main menu expanded (New Game, Options, Exit)
- [x] TASK-30 · Inbox as hub (Gmail-style, all encounters per level)

## MILESTONE 5B — Card Mechanics Redesign ✓
- [x] TASK-31 · Persistent hand & draw-1 system
- [x] TASK-32 · Max hand size (7) & overflow discard
- [x] TASK-33 · Deck composition limits (Uncommon ≤4, Rare ≤2, max 15 cards)
- [!] TASK-34 · Chance-based card drop system → **superseded by TASK-38**
- [!] TASK-35 · Deck builder at rest stops (collection pool) → **superseded by TASK-38**
- [!] TASK-36 · DRAFTS inbox sidebar (collection pool UI) → **superseded by TASK-38**
- [x] TASK-37 · ALL MAIL compendium screen

## MILESTONE 6 — Polish ✓
- [x] TASK-25 · Card animations & visual feedback
- [x] TASK-26 · SFX (card play, damage, shield, death, draw, heal, status, game over)

## MILESTONE 7 — Visual Polish ✓
- [x] TASK-27 · Status effect label chips
- [x] TASK-28 · Consistent 9-sliced sprite across all panels

---

## MILESTONE 8 — Roguelite Reward System

> Replaces the chance-based drop + collection pool model. Every win guarantees a 3-card pick. Cards enter the deck immediately. A meta-unlock system gates which cards can drop.

> **UI convention for all new screens in this milestone and beyond:** All panel/container backgrounds must use the same 9-sliced frame sprite as `PlayerPanel` (Image on the GO itself) and `EnemyPanel > Frame`. Do not introduce new panel styles. Swap the sprite reference on the `Image` component — no layout changes needed.

- [x] **TASK-38 · Guaranteed 3-Card Reward**
  Rework the post-combat reward flow:
  - Every enemy defeat shows exactly 3 cards — pick 1, it enters the active deck immediately
  - Card rarity offered based on enemy tier: Common → Common only, Uncommon → Common/Uncommon, Rare → Uncommon/Rare, Boss → Rare guaranteed
  - Remove `GameManager.CardCollection` (collection pool no longer exists)
  - Remove deck builder screen from rest stops and level transitions (no longer needed)
  - Deck composition limits from TASK-33 still apply — grey out cards that would violate them

- [ ] **TASK-39 · Card Unlock System**
  Cards are locked by default and must be unlocked before they can appear as rewards. Unlocks persist across runs (saved to disk).

  Implement `UnlockManager` singleton with save/load:

  **Rarity unlocks** — triggered automatically by enemy tier, no floor references:
  - Starter + Common: always unlocked
  - Uncommon: unlock on first defeat of any Uncommon-tier enemy
  - Rare: unlock on first defeat of any Rare-tier enemy
  - Legendary: unlock on first Boss defeat

  **Enemy signature unlocks** — first-kill of specific enemy unlocks its card permanently:
  - Reply-All Demon → Forward Bomb
  - Auto-CC Manager → CC the CEO
  - Out-of-Office Loop → Vacation Autoresponder
  - Passive-Aggressive Karen → Report as Spam
  - The Thread That Never Ends → Start New Thread

  **Character-tier unlocks** — behavior-based, tracked within a run:
  - Win a run using only Starter + Common cards → Intern cards
  - Play 10+ cards in a single turn → Dev cards
  - Win a fight without playing any Attack card → Lawyer cards
  - End a turn with 0 AP three turns in a row → Manager cards
  - Win a run after losing to the boss in a prior run → Ghost cards

- [ ] **TASK-40 · All Mail Rework — Unlock State & Card Pool Toggle**
  Rework the existing `AllMail` screen to serve as both the card compendium and the card pool configuration. No separate main menu screen needed.

  **Unlock state display:**
  - Unlocked cards show normally (name, type, rarity, effect, AP cost)
  - Locked cards are greyed out with "LOCKED" replacing the effect text and no toggle available
  - Header shows "N / TOTAL unlocked"

  **Card pool toggle (unlocked cards only):**
  - Each unlocked card row has an enable/disable toggle
  - Disabled cards are visually dimmed but still readable — they won't appear as rewards
  - Starter cards have no toggle — always enabled
  - Enforce minimums: at least 4 Common, 3 Uncommon, 1 Rare must remain enabled; warn and block if toggling off would violate this
  - Enemy signature drops cannot be disabled (they are tied to that enemy's kill, not the general pool)
  - Toggle state persists across runs (saved alongside unlock data in `UnlockManager`)

  **Access:** All Mail remains accessible from the inbox sidebar as before. Also accessible from the main menu ("CONFIGURE" button or similar).

---

## MILESTONE 9 — Content Expansion

> Designed to be content-agnostic. New enemies at any tier plug into the unlock system automatically.

- [ ] **TASK-41 · Legendary Cards**
  Implement Legendary rarity in the reward system (max 1 copy in deck, capped at 1 per specific card).
  Create ScriptableObjects for all Legendary cards. Wire into `AllCardsRegistry`.
  Update deck composition limit display in reward screen.

- [ ] **TASK-42 · Character Card Sets**
  Create ScriptableObjects for all character-tagged cards (Intern, Manager, Lawyer, Dev, Ghost).
  These are locked until their respective unlock condition is met (TASK-39).
  Wire into `AllCardsRegistry`.

- [ ] **TASK-43 · Enemy Intent Deck Expansion**
  Audit all existing enemies — ensure every enemy has a full intent deck (not relying on legacy flat-damage fallback).
  Assign intent decks to any enemy still using `damagePerTurn` only.

---

## MILESTONE 10 — Inbox Priority System

> Transforms the Inbox from a static checklist into a dynamic decision space. The player chooses encounter order, but inaction has a cost — emails escalate the longer they are ignored.
>
> **Design principles:** Clarity over complexity. Escalation must always be visible and predictable. No choice should be strictly correct in all situations.

- [x] **TASK-44 · Free-Choice Row Selection**
  Remove the implied top-to-bottom ordering constraint. All inbox rows are selectable in any order.

  **What to change:**
  - `InboxScreen.BuildRows()` — currently all rows already receive click listeners with no lock. Confirm this is correct and intentional. Add a short visual affordance (e.g. cursor change or subtle highlight on hover) so the player understands all rows are live.
  - `EmailRowView` — all rows display identically whether top or bottom. No greying out, no lock icons.
  - `CombatSetup` — no ordering logic currently exists here; no change needed.
  - `CLAUDE.md` + `GAMEPLAY_FLOW.md` — update "rows resolve in order" language.

  **Effort:** Minimal. Primarily a design confirmation + documentation update.

---

- [x] **TASK-45 · Escalation Data Model**
  Track per-room escalation state. After every completed encounter (combat or rest stop), all remaining inbox rooms escalate by 1.

  **What to change:**
  - `RoomOption` (struct in `FloorMapManager.cs`) — **do not add state here**. It is a value type and cannot hold mutable state cleanly.
  - `CombatSetup` — add `Dictionary<int, int> _escalation` (key = `_inbox` list index → value = escalation level). Rebuild the index map each time `_inbox` changes.
  - `FinishCombat()` and `OnRestStopDone()` — after removing the completed option, call `StepEscalation()`: increment every remaining room's escalation by 1.
  - `ShowInbox()` — pass escalation levels alongside `_inbox` list to `InboxScreen.ShowInContent()`. Signature: `ShowInContent(RectTransform, List<RoomOption>, int[] escalationLevels)`.
  - `InboxScreen.BuildRows()` — accept the escalation array, pass each room's level to `EmailRowView.Bind()`.
  - `EmailRowView.Bind()` — add `int escalation = 0` parameter. Update `previewText` to include escalation level (e.g. `34 HP  *  9 DMG/TURN  *  +2 ESCALATED`).

  **Effort:** Medium. No combat changes yet — this is purely data plumbing.

---

- [x] **TASK-46 · Apply Escalation to Combat Stats**
  Escalated rooms spawn harder enemies. Escalation scales damage and HP by a fixed amount per level.

  **What to change:**
  - `CombatSetup.BeginNextCombat()` — look up the escalation level for `_activeOption` before calling `enemyController.Init()`. Pass it in.
  - `EnemyController.Init(EnemyData data, int escalation = 0)` — scale `currentHP` and the damage stat:
    - HP: `data.maxHP + escalation * EscalationHPBonus` (suggested: +5 HP per level)
    - Damage: `data.damagePerTurn + escalation * EscalationDmgBonus` (suggested: +2 dmg per level)
    - Regen: unchanged (regen enemies are already the hardest content)
  - Constants `EscalationHPBonus` and `EscalationDmgBonus` as `const int` on `EnemyController` (easy to tune).
  - Reward tier is **unchanged** by escalation — harder fight, same reward tier. (Optional future tuning lever: escalated rooms could offer +1 tier reward, but don't implement now.)

  **Effort:** Small. Isolated change to `CombatSetup` + `EnemyController.Init()`.

---

- [x] **TASK-47 · Escalation Visual Feedback on Email Rows**
  Players must be able to read escalation state at a glance. No hidden information.

  **What to change:**
  - `EmailRowView` — add an optional escalation badge. When `escalation > 0`:
    - Show a small label appended to the row (right side or inside preview text): `+1`, `+2`, `+3`
    - Color the sender label by threat level:
      - `+0`: white (normal)
      - `+1`: yellow (`#FFCC44`)
      - `+2`: orange (`#FF8833`)
      - `+3+`: red (`#FF3333`)
    - `previewText` line shows modified stats: `34 HP (+5)  *  9 DMG (+2)`
  - `unreadDot` color — escalated combat rows shift the dot color from blue to the threat color above.
  - No new prefabs needed. All changes are inside `EmailRowView.Bind()`.

  **Effort:** Small. UI-only, no logic changes.

---

- [x] **TASK-48 · Tab System Foundation**
  All 8 sidebar entries exist in `FloorMapScreen.SbLabels`. Currently only INBOX and ALL MAIL are wired. This task establishes the shared plumbing that all subsequent tab tasks (TASK-50 through TASK-55) depend on.

  **Shared architecture changes:**
  - `RoomOption` — add `InboxTab tab` field (`enum InboxTab { Inbox, Spam, Starred, Snoozed, Important, Sent, Drafts }`). Enum lives in `FloorMapManager.cs` alongside `RoomType`.
  - `CombatSetup` — add one `List<RoomOption>` per tab: `_spam`, `_starred`, `_snoozed`, `_important`, `_sent`, `_drafts`. Each escalates independently. `BuildInboxForFloor()` populates all lists.
  - `FloorMapManager` — add `GenerateTabEncounters(List<EnemyData> pool, InboxTab tab)` dispatcher. Each tab has its own generation logic (see TASK-50–55).
  - `FloorMapScreen.BuildSidebar()` — wire click listeners on all currently-dead sidebar entries (SPAM, STARRED, SNOOZED, IMPORTANT, SENT, DRAFTS). Each fires an `OnTabSelected(InboxTab)` event on `FloorMapScreen`.
  - `FloorMapScreen` — add `public UnityEvent<InboxTab> OnTabSelected`. `CombatSetup` subscribes and dispatches to the correct `List<RoomOption>`.
  - `InboxScreen.ShowInContent()` — already tab-agnostic; reuse as-is. All tabs render the same row layout.
  - Floor progression: only `_inbox.Count == 0` triggers `OnLevelCleared()`. All other tabs are optional and never gate progression.

  **Effort:** Medium. This is foundational plumbing — no gameplay in this task, just the wiring. Complete before TASK-50–55.

---

- [x] **TASK-50 · SPAM Tab — Ambush Encounters**
  High-risk, high-reward combat. Spam emails are things that shouldn't exist but showed up anyway.

  **Encounter type:** Combat only.
  **Enemy pool:** Same floor pool as INBOX, but enemies spawn pre-escalated at +1 level (harder than fresh inbox rows).
  **Reward:** +1 tier above floor baseline (e.g. Floor 1 Spam → Uncommon reward, Floor 2 → Rare).
  **Escalation:** Spam rows escalate independently. Completing an INBOX combat does not escalate SPAM, and vice versa.

  **What to change:**
  - `FloorMapManager` — `GenerateTabEncounters(pool, InboxTab.Spam)`: build `RoomOption` list from the floor pool. Set a `baseEscalation = 1` field on each option (requires adding `int baseEscalation` to `RoomOption`). Override `rewardTier` to +1 tier.
  - `CombatSetup.BeginNextCombat()` — when tab is Spam, add `baseEscalation` to the escalation level lookup before passing to `EnemyController.Init()`.
  - `SbCounts` array in `FloorMapScreen` — update SPAM count to show the actual generated row count.

  **Effort:** Small. Builds directly on TASK-48 foundation.

---

- [x] **TASK-51 · STARRED Tab — Priority / Elite Encounters**
  The emails you flagged as important and never dealt with. The hardest optional fights.

  **Encounter type:** Combat only (elite variant).
  **Enemy pool:** Next floor's enemy pool (harder enemies than the current floor). On Floor 4, use the boss's support pool.
  **Reward:** Always one tier higher than SPAM (e.g. Floor 1 Starred → Rare). Guaranteed.
  **Escalation:** Does not escalate at all. STARRED rows are already at maximum threat — they are static challenges.

  **What to change:**
  - `FloorMapManager` — `GenerateTabEncounters(pool, InboxTab.Starred)`: use `GetPoolForFloor(currentFloor + 1)`. Build 2 rooms per floor (not 4 — scarcity matters). Override reward tier to `RewardTier.Rare` minimum.
  - `CombatSetup` — `_starred` list never receives escalation ticks. Pass `escalation = 0` always.
  - `EmailRowView` — STARRED rows show a star icon or `STARRED` badge to communicate their elite nature.

  **Effort:** Small.

---

- [ ] **TASK-52 · SNOOZED Tab — Rest and Recovery Encounters**
  Emails you delayed dealing with. Low threat, no combat — purely a recovery path.

  **Encounter type:** Rest stops and passive recovery choices only. No enemies.
  **Content:** 2–3 rows per floor. Each row is one of:
    - Standard rest stop (restore 15 HP)
    - HP-for-AP trade: "Take a quick call — lose 5 HP, gain +1 max AP this combat"
    - Deck thinning: "Archive it — permanently remove one card from your deck"
  **Escalation:** Does not escalate (there is no threat to escalate).
  **Floor progression:** Clearing all SNOOZED rows has no effect on floor completion.

  **What to change:**
  - `RoomType` enum — add `HpForApTrade` and `DeckThin` variants (or use `RoomType.Event` as a catch-all with `eventType` field on `RoomOption`).
  - `FloorMapManager` — `GenerateTabEncounters(pool, InboxTab.Snoozed)`: generate rest stops + 1 event variant. No enemy required.
  - `CombatSetup` — handle the new room types in `OnSnoozedRowSelected()`.
  - `RestStopScreen` or new `EventScreen` — display the choice UI for trade and deck-thin variants.

  **Effort:** Medium. Requires a new event room type and potentially a new screen.

---

- [x] **TASK-53 · IMPORTANT Tab — Urgent / Pre-Escalated Encounters**
  The emails marked urgent that you kept ignoring. Pre-escalated from the start — already at threat level +2.

  **Encounter type:** Combat only.
  **Enemy pool:** Current floor pool. All rows spawn at escalation +2 immediately.
  **Reward:** +1 tier above floor baseline (same as Spam).
  **Escalation:** Escalates normally from their +2 baseline. Can reach dangerous levels quickly.
  **Design intent:** "You waited too long to open this one."

  **What to change:**
  - `FloorMapManager` — `GenerateTabEncounters(pool, InboxTab.Important)`: set `baseEscalation = 2` on each option. 2 rows per floor.
  - `EmailRowView` — IMPORTANT rows display a `!! URGENT` badge in red from the start.
  - `CombatSetup` — same escalation logic as Spam, using the higher base offset.

  **Effort:** Trivial. Entirely parameterised by base escalation value; no new systems.

---

- [ ] **TASK-54 · SENT Tab — Event / Narrative Encounters**
  Emails you sent and their consequences. No combat — purely about choices and passive effects.

  **Encounter type:** Events only (choose A or B, each with a permanent consequence).
  **Content examples:**
    - "You sent a strongly worded email. +5 max HP, but start next combat with Guilt applied."
    - "You CC'd everyone. Deal 8 damage to next enemy at combat start, but draw 1 fewer card next turn."
    - "You unsubscribed from 3 mailing lists. Remove 1 Starter card from your deck permanently."
  **Escalation:** Does not escalate. Events are static.
  **Floor progression:** Clearing SENT rows never required.

  **What to change:**
  - `EventData` ScriptableObject — `string promptText`, `string optionALabel`, `string optionBLabel`, `CardEffect[] optionAEffects`, `CardEffect[] optionBEffects`. Store in `Assets/Data/Events/`.
  - `EventScreen` — new screen. Displays prompt and two buttons. On choice, applies effects via `CardEffectResolver` or a new `EventEffectResolver`.
  - `FloorMapManager` — `GenerateTabEncounters(pool, InboxTab.Sent)`: pick 2 events from an `EventData[]` pool (serialised on `FloorMapManager`).
  - `CombatSetup` — handle `RoomType.Event` by showing `EventScreen`.

  **Effort:** Large. Requires a new `EventData` SO type, new screen, and new effect resolver. Do last among the tab tasks.

---

- [ ] **TASK-55 · DRAFTS Tab — Shop Encounters**
  Emails you never sent. Unfinished business. Trade resources for cards or relics.

  **Encounter type:** Shop only. No combat.
  **Content:** 1 row per floor. Opens a simple shop screen:
    - 2 cards for sale (spend HP — no gold system exists): "Reply to this draft — lose 8 HP, add this card to your deck"
    - 1 relic for sale: "Send it — lose 15 HP, gain this relic"
    - 1 card removal: "Delete draft — permanently remove 1 card from your deck for free"
  **Escalation:** Does not escalate.

  **What to change:**
  - `ShopScreen` — new screen. Displays 2 card offers and 1 relic offer. HP cost deducted on purchase via `GameManager.TakeDamage()` (direct, bypasses shield). Relic added via `RelicManager`.
  - `FloorMapManager` — `GenerateTabEncounters(pool, InboxTab.Drafts)`: 1 room, `RoomType.Shop`.
  - `CombatSetup` — handle `RoomType.Shop` by showing `ShopScreen`.
  - `AllCardsRegistry` + `UnlockManager` — shop offers come from the enabled reward pool (same source as card rewards). Use the floor's allowed rarities as the offer pool.

  **Effort:** Large. Requires a new screen and shop logic. Do after TASK-54.

---

- [ ] **TASK-49 · Escalation Tuning Pass**
  Playtest and tune escalation constants. This task is explicitly about balance, not new code.

  **Tuning levers:**
  - `EscalationHPBonus` and `EscalationDmgBonus` in `EnemyController`
  - Whether rest stops also trigger escalation (currently: yes — skipping a fight to heal still pressures remaining rooms)
  - Whether boss floor (floor 4, single encounter) uses the escalation system (probably no — it has only 1 room)
  - Escalation cap: consider capping at +3 so rooms don't become impossible on large floors

  **Deliverable:** Updated constant values committed with a note in the Design Change Log.

---

## Design Change Log

- **2026-03-29 · Inbox Priority System**: Linear top-to-bottom row clearing replaced by free-choice selection with escalation. Unaddressed emails grow in difficulty each time an encounter is completed. All 6 sidebar tabs become real gameplay paths: SPAM (ambush combat), STARRED (elite combat), SNOOZED (rest/recovery), IMPORTANT (pre-escalated combat), SENT (events/choices), DRAFTS (shop). Design intent: the inbox becomes a risk management system, not a checklist.
- **2026-03-15 · Inbox as Hub**: Floor map replaced by persistent Gmail-style Inbox screen. All encounters visible upfront. "Floors" → "Levels" in UI.
- **2026-03-26 · Roguelite Reward Overhaul**: Chance-based drops + collection pool replaced by guaranteed 3-card pick per win. Card unlock system added (enemy-tier gates, signature drops, behavior unlocks). Main menu card pool toggle added.
- **2026-03-26 · Screen Ownership Split**: Main Menu is now the pre-run config hub (card pool toggle, unlocks, character select). InboxLayout is shown only during an active run. Inbox tabs act as run paths: Inbox = main combat path, other tabs (Spam, Sent, Promotions, etc.) = optional side paths for relics/events/shops. No explicit map node graph — the inbox IS the map.
