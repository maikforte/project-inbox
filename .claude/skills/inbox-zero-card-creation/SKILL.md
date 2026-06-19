---
name: inbox-zero-card-creation
description: Workflow for adding or editing INBOX//ZERO CardData ScriptableObjects (the roguelite's deck-building cards). Use whenever the task is "add a card", "change a card's damage/cost/effect", or "regenerate the card registry" in this Unity project.
---

# Card Creation Workflow

Cards are never hand-authored as `.asset` files. They are generated in bulk from one source-of-truth C# table.

## Where things live
- Source table: `Assets/Editor/CardBatchCreator.cs` — a static `Def[] AllCards()` array.
- Generated assets: `Assets/Data/Cards/<File>.asset` (one `CardData` per entry).
- Registry: `Assets/Data/AllCardsRegistry.asset` (a `CardData` list, used by `AllMailScreen` for the compendium).
- Data model: `Assets/Scripts/Data/CardData.cs`, `Assets/Scripts/Data/CardEffect.cs`, `Assets/Scripts/Data/Enums.cs`.

## To add or change a card
1. Edit the `Def[] AllCards()` array in `CardBatchCreator.cs`. Each entry:
   ```csharp
   new Def {
       file="UniqueFileName",          // asset filename, no spaces
       name="Display Name",
       type=CardType.Attack,           // Attack | Defend | Special
       rarity=CardRarity.Common,       // Starter | Common | Uncommon | Rare | Legendary
       character=CardCharacter.All,    // All | Intern | Manager | Lawyer | Dev | Ghost
       ap=1,
       desc="Player-facing effect text.",
       flavor="Flavor text.",
       fx=new[]{ new E(CardEffectType.DealDamage, 6) }
   }
   ```
2. `E` (effect) constructor: `new E(type, value, statusType = StatusEffectType.None, statusDuration = 0)`.
   - `CardEffectType`: `DealDamage`, `GainShield`, `DrawCards`, `GainAP`, `ApplyStatusToEnemy`, `ApplyStatusToPlayer`, `RestoreHP`, `HealSelf`.
   - `StatusEffectType`: `None`, `Unread`, `Guilt`, `AwaitingReply`.
   - For status-applying effects, `value` is the stack count and the last two args are the status type + duration, e.g. `new E(CardEffectType.ApplyStatusToEnemy, 1, StatusEffectType.Unread, 2)`.
   - A card with no mechanical effect yet (description-only / not-yet-implemented mechanic) uses `fx=None` (the static empty array) — this is an existing pattern in the table, not an error.
3. In Unity: **InboxZero → Create All Cards**. This creates new `CardData` assets or updates existing ones (matched first by filename, then by `cardName` if the file is missing), then automatically calls `RebuildRegistry()`.
4. If you only reordered/renamed without changing `AllCards()` (e.g. after manually deleting a stray asset), you can run **InboxZero → Rebuild Cards Registry** alone — it re-scans `Assets/Data/Cards` for all `CardData` assets and re-sorts the registry by rarity then name.
5. Ctrl+R, check Console for compile errors, then Play to verify the card shows up with correct text/cost/type-bar color.

## Rules to respect (see CLAUDE.md for full design doc)
- Max deck size 15; Uncommon ≤4 copies, Rare ≤2, Legendary ≤1 (Starter/Common unlimited) — these are deck-building constraints, not something `CardBatchCreator` enforces.
- Character-locked cards (`character` != `All`) gate behind specific in-run unlock conditions documented in CLAUDE.md's "Character-tier unlocks" table — don't add new ones without checking that table.
- Never hand-edit a generated `.asset` file's fields directly if the same change can go in the `Def` table — the next `Create All Cards` run would silently overwrite it.
