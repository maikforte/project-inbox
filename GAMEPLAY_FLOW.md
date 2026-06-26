# INBOX//ZERO — Complete Gameplay Flow

A narrative walkthrough of the full player experience, from the title screen to Inbox Zero (or death).

---

## 1. The Main Menu

The player lands on the **Main Menu**. This is the meta-game hub — everything here persists across all runs.

From here the player can:
- **Start Run** — begins a fresh run
- **All Mail** — opens the card compendium to review and configure the reward pool
- **Options / Exit**

Nothing on the main menu is tied to any active run. It is purely about configuration and meta-progression.

---

## 2. Configuring the Card Pool (All Mail)

Before starting, the player opens **All Mail** — a scrollable compendium of every card in the game.

### What the player sees
Each card is shown as a full card tile. Cards are grouped by rarity. Each unlocked card has an **ON/OFF toggle**.

- **Locked cards** (not yet unlocked) are shown greyed out with a `LOCKED` label. They cannot be toggled.
- **Starter cards** are always ON and cannot be toggled off. They are not part of the reward pool — they just inform the player what is in the starting deck.
- **Unlocked cards** can be freely toggled ON or OFF to include or exclude them from the reward pool for future runs.

### The pool toggle rules

The game enforces minimum pool sizes to prevent degenerate configurations:

| Rarity | Minimum enabled |
|---|---|
| Common | 4 |
| Uncommon | 3 |
| Rare | 1 |

If toggling a card OFF would violate the minimum, the toggle is rejected and a warning message flashes: `NEED 4 COMMON ENABLED` or similar. The player cannot drop below these floors.

Starter cards have their own rule: they cannot be disabled at all. The label reads `STARTER ALWAYS ON`.

### What this means in practice

This is the **Megabonk-style card pool system**. It works like Monster Train's deck-shaping: rather than banning specific synergies, the player shapes the probability space of what rewards can appear. Turning off a card you find weak or redundant means it will never show up as a reward option, tightening the pool toward cards you actually want.

This configuration is **persistent across runs** and saved immediately on toggle. The player does not need to confirm or save manually.

---

## 3. Starting a Run

The player presses **Start Run**.

The game initialises a fresh run:
- HP resets to maximum (100)
- The deck is loaded with the **9 starter cards** (Reply Politely ×2, Archive It ×2, Hard Delete, Mark as Read ×2, Unsubscribe, Set Filter)
- The starting relic **Paperclip** is placed in the active relic slot (grants +1 AP on the first turn of every combat)
- Floor 1 is generated: four combat encounters are drawn from the Floor 1 enemy pool and arranged as email rows in the inbox

The run begins and the **Inbox (Floor Map)** screen appears.

---

## 4. The Inbox — The Run's Navigation Hub

The **Inbox** is the run's map. It looks like a Gmail inbox. Each row is an email. Each email is an encounter.

On Floor 1, the player sees four rows — for example:

```
[ NEWSLETTER FLOOD      ]  Common enemy   · 28 HP · 5 dmg/turn    [×0]
[ CALENDAR INVITE       ]  Common enemy   · 22 HP · 7 dmg/turn    [×0]
[ REST STOP             ]  Recover HP
[ NEWSLETTER FLOOD      ]  Common enemy   · 28 HP · 5 dmg/turn    [×0]
```

The `[×0]` badge is the escalation counter — it increments each time that row is deferred. The player clicks any row to enter that encounter.

The player may click any row **in any order** — there is no forced sequence. However, leaving a row unaddressed has consequences (see Escalation below).

The sidebar has additional tabs representing optional side paths. The player can ignore side tabs entirely and clear the Inbox tab only.

---

## 4b. The Escalation System

Every time the player completes **any encounter** (combat, rest stop, or side-path event), all **unaddressed Inbox rows** escalate by one tier:

- The enemy's **damage per turn increases**
- The enemy's **HP increases**
- If the enemy already had regen, its regen amount increases
- Escalation is shown visually on the inbox row — a counter or colour shift indicating how many times that row has been deferred

### Escalation tiers

| Escalations | Damage modifier | HP modifier |
|---|---|---|
| 0 (base) | ×1.0 | ×1.0 |
| 1 | ×1.2 | ×1.15 |
| 2 | ×1.4 | ×1.30 |
| 3 | ×1.6 | ×1.50 |
| 4+ | ×1.8 (cap) | ×1.70 (cap) |

