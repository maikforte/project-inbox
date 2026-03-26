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

- [ ] **TASK-38 · Guaranteed 3-Card Reward**
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

## Design Change Log

- **2026-03-15 · Inbox as Hub**: Floor map replaced by persistent Gmail-style Inbox screen. All encounters visible upfront. "Floors" → "Levels" in UI.
- **2026-03-26 · Roguelite Reward Overhaul**: Chance-based drops + collection pool replaced by guaranteed 3-card pick per win. Card unlock system added (enemy-tier gates, signature drops, behavior unlocks). Main menu card pool toggle added.
- **2026-03-26 · Screen Ownership Split**: Main Menu is now the pre-run config hub (card pool toggle, unlocks, character select). InboxLayout is shown only during an active run. Inbox tabs act as run paths: Inbox = main combat path, other tabs (Spam, Sent, Promotions, etc.) = optional side paths for relics/events/shops. No explicit map node graph — the inbox IS the map.
