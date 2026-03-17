# INBOX//ZERO
### A Roguelite About Your Emails

**Technical:** Pixel art game. Resolution: 640x360. URP 2D. Unity 6.

## Development Conventions

- **Tasks:** See `TASKS.md` for the full task list. Work on one task at a time as prompted.
- **Placeholder art:** Use `Assets/Graybox2D/` sprites for all in-game visuals until final art is ready. Sprites are named by pixel dimension (e.g. `64x128.png`, `128x32.png`). Pick the size closest to the UI element's intended size — Unity will stretch it to fill. This makes swapping to final art trivial: just replace the sprite reference. Available sizes: 16×16, 32×32, 64×64, 128×128, 256×256 and many rectangular variants. Slope and circle corner variants also exist for shaped elements.
- **Fonts:** Use `Micro5-Regular` (primary) for all UI text — both in scene YAML and programmatic TMP creation. `BetterPixels` is retired. Both are TMP bitmap assets in `Assets/TextMesh Pro/Fonts/`. Every visible UI label needs a TMP text component — panels, bars, slots, and buttons should all have readable placeholder text so the layout blocking is clear. Font GUID for Micro5-Regular: `968711493fda13b4789c59de06321a30`. TMP `TextMeshProUGUI` script GUID: `f4688fdb7df04437aeb418b961361dc5` (from `com.unity.ugui` package). When writing scene YAML directly, always use these exact GUIDs — a wrong GUID produces a "missing script" error. Never use emoji or `\uXXXX` surrogate-pair escapes in `m_text` fields; Unity's YAML parser rejects them. Use plain ASCII substitutes (e.g. `SHIELD 0` not `[shield] 0`). **Standard font size is 14. Minimum is 14 — no exceptions.** This applies to `m_fontSize`, `m_fontSizeBase`, and `m_fontSizeMin` in scene YAML and to any programmatic `fontSize` assignments in scripts.
- **No screenshots:** Do not take screenshots to verify results.
- **Scripts folder:** All game scripts go in `Assets/Scripts/`. Organize by subfolder: `Core/`, `Combat/`, `UI/`, `Data/`, `Cards/`, `Enemies/`, `Relics/`.
- **ScriptableObjects:** Card, Enemy, and Relic data are ScriptableObjects stored in `Assets/Data/`.
- **Asset GUIDs:** When writing `.asset` files and their `.meta` files manually, Unity may regenerate the `.meta` with a new GUID on first import. Always read the `.meta` file back after Ctrl+R and update any scene YAML references that used the pre-assigned GUID. Symptoms of a stale GUID: null entries in inspector lists, NullReferenceException from code that iterates those lists.
- **Inbox UI architecture (SPA pattern):** The inbox screen uses a single-page-app pattern. `InboxLayout.prefab` provides the persistent chrome (top bar + sidebar). Each sidebar nav item swaps a *page prefab* into the `ContentArea` `RectTransform`. Current pages: `DraftsPage.prefab` (deck builder, shown by `DeckBuilderScreen.ShowInContent`), `AllMailPanel.prefab` (card compendium, shown by `AllMailScreen`). Each prefab has a *View component* (`DraftsPageView`, `AllMailPanelView`, `LayoutView`) that exposes serialized child references — scripts read these refs at runtime instead of using `GetComponentInChildren`. **Never run a builder MenuItem (`InboxZero → Rebuild … Prefab`) on a prefab that has been customized in the inspector — it will wipe those customizations.** Only run builders to create a prefab from scratch.

## How to Test After Each Task

### After any script change
1. **Ctrl+R** (Assets → Refresh) in Unity to recompile.
2. Check **Console** — fix all errors before testing. Warnings are OK.
3. Press **Play**.

### Milestone 2 — Run structure (TASK-10 to 13B)
- Kill the enemy → **Victory panel** appears ("INBOX CLEARED"). Click **Continue** → panel hides, card reward screen appears.
- Pick a card or skip → **Inbox screen** appears showing remaining emails for this level.
- Click a Combat email → new combat starts. Click a Rest Stop email → rest screen heals HP, returns to Inbox.
- After all emails cleared → level transition screen shows "LEVEL X CLEARED". Click Continue → next level's Inbox loads.
- Let HP reach 0 → game over panel appears. Click Restart → combat resets.

### Milestone 3 — Status effects & starter cards (TASK-14 to 15)
- Press Play → hand should show **5 cards** from the 9-card starter deck (Reply Politely ×2, Archive It ×2, Hard Delete, Mark as Read ×2, Unsubscribe, Set Filter). Card names and type-bar colors should be correct.
- Hover a card → preview panel shows card details. **Click** the card → preview dismisses immediately (no lingering panel).
- To test **Unread**: temporarily set the test enemy's `Status Applied On Attack = Unread`, `Status Duration = 2` in the inspector. End your turn → the enemy panel shows `UNREAD 2`. On the enemy's turn it skips the attack. On the next enemy turn it attacks and the counter drops to 1, then clears.
- To test **AwaitingReply**: set enemy SO to `Status Applied On Attack = AwaitingReply`, `Status Duration = 1`. End turn → after the enemy attacks, start of your next turn shows `AWAITINGREPLY 1` briefly on the player panel, and you have 2 AP instead of 3.
- To test **Guilt on enemy**: temporarily add a card effect `ApplyStatusToEnemy / Guilt / duration 3` to any card. Play it → enemy panel shows `GUILT 3`. Each enemy turn it takes 6 damage (3 stacks × 2) and the counter drops.