### The strategic tension

The escalation system creates meaningful decisions:
- **Rushing difficult rows** (e.g. clearing the hardest row first) locks in lower stats but means fighting it without warm-up cards from easier fights.
- **Clearing easy rows first** builds the deck but risks inflating a dangerous row to a nearly unkillable state.
- **Side paths** (Spam, Important) do not escalate Inbox rows — but completing them still costs a "turn", during which Inbox rows escalate.

The intended pacing: clear the row you can beat right now before it becomes the row that kills you later.

---

## 4c. The Side Tabs

Each tab in the inbox sidebar is a different flavour of optional side path. Side-tab rows do not escalate (they are not threats on a timer), but completing them advances the escalation counter for all unaddressed Inbox rows.

### Spam

High-risk, high-reward ambush fights. Spam enemies have higher base stats than their tier equivalent in the main inbox but drop better card rewards — one rarity tier higher than normal.

Example encounters:
- **Unsubscribe Request Denied** — a Common enemy with Uncommon-tier HP and damage. Drops an Uncommon card on victory.
- **Nigerian Prince** — an Uncommon enemy with Rare-tier stats. Drops a Rare card on victory.

Clearing a Spam row is never required. It is a bet: risk HP for a better reward.

### Important

Flagged high-priority encounters. These are Rare-tier threats that appear mid-run — tougher than the standard Inbox path but carrying the best non-boss rewards: **a choice between two relics** rather than a card draw.

Important rows appear once per floor (if any). They do not escalate and are not mandatory.

**Example encounters:**
- **The Urgent Request** — an elite variant with high damage, applies Guilt to the player on attack. Reward: choose 1 of 2 relics.
- **The Escalation Chain** — an Out-of-Office Loop variant that applies both Awaiting Reply and Unread-lock mechanic to the player simultaneously.

### Sent

Events and narrative choices. No combat. The player reads a short prompt and picks from two or three outcomes:

- *"Reply All to the company-wide chain?"* → Gain a card | Take 5 damage | Apply Guilt to the next enemy
- *"Accept the calendar hold?"* → Gain 10 shield for the next combat | Lose 5 max HP for this floor

Sent rows are low-risk, situational. Good when you are healthy and want to fish for a specific effect.

### Promotions

Shops and relic offers. The player is shown purchasable items using a **Favour** currency (earned by clearing combat rows):

| Item | Cost |
|---|---|
| Remove a card from your deck permanently | 2 Favour |
| Buy a random Common card | 1 Favour |
| Buy a random Uncommon card | 2 Favour |
| Buy a random Relic | 3 Favour |

Promotions rows appear once or twice per floor. Useful for deck thinning (removing weak starters) or picking up a relic slot you missed.

---

## 5. Combat — Turn by Turn

The player clicks a combat row. The combat screen loads. The enemy appears.

### Combat start
- The deck is shuffled
- The player draws **3 cards** immediately (no special opening hand — every turn works the same way)
- AP is set to 3 (or 4 on turn 1 if Paperclip relic is active)
- The enemy telegraphs its intent card (what it will do on its turn)

### The player's turn

The player has 3 AP and 3 cards in hand. They spend AP to play cards:

- **Attack cards** (red) deal damage to the enemy
- **Defend cards** (green) add shield — shield absorbs incoming damage and resets to 0 at end of turn
- **Special cards** (purple) draw more cards, gain AP, apply status effects, or restore HP

Playing a card immediately moves it to the discard pile. AP is deducted. The hand updates. If a card draws additional cards (e.g. Unsubscribe draws 2), those cards appear immediately and can be played in the same turn.

When the player is done, they press **End Turn**.

### End of player turn
- All remaining cards in hand (unplayed) are discarded
- The hand is cleared — nothing carries over to the next turn
- Enemy turn begins

### The enemy's turn
The enemy plays the card it telegraphed:
- If its intent was an attack, the player takes damage (shield absorbs first, remainder hits HP)
- If it had a status effect (e.g. Awaiting Reply), it is applied to the player
- If the enemy was **Unread** (frozen), it skips its action entirely

After acting:
- Enemy regen triggers (if any) — the enemy heals
- Enemy shield resets to 0
- All status durations on the enemy tick down by 1. Expired statuses are removed
- The enemy draws its next intent card and telegraphs it for the upcoming turn

