# INBOX//ZERO — Task List

Tasks are ordered by priority. Complete the game loop first before content and polish.
Each task is sized to be a single prompt session.

**Status legend:** `[ ]` todo · `[x]` done · `[~]` in progress

---

## MILESTONE 1 — Core Combat Loop (Playable Combat)

- [x] **TASK-01 · Data Models**
  Define `CardData`, `EnemyData`, and `RelicData` as ScriptableObjects.
  Fields per spec in CLAUDE.md. No game logic — data only.

- [x] **TASK-02 · Game Manager & Run State**
  Singleton `GameManager` tracking: current HP, max HP, current AP, deck list, hand, discard, active relics, current floor, current room.
  No UI — pure state.

- [x] **TASK-03 · Combat Scene UI Layout**
  Build the combat scene canvas (640×360):
  - Enemy panel (name, HP bar, status icons area)
  - Player panel (HP bar, AP pips, shield display)
  - Hand area (card slots at bottom)
  - End Turn button
  Use Graybox2D sprites and Micro5 font. No logic yet — layout only.

- [ ] **TASK-04 · Deck & Hand System**
  `DeckManager`: shuffle draw pile, draw N cards into hand, discard hand at end of turn, reshuffle discard into draw pile when empty.
  Wire up to GameManager.

- [ ] **TASK-05 · Turn & AP System**
  `TurnManager`: player turn start (draw 5, gain 3 AP), end turn button triggers enemy turn, enemy turn end returns to player.
  AP spend/refund logic. Block card play when AP insufficient.

- [ ] **TASK-06 · Card UI & Playing Cards**
  Instantiate hand cards as UI elements. Card shows name, cost, type color, effect text.
  Click to play: deduct AP, trigger effect, move card to discard.
  Hover to preview full card.

- [ ] **TASK-07 · Basic Card Effects**
  Implement effect handlers for: `DealDamage`, `GainShield`, `DrawCards`, `GainAP`.
  Shield absorbs damage before HP; resets to 0 at end of player turn.

- [ ] **TASK-08 · Enemy Combat Logic**
  `EnemyController`: display enemy stats, attack player each enemy turn (respects shield), apply regen at end of enemy turn, die when HP ≤ 0.

- [ ] **TASK-09 · Combat Win / Lose**
  On enemy death → show "Victory" state, pause for input.
  On player HP ≤ 0 → trigger Game Over.
  Both states block further input.

---

## MILESTONE 2 — Run Structure (Full Floor Loop)

- [ ] **TASK-10 · Card Reward Screen**
  After combat victory: show 3 random cards drawn from the reward pool (filtered by floor tier). Player picks 1 to add to deck, or skips. Then advance to room selection.

- [ ] **TASK-11 · Floor Map & Room Selection**
  After reward screen: show 2–3 room options for next room (combat, rest stop icons).
  Player picks one. Track room progress (4 rooms per floor).

- [ ] **TASK-12 · Rest Stop Room**
  Non-combat room: display flavour text, restore 15 HP (capped at max HP), advance to next room.

- [ ] **TASK-13 · Floor Transition**
  After Room 4 of a floor: show floor-cleared message, award a relic (Floors 2 and 3 only), then load next floor.
  On Floor 4 Room 1 cleared → trigger Victory.

---

## MILESTONE 3 — Status Effects & Starter Content

- [ ] **TASK-14 · Status Effects System**
  Implement `Unread` (enemy skips attack, ticks down each enemy turn), `Guilt` (2 dmg/turn to afflicted, stackable), `Awaiting Reply` (player loses 1 AP next turn per stack).
  Status icon display on enemy/player panel.

- [ ] **TASK-15 · All Starter Cards**
  Implement and create ScriptableObjects for all 9 starter deck cards:
  Reply Politely, Archive It (×2), Hard Delete, Mark as Read (×2), Unsubscribe, Set Filter.

- [ ] **TASK-16 · Floor 1 Enemies**
  Create EnemyData ScriptableObjects and wire combat for:
  - Newsletter Flood (28 HP, 5 dmg/turn)
  - Calendar Invite (22 HP, 7 dmg/turn)

- [ ] **TASK-17 · Floor 2 Enemies & Uncommon Cards**
  Enemies: Reply-All Demon (40 HP, 9 dmg), Auto-CC Manager (35 HP, 8 dmg).
  Uncommon reward cards: Forward Bomb, Snooze 7 Days, Keyboard Shortcut, CC the CEO, Report as Spam.

- [ ] **TASK-18 · Floor 3 Enemies & Rare Cards**
  Enemies: Out-of-Office Loop (50 HP, 10 dmg, applies Awaiting Reply), Passive-Aggressive Karen (45 HP, 11 dmg, 4 regen/turn).
  Rare reward cards: Vacation Autoresponder, Recall Email, Start New Thread.

- [ ] **TASK-19 · Floor 4 Boss**
  The Thread That Never Ends (80 HP, 14 dmg/turn, 5 regen/turn).
  Wire into Floor 4 room 1 as the sole encounter.

---

## MILESTONE 4 — Relics

- [ ] **TASK-20 · Relic System & Starting Relic**
  `RelicManager`: store active relics, trigger hooks (on turn start, on card played, on damage taken, on combat start).
  Implement Paperclip (+1 AP on first turn of combat). Show relic icons in UI.

- [ ] **TASK-21 · Acquirable Relics**
  Implement all 5 acquirable relics with their hooks:
  Cold Coffee (+3 HP/turn start), Inbox Zero Badge (+2 dmg per attack card), Mechanical Keyboard (+1 draw/turn), Do Not Disturb (-2 incoming dmg), Work Phone Off (+15 max HP on acquire).

---

## MILESTONE 5 — Game Screens

- [ ] **TASK-22 · Main Menu / Run Start Screen**
  Title screen: game name, "Start Run" button, brief flavour text. Loads into Floor 1 Room 1 combat.

- [ ] **TASK-23 · Game Over Screen**
  On HP ≤ 0: show "RUN TERMINATED" screen, floor reached, prompt to restart. Returns to main menu.

- [ ] **TASK-24 · Victory Screen**
  On boss death: show "INBOX ZERO ACHIEVED" screen with run stats (floors cleared, cards played count, damage dealt). Return to main menu.

---

## MILESTONE 6 — Polish

- [ ] **TASK-25 · Card Animations & Visual Feedback**
  Card play animation (slide up + fade), damage numbers floating on hit, HP bar smooth lerp, shield flash on block.

- [ ] **TASK-26 · SFX Placeholders**
  Hook up placeholder audio: card play click, damage hit, shield block, enemy death, turn end. Use Unity's built-in AudioSource.

---

*Last updated: 2026-03-13*