> *"The inbox is a dungeon. Every unread is a monster. You are the only one who can save yourself."*

---

## Table of Contents

1. [Overview](#overview)
2. [Core Loop](#core-loop)
3. [The Floors](#the-floors)
4. [Enemies](#enemies)
5. [Cards](#cards)
6. [Status Effects](#status-effects)
7. [Relics](#relics)
8. [Meta-Progression](#meta-progression)
9. [Playthrough Walkthrough](#playthrough-walkthrough)
10. [Strategies & Tips](#strategies--tips)
11. [Glossary](#glossary)
12. [Design Notes](#design-notes)

---

## Overview

**INBOX//ZERO** is a single-player roguelite card game where every run simulates a single workday's battle against your email inbox. You build a deck of response actions, fight email monsters level by level, collect relics, and attempt to reach the mythical state of Inbox Zero.

Each run is fresh. Each run will probably kill you.

| Stat | Value |
|---|---|
| Inbox Levels | 4 |
| Emails per Level | 4 |
| Starting HP | 50 |
| Starting AP per turn | 3 |
| Starting Hand Size | 5 |
| Win Condition | Defeat the Final Boss on Level 4 |
| Lose Condition | HP reaches 0 |

---

## Core Loop

### The Turn Structure

Each combat encounter follows this sequence:

```
COMBAT START (once per fight)
    → Draw 5 cards into hand (opening hand)
    → Gain 3 AP

START OF TURN (every subsequent turn)
    → Draw 1 card
    → Gain 3 AP
    → Apply start-of-turn relic effects (Cold Coffee: +3 HP)
    → Apply start-of-turn status effects (AwaitingReply AP reduction)

YOUR TURN
    → Play cards from hand by spending AP
    → Played cards go to the discard pile immediately
    → Unplayed cards STAY IN HAND — they are never auto-discarded
    → End turn when done (or out of AP)

ENEMY TURN
    → Enemy attacks for its listed damage
    → Shield absorbs damage first
    → Leftover damage hits your HP
    → Enemy regen triggers (if applicable)
    → Player status effects tick down (Guilt deals 2 dmg, etc.)

END OF TURN
    → Shield resets to 0
    → Loop back to Start of Turn
```

### Card Draw & Deck Cycling

- **Hand persists.** Only played cards move to the discard pile. Unplayed cards remain in hand.
- **Draw 1 per turn.** After the opening hand of 5, you draw exactly 1 card at the start of each turn (effects like Mark as Read or Unsubscribe can draw additional cards).
- **Deck exhaustion.** When the draw pile runs out, shuffle the discard pile into a new draw pile. Cards currently in hand are **never** included in the reshuffle.
- **Max hand size: 7.** If drawing would exceed 7 cards, you must choose a card to discard before the new card enters. This prevents indefinite hand accumulation.

### Card Drops (replaces guaranteed reward screen)

Enemies do **not** guarantee a card reward. Instead, defeating an enemy has a chance to drop a card based on the enemy's reward tier. Dropped cards go into a **collection pool** — they are not immediately added to the deck.

| Enemy Tier | Drop Chance | Card Rarity Offered |
|---|---|---|
| Common | 40% | Common |
| Uncommon | 65% | Common or Uncommon |
| Rare | 90% | Uncommon or Rare |
| Boss | 100% | Rare |

**Deck building** happens at dedicated moments only — not mid-combat, not after every fight. Proposed triggers (TBD, pick one or combine):
- **Rest Stops** — the rest stop screen shows collected cards and lets you swap them into/out of your active deck alongside the HP heal
- **Level transitions** — the floor-cleared screen includes a deck builder step before moving to the next level
- **Inbox Drafts** — dropped cards appear as a special "Drafts" folder row in the inbox sidebar; opening it at any time lets you review and equip

### Deck Composition Rules

These limits apply when adding cards to the active deck:

| Rarity | Max copies in deck |
|---|---|
| Starter | No limit (form the baseline) |
| Common | No limit |
| Uncommon | 4 total |
| Rare | 2 total |

- **Max deck size: 15 cards.** You may always leave collected cards in the pool unequipped.
- You may never hold more than 1 copy of the same Rare card.
- Cards in the collection pool but not in the active deck are kept for the rest of the run.

### Proposed Future Mechanics (design notes)

Ideas worth considering for later tasks — not yet implemented:

| Mechanic | Description |
|---|---|
| **Overflow Discard** | When hand hits 7 and you must discard, you choose — creates meaningful tension between keeping cheap cards vs. strong ones |
| **Priority Flag** | Mark 1 card per deck as Priority; it is always in your opening hand of 5 |
| **Momentum** | Playing 3+ cards in a single turn grants +1 AP the following turn ("cleared the queue") |
| **Reply Chain** | Playing the same card twice in consecutive turns gives it +50% effect ("the thread keeps going") |
| **Mark Unread** | You can return a card from hand to top of draw pile — useful for protecting a key card when hand is nearly full |
| **Attachment** | Some cards have an Attachment flag; they must be played on the turn immediately after being drawn, or they auto-discard |
| **Inbox Drafts** | Dropped cards appear as a "Drafts" row in the inbox sidebar — opens a deck builder overlay at any time between fights |
| **Card Selling** | At rest stops, you can permanently delete a card from the collection pool in exchange for +5 HP ("unsubscribe from the mailing list") |
| **Forwarded Cards** | Some drops are "forwarded" — they have a one-time use bonus effect the first time they are played, then become a normal card |

### The Inbox Structure

The **Inbox screen** (Gmail-style) is the primary navigation hub. On starting a new game and after every combat:
1. The Inbox screen is shown — all emails for the current level are listed as rows (sender, subject, preview)
2. The player clicks any email to fight that enemy
3. After winning → **card reward screen** — choose 1 of 3 cards to add (or skip) → return to Inbox
4. Rest Stop emails appear in the list too; clicking one heals 15 HP and removes it from the Inbox
5. When all emails in the Inbox are cleared → level complete, relic drop if applicable, next level's Inbox loads

### The Level Structure

| Level | Theme | Difficulty | Relic Drop? |
|---|---|---|---|
| 1 | The Inbox | Easy | No |
| 2 | The Threads | Medium | Yes |
| 3 | The Escalations | Hard | Yes |
| 4 | The Final Thread | Boss | No |

---

## The Floors

### Floor 1 — The Inbox

*"You open your laptop. It's already bad."*

Your first encounters are standard bulk mail and meeting requests. They hit for modest damage (5–7 per turn) and have low HP (22–28). This is the tutorial floor, whether you asked for one or not.

**Goal:** Build your deck. Add 2–4 new cards before Floor 2. Prefer cards that draw more cards or apply statuses — raw damage scales better later when you have more AP.

**Enemies:** Newsletter Flood, Calendar Invite

---

### Floor 2 — The Threads

*"Someone replied all. Then someone replied to the reply all."*

Enemies gain regen behaviors and hit harder (8–9 per turn). Elite encounters introduce status effects. You will receive your first relic after clearing this floor.

**Goal:** Establish a shield-or-burst strategy. You cannot out-sustain Floor 2 enemies with only starter cards.

**Enemies:** Reply-All Demon, Auto-CC Manager

---

### Floor 3 — The Escalations

*"It has been forwarded to your manager. And their manager. And HR."*

Enemies hit for 10–11 per turn and some regenerate HP every enemy turn. The Out-of-Office Loop applies Awaiting Reply. Passive-Aggressive Karen is the most dangerous non-boss encounter in the game due to her regen.

**Goal:** You must have a way to deal burst damage by now. Debuffs (Unread, Guilt) are critical here. If you have Forward Bomb or CC the CEO, prioritize them.

**Enemies:** Out-of-Office Loop, Passive-Aggressive Karen

---

### Floor 4 — The Final Thread

*"Subject: Re: Re: Re: Fw: Fw: Re: Friday Lunch? (78 participants)"*

One encounter. One enemy. The Thread That Never Ends.

The boss hits for 14 damage per turn, regenerates 5 HP every enemy turn, and has 80 HP. You must deal ~100–110 effective damage to kill it when accounting for regen. This requires burst.

**Goal:** Survive long enough to assemble a kill combo. Unread + multi-card burst rounds are your win condition.

**Enemy:** The Thread That Never Ends

---

## Enemies

### Newsletter Flood
| Stat | Value |
|---|---|
| Type | BULK MAIL |
| HP | 28 |
| Damage/turn | 5 |
| Regen | None |
| Status applied | None |
| Reward tier | Common |

*"You signed up for this. You did this to yourself."*

The gentlest encounter in the game. Use it to cycle your deck and draw your better cards. Don't waste rare cards here.

---

### Calendar Invite
| Stat | Value |
|---|---|
| Type | MEETING REQUEST |
| HP | 22 |
| Damage/turn | 7 |
| Regen | None |
| Status applied | None |
| Reward tier | Common |

*"Someone wants 45 minutes of your life. Recurring."*

Low HP but slightly higher damage than the Newsletter. Priority target for a quick kill before it accumulates turns.

---

### Reply-All Demon
| Stat | Value |
|---|---|
| Type | INBOX MONSTER |
| HP | 40 |
| Damage/turn | 9 |
| Regen | None |
| Status applied | None |
| Reward tier | Uncommon |

*"Sent to everyone in the company. Every reply spawns more replies."*

Your first real threat. Without shield cards or burst damage, this fight lasts 5+ turns which compounds into serious HP loss. Apply **Unread** to buy a free turn if you have Forward Bomb.

---

### Auto-CC Manager
| Stat | Value |
|---|---|
| Type | PHANTOM CC |
| HP | 35 |
| Damage/turn | 8 |
| Regen | None |
| Status applied | None |
| Reward tier | Uncommon |

*"Just keeping everyone in the loop."*

Lower HP than the Reply-All Demon but still hits hard. More manageable if you've added shield cards from Floor 1 rewards.

---

### Out-of-Office Loop
| Stat | Value |
|---|---|
| Type | INFINITE BOUNCE |
| HP | 50 |
| Damage/turn | 10 |
| Regen | None |
| Status applied | Awaiting Reply (player) |
| Reward tier | Rare |

*"Replies to itself. Has been going for 3 years."*

Applies **Awaiting Reply** every attack, which reduces your max AP the following turn. This fight becomes a resource war. Kill it fast or it starves your hand of playable cards.

---

### Passive-Aggressive Karen
| Stat | Value |
|---|---|
| Type | ELITE THREAT |
| HP | 45 |
| Damage/turn | 11 |
| Regen | 4 HP/turn |
| Status applied | None |
| Reward tier | Rare |

*"Per my last email..."*

The hardest non-boss encounter. She regenerates 4 HP every enemy turn, meaning you must deal ~65+ effective damage across the fight. If your deck lacks burst damage, this encounter is a death sentence. **Unsubscribe + CC the CEO + Forward Bomb** in a single turn is the ideal solution.

---

### The Thread That Never Ends *(Final Boss)*
| Stat | Value |
|---|---|
| Type | FINAL BOSS |
| HP | 80 |
| Damage/turn | 14 |
| Regen | 5 HP/turn |
| Status applied | None |
| Reward tier | Victory |

*"78 participants. 1,204 replies. Subject: Re: Re: Re: Fw: Fw: Re: Friday Lunch?"*

The endgame boss. Deals 14 damage per turn and regenerates 5 HP after every attack. You need to deal approximately 100–110 total damage to win. Effective strategies:

- **Freeze + Burst:** Apply **Unread** to skip one of its attack turns, then dump your entire hand into burst damage.
- **Relic stacking:** Inbox Zero Badge (+2 damage per attack card), Do Not Disturb (-2 incoming damage), Cold Coffee (+3 HP/turn) all significantly shift this matchup.
- **Card combos:** Start New Thread (12 dmg + 2 draw + 1 AP) chains into further plays. Look for turns where you can play 4–5 cards.

---

## Cards

Cards are the primary resource of every run. You start with a 9-card starter deck and can add 1 card after every encounter.

### Rarity Tiers

| Tier | Description | Available From |
|---|---|---|
| **Common** | Reliable, simple effects | Floor 1–2 rewards |
| **Uncommon** | Stronger effects with interesting mechanics | Floor 2–3 rewards |
| **Rare** | High impact, often multi-effect | Floor 3–4 rewards |

### Card Types

| Type | Color | Role |
|---|---|---|
| **Attack** | Red | Deals damage to the enemy |
| **Defend** | Green | Generates shield |
| **Special** | Purple | Draw, AP gain, status application, utility |

---

### Starter Deck Cards

#### Reply Politely
| Field | Value |
|---|---|
| Type | Attack |
| Cost | 1 AP |
| Effect | Deal 6 damage |
| Rarity | Starter |

Your bread-and-butter attack. Low damage, low cost. Gets outscaled quickly but remains useful as an AP-efficient filler. You start with 2 copies.

---

#### Archive It
| Field | Value |
|---|---|
| Type | Defend |
| Cost | 1 AP |
| Effect | Gain 8 shield |
| Rarity | Starter |

The most efficient shield card in the starter set. You start with 2 copies. Becomes less relevant when you have Set Filter, but never useless.

---

#### Hard Delete
| Field | Value |
|---|---|
| Type | Attack |
| Cost | 2 AP |
| Effect | Deal 14 damage |
| Rarity | Starter |

The hardest-hitting starter. Spends 2 AP for 14 damage — better damage-per-AP than Reply Politely. Use on turns where you have AP to spare.

---

#### Mark as Read
| Field | Value |
|---|---|
| Type | Special |
| Cost | 0 AP |
| Effect | Draw 1 card + deal 2 damage |
| Rarity | Starter |

Zero-cost cards are extremely powerful in any roguelite deck. Draws a card and prods the enemy for 2. You start with 2 copies. Never remove these.

---

#### Unsubscribe
| Field | Value |
|---|---|
| Type | Special |
| Cost | 1 AP |
| Effect | Deal 5 damage + draw 2 cards |
| Rarity | Starter |

Exceptional card. Combines a light attack with 2 card draws, essentially replacing itself and adding another. A cornerstone of draw-engine decks.

---

#### Set Filter
| Field | Value |
|---|---|
| Type | Defend |
| Cost | 1 AP |
| Effect | Gain 12 shield |
| Rarity | Starter |

Strictly better than Archive It at the same cost. If this appears as a reward, always take it.

---

### Reward Cards

#### Forward Bomb *(Uncommon)*
| Field | Value |
|---|---|
| Type | Attack |
| Cost | 2 AP |
| Effect | Deal 10 damage + apply Unread (2 turns) to enemy |
| Rarity | Uncommon |

One of the best cards in the game. Applies **Unread**, freezing the enemy for 2 turns. That's 2 turns of free attacks and no incoming damage. Indispensable in the late floors.

---

#### Snooze 7 Days *(Uncommon)*
| Field | Value |
|---|---|
| Type | Special |
| Cost | 0 AP |
| Effect | Draw 2 cards |
| Rarity | Uncommon |

Zero AP cost for 2 draws. Pure engine card. In a well-built deck with Mark as Read + Unsubscribe, this creates chains where you draw through most of your deck in a single turn.

---

#### Keyboard Shortcut *(Uncommon)*
| Field | Value |
|---|---|
| Type | Special |
| Cost | 0 AP |
| Effect | Gain 2 AP this turn |
| Rarity | Uncommon |

Zero-cost AP generation. Play this early in a turn to supercharge the rest of your plays. Combos exceptionally well with Start New Thread.

---

#### CC the CEO *(Uncommon)*
| Field | Value |
|---|---|
| Type | Attack |
| Cost | 2 AP |
| Effect | Deal 18 damage |
| Rarity | Uncommon |

Raw damage, nothing fancy. At 18 damage for 2 AP, it's the highest damage-per-AP of any attack card. Essential in boss fights.

---

#### Report as Spam *(Uncommon)*
| Field | Value |
|---|---|
| Type | Attack |
| Cost | 1 AP |
| Effect | Deal 8 damage + apply Guilt to enemy |
| Rarity | Uncommon |

Efficient attack that also applies Guilt, which deals 2 damage per turn to the enemy. More meaningful in long fights against high-regen enemies like Passive-Aggressive Karen.

---

#### Vacation Autoresponder *(Rare)*
| Field | Value |
|---|---|
| Type | Defend |
| Cost | 2 AP |
| Effect | Gain 20 shield + deal 5 damage |
| Rarity | Rare |

The strongest single shield card in the game. The bonus 5 damage is a free addition. 20 shield blocks one full turn of boss damage.

---

#### Recall Email *(Rare)*
| Field | Value |
|---|---|
| Type | Special |
| Cost | 1 AP |
| Effect | Restore 5 HP + draw 1 card |
| Rarity | Rare |

HP recovery is extremely scarce. This is the only card that heals, making it disproportionately valuable in long runs. Take it if you're below 30 HP.

---

#### Start New Thread *(Rare)*
| Field | Value |
|---|---|
| Type | Special |
| Cost | 2 AP |
| Effect | Deal 12 damage + draw 2 cards + gain 1 AP |
| Rarity | Rare |

The most complex card in the game. Spends 2 AP, gains 1 back (net cost: 1 AP), deals 12 damage, and draws 2 cards. Those 2 draws frequently result in further plays, making the actual cost often less than 1 AP. Cornerstone of any viable late-game deck.

---

### Card Synergy Table

| Combo | Cards | Effect |
|---|---|---|
| Zero-cost chain | Mark as Read + Snooze 7 Days + Keyboard Shortcut | Draw through deck while building AP |
| Freeze burst | Forward Bomb → (enemy frozen) → CC the CEO + Hard Delete | 42 damage with no retaliation |
| Engine loop | Unsubscribe + Start New Thread | Each card feeds the other, cycling indefinitely |
| Shield wall | Vacation Autoresponder + Set Filter + Archive It | 40+ shield in a single turn |
| Poison stack | Report as Spam × 2 + Reply Politely × 2 | Applies multiple Guilt stacks for sustained DoT |

---

## Status Effects

Status effects are temporary conditions applied to players or enemies. They tick down by 1 at the end of each applicable turn.

### Enemy Status Effects (You apply these to enemies)

#### Unread
> *"Marked as unread. Temporarly paralyzed by the existential dread of being unread."*

| Field | Value |
|---|---|
| Applied by | Forward Bomb |
| Duration | 2 turns |
| Effect | Enemy cannot attack while Unread is active |
| Ticks | At the end of the enemy's turn |

The most powerful status in the game. An enemy that cannot attack is effectively frozen while you build burst damage. Two turns of free attacks while Unread is active can swing an otherwise losing fight.

---

#### Guilt *(Enemy)*
> *"They know what they did."*

| Field | Value |
|---|---|
| Applied by | Report as Spam |
| Duration | Stacking (each application adds duration) |
| Effect | Enemy takes 2 damage at the start of each enemy turn |
| Ticks | Per turn |

Damage over time. Relatively minor on its own but stacks. Against high-HP regen enemies like Passive-Aggressive Karen, multiple Guilt stacks can offset her regen entirely.

---

### Player Status Effects (Enemies apply these to you)

#### Guilt *(Player)*
> *"You know what you did. You never replied to that birthday email."*

| Field | Value |
|---|---|
| Applied by | Certain enemies (design expansion) |
| Effect | You take 2 damage at the end of each turn |
| Duration | Stacks, ticks down by 1 per turn |

The player version of Guilt. Punishing if it stacks. Prioritize clearing fights quickly when afflicted.

---

#### Awaiting Reply
> *"The cursor blinks. They are waiting. They are still waiting."*

| Field | Value |
|---|---|
| Applied by | Out-of-Office Loop |
| Effect | Reduces effective AP next turn |
| Duration | 1 per application |

Applied every time the Out-of-Office Loop attacks. Extends the fight by limiting your plays per turn. The longer you spend in this fight, the more it stacks.

---

## Relics

Relics are passive items that persist for the entire run. You start with one relic and can acquire additional ones between floors.

### Starting Relic

#### 📎 Paperclip
> *"Someone left this on your desk. It has survived three office moves."*

| Effect | Gain 1 bonus AP on the first turn of each combat |
|---|---|

The most consistent early relic. That extra AP on turn 1 lets you play an additional card before the enemy first attacks.

---

### Acquirable Relics

#### ☕ Cold Coffee
> *"You made it three hours ago. It's fine."*

| Effect | Restore 3 HP at the start of each turn |
|---|---|
| Acquired | After Floor 2 or 3 (random) |

Passive HP regeneration. Against high-damage enemies this may barely register, but across a full floor it adds 12–20 HP which is substantial. Excellent pairing with a low-damage, long-fight strategy.

---

#### 🗂 Inbox Zero Badge
> *"You achieved it once. In 2019. For six minutes."*

| Effect | All attack cards deal +2 bonus damage |
|---|---|
| Acquired | After Floor 2 or 3 (random) |

Scales with every attack card in your deck. In a deck with 6–8 attack cards, this relic adds 12–16 bonus damage per full deck cycle. Essential for boss fights.

---

#### ⌨️ Mechanical Keyboard
> *"Tactile feedback. Everyone in the office hates you."*

| Effect | Draw 1 extra card at the start of each turn |
|---|---|
| Acquired | After Floor 2 or 3 (random) |

Draw 6 instead of 5 each turn. In a 12-card deck, this means you see your whole deck every 2 turns instead of 2.4 turns. Stronger than it sounds in draw-engine decks.

---

#### 🔕 Do Not Disturb
> *"Enabled since March. No one has noticed."*

| Effect | Reduce all incoming damage by 2 |
|---|---|
| Acquired | After Floor 2 or 3 (random) |

A flat damage reduction that applies before shield. Against the final boss's 14 damage/turn, this reduces it to 12 — saving 2 HP every enemy turn, or roughly 10–14 HP over an average boss fight.

---

#### 📱 Work Phone (Off)
> *"You turn it on every morning. Every morning you regret it."*

| Effect | +15 max HP (applied immediately on acquisition) |
|---|---|
| Acquired | After Floor 2 or 3 (random) |

Straightforward HP buffer. If you're below 25 HP entering Floor 3, this relic may be the only thing that keeps you alive long enough to reach the boss.

---

### Relic Synergies

| Combo | Relics | Why It's Strong |
|---|---|---|
| Damage stacking | Inbox Zero Badge + Paperclip | Extra turn 1 AP + damage bonus on all attacks |
| Sustain machine | Cold Coffee + Do Not Disturb | Net 5 HP restored per turn cycle (3 gained, 2 less taken) |
| Draw engine | Mechanical Keyboard + Paperclip | Extra draw + extra AP compounds into seeing more cards faster |
| Tank build | Work Phone + Cold Coffee + Do Not Disturb | 65 max HP, healing, damage reduction |

---

## Meta-Progression

> *(Framework for future development — currently the foundations are in place)*

Meta-progression refers to permanent unlocks that persist across runs, outside of any single run's deck or relics.

### Current Implementation

In the prototype, meta-progression is implicit — the player accumulates knowledge of enemy patterns, card priorities, and relic synergies. The game does not currently save cross-run progress to storage.

### Planned Progression Systems

#### Unlock Tiers
Each floor cleared for the first time would unlock content:

| Milestone | Unlock |
|---|---|
| Defeat Floor 1 | Uncommon card pool added to reward rotation |
| Defeat Floor 2 | Rare card pool added |
| Defeat Floor 3 | Hard mode modifier unlocked |
| Defeat Floor 4 (Inbox Zero) | New starting relic options |
| First death | "Deleted Items" graveyard — view all past run decks |

#### The Unread Counter
A persistent counter tracking total unread emails defeated across all runs. At milestone thresholds, new enemy types and card types would unlock:

| Unreads Defeated | Unlock |
|---|---|
| 50 | Auto-Reply card |
| 100 | Phishing Attempt enemy |
| 250 | The Group Chat enemy (Floor 2 variant) |
| 500 | Alt inbox — "The Work Slack" mode |

#### Daily Inbox
A seeded daily run where all players face the same enemies and reward offerings. Leaderboard scored by: damage dealt, cards played, HP remaining, floors cleared.

---

## Playthrough Walkthrough

A sample successful run, annotated.

---

### Run Start

Deck begins as:
- Reply Politely ×2
- Archive It ×2
- Hard Delete ×1
- Mark as Read ×2
- Unsubscribe ×1
- Set Filter ×1

Starting relic: 📎 Paperclip

---

### Floor 1, Room 1 — Newsletter Flood (28 HP, 5 dmg/turn)

**Turn 1** *(4 AP from Paperclip)*
- Play Mark as Read (0 AP) → draw Unsubscribe
- Play Unsubscribe (1 AP) → deal 5 dmg, draw Archive It + Reply Politely
- Play Hard Delete (2 AP) → deal 14 dmg
- Play Reply Politely (1 AP) → deal 6 dmg
- *Total: 25 damage dealt. Enemy at 3 HP.*

**Turn 2** *(took 5 dmg, now at 45 HP)*
- Play any attack → kill

**Reward:** Choose **Forward Bomb** (deal 10 + apply Unread)

---

### Floor 1, Room 2 — Calendar Invite (22 HP, 7 dmg/turn)

**Turn 1** *(3 AP)*
- Play Forward Bomb (2 AP) → deal 10 dmg, apply Unread
- Play Reply Politely (1 AP) → deal 6 dmg
- Enemy at 6 HP. Enemy is Unread — skips turn.

**Turn 2** *(no incoming damage last turn, still at 45 HP)*
- Kill with any card.

**Reward:** Choose **Snooze 7 Days** (draw 2, 0 AP)

---

### Floor 1, Room 3 — Rest Stop

Restore 15 HP. Now at 60 HP (technically above starting HP due to rounding — note for balance).

> *Design note: Rest stops should not heal above max HP. Known edge case.*

---

### Floor 1, Room 4 — Newsletter Flood again

Quick kill. No drama.

**Reward:** Choose **Keyboard Shortcut** (gain 2 AP, 0 AP cost)

**End of Floor 1 — Relic:** 🗂 Inbox Zero Badge (attack cards +2 dmg)

---

### Floor 2, Room 1 — Reply-All Demon (40 HP, 9 dmg/turn)

Deck now has Forward Bomb, Snooze, Keyboard Shortcut. This fight is manageable.

**Turn 1** *(3 AP)*
- Keyboard Shortcut (0 AP) → +2 AP = 5 AP available
- Forward Bomb (2 AP) → 12 dmg (10 + Badge bonus), apply Unread
- Hard Delete (2 AP) → 16 dmg (14 + Badge bonus)
- Mark as Read (0 AP) → 4 dmg (2 + Badge bonus), draw Set Filter
- *Total: 32 damage. Enemy at 8 HP. Enemy is Unread — skips turn.*

**Turn 2:**
- Kill.

**Reward:** Choose **Start New Thread** (12 dmg + draw 2 + +1 AP)

---

### Floor 2, Room 2 — Auto-CC Manager (35 HP, 8 dmg/turn)

Start New Thread is now in the deck. New combo unlocked.

**Turn 1:**
- Start New Thread (2 AP, refund 1) → 14 dmg, draw 2 cards including CC the CEO (wait — not in deck yet)
- Draws bring Hard Delete + Forward Bomb
- Forward Bomb (2 AP) → 12 dmg, Unread
- *Total: 26 dmg. Enemy at 9 HP. Frozen.*

**Turn 2:** Kill.

**Reward:** Choose **CC the CEO** (deal 18 dmg)

**End of Floor 2 — Relic:** ☕ Cold Coffee (+3 HP per turn)

---

### Floor 3, Room 1 — Passive-Aggressive Karen (45 HP, 11 dmg/turn, regen 4/turn)

This is the test. You have a strong deck now.

Key insight: Karen regens 4 HP per enemy turn. You must burst her down before regen compounds.

**Turn 1** *(3 AP)*
- Keyboard Shortcut (0) → +2 AP = 5 total
- Start New Thread (net 1 AP) → 14 dmg, draw 2
- CC the CEO (2 AP) → 20 dmg
- Mark as Read (0) → 4 dmg, draw 1
- *Total: 38 damage. Karen at 7 HP. She attacks for 11, you take 11 → 49 HP. Karen regens to 11 HP.*

**Turn 2:**
- Any two cards kill her.

**Reward:** Choose **Recall Email** (heal 5 HP + draw 1)

---

### Floor 3, Room 2 — Out-of-Office Loop (50 HP, 10 dmg/turn, applies Awaiting Reply)

Use Forward Bomb immediately to freeze it, then burst.

---

### Floor 3, Room 3 — Rest Stop

Heal 15 HP. Entering boss floor with solid HP and a tuned deck.

---

### Floor 4, Room 1 — THE THREAD THAT NEVER ENDS (80 HP, 14 dmg/turn, regen 5/turn)

The deck at this point: Reply Politely ×2, Archive It ×2, Hard Delete, Mark as Read ×2, Unsubscribe, Set Filter, Forward Bomb, Snooze 7 Days, Keyboard Shortcut, Start New Thread, CC the CEO, Recall Email.

Relics: 📎 Paperclip, 🗂 Inbox Zero Badge, ☕ Cold Coffee

**Strategy:** Freeze → draw engine → burst

**Turn 1** *(4 AP from Paperclip)*
- Forward Bomb → 12 dmg, apply Unread. Boss frozen.
- Start New Thread → 14 dmg, draw 2, +1 AP (3 AP now)
- Keyboard Shortcut → +2 AP (5 AP)
- CC the CEO → 20 dmg
- *Total: 46 dmg. Boss at 34 HP.*

**Enemy turn:** Boss is Unread. Skips. Cold Coffee restores 3 HP.

**Turn 2** *(3 AP)*
- Boss still Unread (1 turn remaining)
- Start New Thread → 14 dmg, draw 2
- CC the CEO → 20 dmg
- *Total: 34 dmg. Boss at 0 HP.*

**INBOX ZERO ACHIEVED.**

---

## Strategies & Tips

### Deck Building Principles

**1. Keep your deck small**
Every card you add dilutes the deck. Adding 10 mediocre cards to reach a good one means you'll draw that good card less often. Ideal deck size: 12–15 cards.

**2. Zero-cost cards are broken**
Mark as Read, Snooze 7 Days, and Keyboard Shortcut cost 0 AP. These cards are never dead draws. Prioritize them in rewards.

**3. Engine before damage**
Early floors are survivable. Use early reward picks to build your draw engine (Unsubscribe, Start New Thread, Snooze 7 Days). Damage cards become dominant once you can reliably draw them.

**4. One shield strategy is enough**
Don't fill your deck with shield cards — they don't kill enemies. One strong shield turn per fight (Vacation Autoresponder or Set Filter × 2) buys enough time. The rest should be damage.

**5. Never skip Forward Bomb**
If Forward Bomb appears as a reward, take it unless your deck already has a copy. It is the only source of Unread, and Unread wins boss fights.

### Floor-Specific Tips

| Floor | Priority |
|---|---|
| Floor 1 | Build draw engine. Take Unsubscribe or Snooze 7 Days over damage. |
| Floor 2 | Take Forward Bomb immediately. Start New Thread if available. |
| Floor 3 | You need burst. CC the CEO and Hard Delete become critical. |
| Floor 4 | Don't take any more cards. Deck should be tuned. Skip reward and go. |

### HP Management

- You start with 50 HP. The game deals approximately 60–80 HP of unavoidable damage across a clean run.
- You need healing. Recall Email and Rest Stops are the only HP sources.
- If you reach Floor 3 below 25 HP, prioritize Rest Stops over fight shortcuts.
- Cold Coffee (+3 HP/turn) across a 5-turn floor 3 fight recovers 15 HP — equivalent to a Rest Stop.

---

## Glossary

| Term | Definition |
|---|---|
| **AP** | Action Points. Spent to play cards. Resets to 3 (or more with relics) at start of each turn. |
| **Burst** | Dealing large damage in a single turn, typically through card combos. The primary win condition against regen enemies. |
| **Deck** | Your full collection of cards in a run. Includes draw pile, hand, and discard pile. |
| **Discard pile** | Cards played this turn (or previous turns after reshuffling). Reshuffled into draw pile when draw pile empties. |
| **DoT** | Damage over Time. Status effects like Guilt that deal recurring damage each turn. |
| **Draw pile** | Cards not yet drawn this run cycle. Drawn 5 at a time at start of turn. |
| **Draw engine** | A combination of zero-cost or low-cost cards that draw more cards, creating cascading turns. |
| **Elite encounter** | A harder variant encounter that appears mid-floor. Higher HP, damage, or regen. Yields better rewards. |
| **Floor** | One of 4 zones in the game. Each floor contains 4 rooms. Difficulty escalates per floor. |
| **Hand** | Cards available to play this turn. Drawn at turn start, discarded at turn end. |
| **HP** | Hit Points. Reaches 0 = run over. |
| **Inbox Zero** | The win state. Reached by defeating the Final Boss on Floor 4. A mythical condition. |
| **Meta-progression** | Persistent unlocks that carry over between runs, outside any single run's deck. |
| **Relic** | A passive item that provides a permanent benefit for the duration of the run. |
| **Regen** | HP regeneration. Some enemies restore HP at the end of their turn. Requires burst damage to overcome. |
| **Rest Stop** | A non-combat room that restores 15 HP. Appears randomly on the floor map. |
| **Reward tier** | The quality level of cards offered as rewards after encounters (Common, Uncommon, Rare). |
| **Roguelite** | A game with roguelike-inspired elements (randomness, permadeath, run structure) plus meta-progression between runs. |
| **Room** | A single encounter within a floor. 4 rooms per floor. |
| **Run** | A full playthrough attempt from start to win or death. Each run starts fresh. |
| **Shield** | Temporary HP that absorbs incoming damage. Resets to 0 at end of each turn. Does not carry over. |
| **Status effect** | A temporary condition applied to player or enemy. Ticks down by 1 per applicable turn. |
| **Unread** | A status effect that freezes an enemy, preventing it from attacking. Applied by Forward Bomb. |
| **Awaiting Reply** | A status effect applied to the player by the Out-of-Office Loop. Reduces available AP next turn. |
| **Guilt** | A status effect dealing 2 damage per turn. Can be applied to either player or enemy depending on source. |

---

## Design Notes

### Why Email?

The inbox maps almost perfectly to roguelite mechanics. It has:
- A **queue** of threats that grows if ignored (the deck / encounter pool)
- **Resource management** — attention (AP), capacity (hand size), energy (HP)
- **Threat categorization** — bulk mail (weak enemies), threads (elites), escalations (bosses)
- **Status effects** as email behaviors — being "Unread" freezes, being "Awaiting Reply" drains focus
- **Meta-progression** as actual email hygiene — filters, unsubscribes, and habits persist

The comedy is built into the premise. Every mechanic doubles as an email joke.

### Balance Philosophy

The game intentionally front-loads learning. Floor 1 is almost impossible to lose. Floor 3 is almost impossible to win without proper deck construction. The difficulty curve is steep and punishing — by design. Inbox Zero is supposed to feel impossible until it suddenly isn't.

Known balance issues in the current prototype:
- Rest Stops can heal above max HP (edge case)
- Awaiting Reply currently reduces AP inconsistently
- Paperclip bonus on Floor 1 Room 1 doesn't account for subsequent rooms
- No upper cap on AP (Keyboard Shortcut can stack beyond intent)

### Future Cards Under Consideration

| Card | Type | Concept |
|---|---|---|
| Unilateral Reply | Attack | Deal damage to all enemies (for future multi-enemy rooms) |
| The Attachment | Special | Deals increasing damage the longer the run goes |
| Read Receipt | Special | Forces enemy to reveal their next attack value |
| Legal CC | Attack | Deal damage that cannot be reduced by any effect |
| Out of Office | Defend | Skip the enemy's next attack; costs HP to play |
| The Cold Open | Special | First card played each turn costs 0 AP |
| Thread Summary | Special | Combine all discarded cards into one mega-attack |

### Future Enemies Under Consideration

| Enemy | Mechanic |
|---|---|
| The Group Chat | Splits into 3 weaker enemies on defeat |
| Phishing Attempt | Copying your last card and using it against you |
| The LinkedIn Request | Cannot be killed; must be archived (special mechanic) |
| Automated Survey | Applies Awaiting Reply until you "complete" it |
| The Boomerang Email | Returns to the top of the enemy deck after defeat |

---

*INBOX//ZERO v0.1 — Built as a roguelite prototype.*
*"You will not achieve inbox zero. But you will try."*