### Start of next player turn
- If the draw pile has **≤ 2 cards** and the discard pile is non-empty, the discard pile is shuffled back into the draw pile before drawing
- 3 new cards are drawn from the (now replenished) draw pile
- AP resets to 3
- Start-of-turn status effects process (Guilt damage, AwaitingReply AP reduction)
- Start-of-turn relic effects process (Cold Coffee: +3 HP)

This loop continues until either the enemy dies or the player's HP reaches 0.

### Status effects in combat

**Applied to the enemy:**
- **Unread** — enemy skips its attack turn. Applied by Forward Bomb (2 turns). The most powerful status in the game.
- **Guilt** — enemy takes 2 damage per stack at the start of its turn. Stacks additively — multiple applications add duration. Applied by Report as Spam.

**Applied to the player:**
- **Awaiting Reply** — reduces available AP on the next turn. Applied by Out-of-Office Loop every attack.
- **Guilt (player)** — player takes 2 damage per stack at start of their turn.

---

## 6. Victory — Inbox Cleared

When the enemy's HP reaches 0, combat ends. The **Victory panel** appears: `INBOX CLEARED`.

The player clicks **Continue**.

---

## 7. The Card Reward Screen

The card reward screen appears. Three cards are displayed, drawn from the reward pool based on the defeated enemy's **reward tier**:

| Enemy Tier | Cards Offered |
|---|---|
| Common | 3 Common cards |
| Uncommon | Mix of Common and Uncommon |
| Rare | Mix of Uncommon and Rare |
| Boss | Rare cards guaranteed |

### How the pool is built at runtime
The game queries the **AllCardsRegistry** (every card in the game) and filters it through the **UnlockManager**:

1. Only cards matching the allowed rarities for this tier are considered
2. Starter cards are excluded (they are never rewards)
3. Only **unlocked** cards are included
4. Only **enabled** cards (those the player left ON in All Mail) are included
5. The filtered pool is shuffled and 3 cards are picked

This means the pool toggle the player set up before the run actively affects which cards can appear here. Disabled cards are never drawn.

### Deck composition check
Each of the 3 offered cards is evaluated against the current deck:

- **Max deck size: 15 cards** — if the deck is already at 15, no card can be added
- **Uncommon limit: 4** — if the deck already has 4 Uncommons, Uncommon cards show as unselectable
- **Rare limit: 2** — same for Rares
- **Legendary limit: 1** — same for Legendaries
- **Duplicate Rare/Legendary** — you may never hold more than 1 copy of the same Rare or Legendary

Cards that cannot be added appear greyed out with a red reason label (`DECK FULL`, `MAX UNCOMMONS`, etc.). The player cannot click them.

### Picking a card
The player clicks a selectable card. That card is **immediately added to their draw pile**. It will appear in future combats this run.

The player can also click **SKIP** to decline all three and take nothing.

After picking or skipping, the card reward screen closes and the Inbox returns.

---

## 8. Unlock Triggers (After Every Combat)

When the combat is finalized (after the reward is picked or skipped), the game fires unlock triggers based on the defeated enemy's tier:

| Enemy Defeated | Permanent Unlocks |
|---|---|
| Any Common-tier enemy | (Common cards are always unlocked — no change) |
| Any Uncommon-tier enemy | All Uncommon cards unlock permanently |
| Any Rare-tier enemy | All Uncommon + Rare cards unlock permanently |
| Boss | All Uncommon + Rare + Legendary cards unlock permanently |

Additionally, each specific enemy has a **signature card** — a card permanently unlocked the first time that enemy is defeated:

| Enemy | Signature Unlock |
|---|---|
| Reply-All Demon | Forward Bomb |
| Auto-CC Manager | CC the CEO |
| Out-of-Office Loop | Vacation Autoresponder |
| Passive-Aggressive Karen | Report as Spam |
| The Thread That Never Ends | Start New Thread |

These unlocks are saved to PlayerPrefs immediately and persist between runs. On the next run (or after the current reward screen), newly unlocked cards become available in the enabled pool and will appear in All Mail with their toggles active.

---

## 9. Back to the Inbox

After the reward screen, the Inbox reloads with the completed encounter removed. All remaining unaddressed Inbox rows tick up one escalation level. The player picks the next row.

If a **Rest Stop** row is selected, the rest stop screen opens:
- The player restores a fixed amount of HP
- No card choice, no combat
- The rest stop is consumed and removed from the inbox

The player continues clearing rows until all encounters on the floor are defeated.

---

## 10. Floor Transition

When the last inbox row is cleared, the **Floor Transition screen** appears: `LEVEL X CLEARED`.

If a relic award is due (after Floor 2 or 3), a relic is offered. The player picks it and it is active for the rest of the run.

The player clicks **Continue**. The next floor's inbox is generated from a harder enemy pool. The run continues.

---

## 11. The Four Floors

| Floor | Theme | Enemy Damage | Notable Mechanic |
|---|---|---|---|
| 1 | The Inbox | 5–7/turn | Tutorial-difficulty. Build the draw engine here. |
| 2 | The Threads | 8–9/turn | Regen enemies appear. First relic awarded after clearing. |
| 3 | The Escalations | 10–11/turn | Awaiting Reply and high regen. Second relic awarded after clearing. |
| 4 | The Final Thread | 14/turn + 5 regen | One encounter: the boss. No relic. Win or die. |

Each floor has four rows, except Floor 4 which has one encounter.

---

## 12. The Boss — The Thread That Never Ends

Floor 4 is a single encounter against the final boss (80 HP, 14 damage/turn, 5 HP regen per enemy turn).

The boss has no special mechanics beyond raw numbers. The challenge is math: the player must deal roughly 100–110 effective damage before their HP reaches 0, accounting for boss regen.

The intended winning strategy:
1. Apply **Unread** (Forward Bomb) to freeze the boss for 2 turns
2. Chain draw effects and AP generators to dump maximum damage while frozen
3. Shield to survive turns where the freeze has lapsed

If the boss dies — **INBOX ZERO**. The victory screen appears and the run ends.

---

## 13. Game Over

If the player's HP reaches 0 at any point, the **Game Over** screen appears. The run is over. All in-run progress (deck, relics, floor, room) is lost.

However, any **unlocks triggered during the run are kept** — if the player defeated an Uncommon-tier enemy before dying, Uncommon cards remain unlocked permanently. The pool configuration in All Mail reflects those new unlocks on the next run.

The player clicks **Restart** and returns to the main menu to start fresh.

---

## 14. Between Runs — The Compounding Meta Loop

Each run, the player's permanent state grows:

1. **More unlocked cards** appear in All Mail after defeating higher-tier enemies
2. **Signature cards** unlock after first-kill milestones, adding powerful specific cards to the pool
3. **Pool configuration** can be refined — the player can now disable cards that cluttered earlier pools

This creates a compounding loop: early runs unlock content, later runs are shaped by deliberate pool curation. A player who turns off weaker Commons tightens the pool so reward draws more consistently hit impactful cards.

The endgame of the meta is a fully unlocked card pool with a precisely curated enabled set — every reward draw is useful, every run has a coherent plan.

---

## Quick Reference: The Full Flow

```
MAIN MENU
  → All Mail: review card pool, toggle ON/OFF
  → Start Run

RUN START
  → Deck initialised with 9 starter cards + Paperclip relic
  → Floor 1 inbox generated (4 encounters + side tabs populated)

PER ENCOUNTER (any tab)
  → Click any inbox row in any order
  → After each encounter: all unaddressed INBOX rows escalate (+1 tier)
  → Side-tab rows (Spam, Important, Sent, Promotions) do not escalate

COMBAT
  → Draw 3 cards, gain 3 AP (4 on turn 1 with Paperclip)
  → Play cards, spend AP
  → End turn: unplayed cards discarded, enemy acts
  → Repeat until enemy dead or player dead

ON VICTORY
  → Victory panel: INBOX CLEARED
  → Continue → Card Reward Screen (3 cards from filtered pool)
  → Pick 1 card (or skip) → card added to draw pile
  → Unlock triggers fire (rarity + signature unlocks saved to PlayerPrefs)
  → Return to inbox, cleared encounter removed, remaining rows escalate

SIDE PATHS
  → Spam: harder fight, better card drop (one rarity tier up)
  → Important: elite fight, reward is a relic choice (not a card)
  → Sent: narrative event, pick an outcome
  → Promotions: shop — spend Favour to buy cards or relics, remove cards

PER FLOOR
  → Clear all INBOX rows (side tabs are optional)
  → Floor Transition screen
  → Relic award (Floor 2 and 3 only)
  → Next floor's inbox generated

FLOOR 4
  → Single boss encounter (no side tabs)
  → Win → INBOX ZERO, run ends
  → Lose → GAME OVER, return to main menu (unlocks kept)
```
