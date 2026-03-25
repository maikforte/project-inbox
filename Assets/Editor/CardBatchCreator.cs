using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InboxZero.Data;

namespace InboxZero.Editor
{
    public static class CardBatchCreator
    {
        const string CardsFolder = "Assets/Data/Cards";

        struct E
        {
            public CardEffectType t; public int v; public StatusEffectType s; public int d;
            public E(CardEffectType t, int v, StatusEffectType s = StatusEffectType.None, int d = 0)
            { this.t = t; this.v = v; this.s = s; this.d = d; }
        }

        struct Def
        {
            public string name, desc, flavor, file;
            public CardType type; public CardRarity rarity; public CardCharacter character;
            public int ap;
            public E[] fx;
        }

        static readonly E[] None = new E[0];

        [MenuItem("InboxZero/Create All Cards")]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder(CardsFolder))
                AssetDatabase.CreateFolder("Assets/Data", "Cards");

            var defs = AllCards();
            int created = 0, updated = 0;

            foreach (var def in defs)
            {
                string path = $"{CardsFolder}/{def.file}.asset";
                var asset = AssetDatabase.LoadAssetAtPath<CardData>(path);

                if (asset == null)
                {
                    // Search by cardName in case filename differs from convention
                    foreach (string guid in AssetDatabase.FindAssets("t:CardData", new[] { CardsFolder }))
                    {
                        var cd = AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(guid));
                        if (cd != null && cd.cardName == def.name) { asset = cd; break; }
                    }
                }

                if (asset != null)
                {
                    Apply(asset, def);
                    EditorUtility.SetDirty(asset);
                    updated++;
                }
                else
                {
                    asset = ScriptableObject.CreateInstance<CardData>();
                    Apply(asset, def);
                    AssetDatabase.CreateAsset(asset, path);
                    created++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CardBatchCreator] Done — {created} created, {updated} updated.");

            RebuildRegistry();
        }

        [MenuItem("InboxZero/Rebuild Cards Registry")]
        public static void RebuildRegistry()
        {
            var registry = AssetDatabase.LoadAssetAtPath<AllCardsRegistry>("Assets/Data/AllCardsRegistry.asset");
            if (registry == null)
            {
                Debug.LogError("[CardBatchCreator] AllCardsRegistry.asset not found at Assets/Data/AllCardsRegistry.asset");
                return;
            }

            registry.allCards.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:CardData", new[] { CardsFolder }))
            {
                var card = AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(guid));
                if (card != null) registry.allCards.Add(card);
            }

            // Sort by rarity then name for a consistent order
            registry.allCards.Sort((a, b) =>
            {
                int r = a.rarity.CompareTo(b.rarity);
                return r != 0 ? r : string.Compare(a.cardName, b.cardName, System.StringComparison.Ordinal);
            });

            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CardBatchCreator] Registry rebuilt — {registry.allCards.Count} cards.");
        }

        static void Apply(CardData c, Def d)
        {
            c.cardName         = d.name;
            c.cardType         = d.type;
            c.rarity           = d.rarity;
            c.character        = d.character;
            c.apCost           = d.ap;
            c.effectDescription = d.desc;
            c.flavorText       = d.flavor;
            c.effects          = new List<CardEffect>();
            foreach (var e in d.fx)
                c.effects.Add(new CardEffect { effectType = e.t, value = e.v, statusType = e.s, statusDuration = e.d });
        }

        static Def[] AllCards() => new Def[]
        {
            // ── Starter ──────────────────────────────────────────────────────────
            new Def { file="ReplyPolitely",     name="Reply Politely",      type=CardType.Attack,   rarity=CardRarity.Starter,   character=CardCharacter.All, ap=1, desc="Deal 6 damage.",                              flavor="Professionally seething.",                                      fx=new[]{new E(CardEffectType.DealDamage,6)} },
            new Def { file="ArchiveIt",         name="Archive It",          type=CardType.Defend,   rarity=CardRarity.Starter,   character=CardCharacter.All, ap=1, desc="Gain 8 shield.",                              flavor="Out of sight, out of mind.",                                    fx=new[]{new E(CardEffectType.GainShield,8)} },
            new Def { file="HardDelete",        name="Hard Delete",         type=CardType.Attack,   rarity=CardRarity.Starter,   character=CardCharacter.All, ap=2, desc="Deal 14 damage.",                             flavor="Shift+Delete. No recycle bin.",                                 fx=new[]{new E(CardEffectType.DealDamage,14)} },
            new Def { file="MarkAsRead",        name="Mark as Read",        type=CardType.Special,  rarity=CardRarity.Starter,   character=CardCharacter.All, ap=0, desc="Draw 1 card. Deal 2 damage.",                 flavor="Acknowledged. Ignored.",                                        fx=new[]{new E(CardEffectType.DrawCards,1), new E(CardEffectType.DealDamage,2)} },
            new Def { file="Unsubscribe",       name="Unsubscribe",         type=CardType.Special,  rarity=CardRarity.Starter,   character=CardCharacter.All, ap=1, desc="Deal 5 damage. Draw 2 cards.",                flavor="Click. Click. 'Are you sure?' Click.",                          fx=new[]{new E(CardEffectType.DealDamage,5), new E(CardEffectType.DrawCards,2)} },
            new Def { file="SetFilter",         name="Set Filter",          type=CardType.Defend,   rarity=CardRarity.Starter,   character=CardCharacter.All, ap=1, desc="Gain 12 shield.",                             flavor="Straight to the folder that never opens.",                      fx=new[]{new E(CardEffectType.GainShield,12)} },

            // ── Common (All) ──────────────────────────────────────────────────────
            new Def { file="QuickReply",        name="Quick Reply",         type=CardType.Attack,   rarity=CardRarity.Common,    character=CardCharacter.All, ap=1, desc="Deal 7 damage. If this kills the enemy, draw 1 card.",     flavor="Short, curt, done.",                                fx=new[]{new E(CardEffectType.DealDamage,7), new E(CardEffectType.DrawCards,1)} },
            new Def { file="FlagForFollowUp",   name="Flag for Follow-Up",  type=CardType.Special,  rarity=CardRarity.Common,    character=CardCharacter.All, ap=1, desc="Apply 2 Guilt stacks to the enemy.",                      flavor="Red flag. Metaphorically and literally.",           fx=new[]{new E(CardEffectType.ApplyStatusToEnemy,2,StatusEffectType.Guilt,2)} },
            new Def { file="MoveToFolder",      name="Move to Folder",      type=CardType.Defend,   rarity=CardRarity.Common,    character=CardCharacter.All, ap=1, desc="Gain 10 shield.",                                         flavor="The folder is called 'Later'. It has 847 items.",   fx=new[]{new E(CardEffectType.GainShield,10)} },
            new Def { file="EmptyInbox",        name="Empty Inbox",         type=CardType.Attack,   rarity=CardRarity.Common,    character=CardCharacter.All, ap=2, desc="Deal 10 damage. Gain 5 shield.",                          flavor="Brief. Fleeting. Beautiful.",                       fx=new[]{new E(CardEffectType.DealDamage,10), new E(CardEffectType.GainShield,5)} },
            new Def { file="NoReplyAddress",    name="No-Reply Address",    type=CardType.Special,  rarity=CardRarity.Common,    character=CardCharacter.All, ap=0, desc="Prevent the next 3 damage you would take this turn.",      flavor="This mailbox is not monitored.",                    fx=new[]{new E(CardEffectType.GainShield,3)} },
            new Def { file="ReadLater",         name="Read Later",          type=CardType.Special,  rarity=CardRarity.Common,    character=CardCharacter.All, ap=0, desc="Draw 1 card. Lose 1 HP.",                                 flavor="You won't.",                                        fx=new[]{new E(CardEffectType.DrawCards,1), new E(CardEffectType.ApplyStatusToPlayer,1,StatusEffectType.Guilt,1)} },
            new Def { file="DraftSaved",        name="Draft Saved",         type=CardType.Special,  rarity=CardRarity.Common,    character=CardCharacter.All, ap=1, desc="Draw 2 cards. Discard 1 card.",                           flavor="Autosaved at 11:59 PM. Sent never.",                fx=new[]{new E(CardEffectType.DrawCards,2)} },
            new Def { file="Ping",              name="Ping",                type=CardType.Attack,   rarity=CardRarity.Common,    character=CardCharacter.All, ap=0, desc="Deal 3 damage.",                                          flavor="Just circling back on this.",                       fx=new[]{new E(CardEffectType.DealDamage,3)} },
            new Def { file="DeclineMeeting",    name="Decline Meeting",     type=CardType.Defend,   rarity=CardRarity.Common,    character=CardCharacter.All, ap=1, desc="Gain 14 shield. The enemy skips applying status effects this turn.", flavor="Declined with a note: 'Conflict.'",        fx=new[]{new E(CardEffectType.GainShield,14)} },
            new Def { file="Autoformat",        name="Autoformat",          type=CardType.Special,  rarity=CardRarity.Common,    character=CardCharacter.All, ap=1, desc="The next card you play this turn costs 1 less AP (min 0).", flavor="Ctrl+Shift+F. Instant credibility.",              fx=new[]{new E(CardEffectType.GainAP,1)} },
            new Def { file="OutOfOfficeDraft",  name="Out of Office (Draft)",type=CardType.Defend,  rarity=CardRarity.Common,    character=CardCharacter.All, ap=2, desc="Gain 18 shield.",                                         flavor="Not a lie. Just... aspirational.",                  fx=new[]{new E(CardEffectType.GainShield,18)} },
            new Def { file="SearchBar",         name="Search Bar",          type=CardType.Special,  rarity=CardRarity.Common,    character=CardCharacter.All, ap=1, desc="Look at the top 4 cards of your deck. Draw 1, discard the rest.", flavor="Results: 2,847 emails.",                  fx=new[]{new E(CardEffectType.DrawCards,1)} },
            new Def { file="PrintToPDF",        name="Print to PDF",        type=CardType.Special,  rarity=CardRarity.Common,    character=CardCharacter.All, ap=1, desc="Copy the effect of the last card played.",                 flavor="Why is there a 47-page thread in here?",            fx=None },
            new Def { file="InboxSweep",        name="Inbox Sweep",         type=CardType.Attack,   rarity=CardRarity.Common,    character=CardCharacter.All, ap=2, desc="Deal 8 damage. Remove 1 Guilt stack from yourself.",       flavor="Today I will be a different person.",               fx=new[]{new E(CardEffectType.DealDamage,8)} },
            new Def { file="BCCYourself",       name="BCC Yourself",        type=CardType.Special,  rarity=CardRarity.Common,    character=CardCharacter.All, ap=0, desc="Gain 1 AP.",                                              flavor="Paper trail. Trust no one.",                        fx=new[]{new E(CardEffectType.GainAP,1)} },

            // ── Uncommon (All) ────────────────────────────────────────────────────
            new Def { file="ForwardBomb",       name="Forward Bomb",        type=CardType.Attack,   rarity=CardRarity.Uncommon,  character=CardCharacter.All, ap=2, desc="Deal 10 damage. Apply Unread (2 turns) to the enemy.",      flavor="Forwarded to 40 people. Watch it burn.",        fx=new[]{new E(CardEffectType.DealDamage,10), new E(CardEffectType.ApplyStatusToEnemy,1,StatusEffectType.Unread,2)} },
            new Def { file="Snooze7Days",       name="Snooze 7 Days",       type=CardType.Special,  rarity=CardRarity.Uncommon,  character=CardCharacter.All, ap=0, desc="Draw 2 cards.",                                            flavor="Not a tomorrow problem. A next-week problem.",  fx=new[]{new E(CardEffectType.DrawCards,2)} },
            new Def { file="KeyboardShortcut",  name="Keyboard Shortcut",   type=CardType.Special,  rarity=CardRarity.Uncommon,  character=CardCharacter.All, ap=0, desc="Gain 2 AP this turn.",                                     flavor="Ctrl+Alt+Survive.",                             fx=new[]{new E(CardEffectType.GainAP,2)} },
            new Def { file="CCtheCEO",          name="CC the CEO",          type=CardType.Attack,   rarity=CardRarity.Uncommon,  character=CardCharacter.All, ap=2, desc="Deal 18 damage.",                                          flavor="Suddenly everyone is very available.",          fx=new[]{new E(CardEffectType.DealDamage,18)} },
            new Def { file="ReportAsSpam",      name="Report as Spam",      type=CardType.Attack,   rarity=CardRarity.Uncommon,  character=CardCharacter.All, ap=1, desc="Deal 8 damage. Apply 1 Guilt stack to enemy.",             flavor="Problem solved. Ethically questionable.",       fx=new[]{new E(CardEffectType.DealDamage,8), new E(CardEffectType.ApplyStatusToEnemy,1,StatusEffectType.Guilt,1)} },
            new Def { file="EscalateToManager", name="Escalate to Manager", type=CardType.Attack,   rarity=CardRarity.Uncommon,  character=CardCharacter.All, ap=2, desc="Deal 15 damage. If the enemy is above 50% HP, deal 5 extra damage.", flavor="I didn't want to do this.",            fx=new[]{new E(CardEffectType.DealDamage,15)} },
            new Def { file="ThreadSummary",     name="Thread Summary",      type=CardType.Special,  rarity=CardRarity.Uncommon,  character=CardCharacter.All, ap=1, desc="Deal 2 damage for each card in your discard pile.",        flavor="TL;DR: everything is on fire.",                 fx=new[]{new E(CardEffectType.DealDamage,2)} },
            new Def { file="PriorityFlag",      name="Priority Flag",       type=CardType.Special,  rarity=CardRarity.Uncommon,  character=CardCharacter.All, ap=1, desc="The next Attack card you play this turn deals double damage.", flavor="!!!!! URGENT !!!!!",                           fx=None },
            new Def { file="RequestReadReceipt",name="Request Read Receipt", type=CardType.Special, rarity=CardRarity.Uncommon,  character=CardCharacter.All, ap=1, desc="Reveal the enemy's intent for the next 2 turns. Draw 1 card.", flavor="They opened it at 9:03 AM. And said nothing.", fx=new[]{new E(CardEffectType.DrawCards,1)} },
            new Def { file="ReplyAll",          name="Reply All",           type=CardType.Attack,   rarity=CardRarity.Uncommon,  character=CardCharacter.All, ap=1, desc="Deal 6 damage. Apply 1 Awaiting Reply to the enemy.",      flavor="The chaos was intentional.",                    fx=new[]{new E(CardEffectType.DealDamage,6), new E(CardEffectType.ApplyStatusToEnemy,1,StatusEffectType.AwaitingReply,1)} },
            new Def { file="DelaySend",         name="Delay Send",          type=CardType.Special,  rarity=CardRarity.Uncommon,  character=CardCharacter.All, ap=0, desc="Reduce incoming damage this turn by 5. Draw 1 card next turn's start.", flavor="Scheduled for 9:00 AM. Professional.",    fx=new[]{new E(CardEffectType.GainShield,5)} },
            new Def { file="Attachment",        name="Attachment",          type=CardType.Attack,   rarity=CardRarity.Uncommon,  character=CardCharacter.All, ap=2, desc="Deal 8 damage +2 for each turn elapsed this combat (max +12).", flavor="See attached. (It's huge.)",                fx=new[]{new E(CardEffectType.DealDamage,8)} },
            new Def { file="ColdOpen",          name="Cold Open",           type=CardType.Special,  rarity=CardRarity.Uncommon,  character=CardCharacter.All, ap=0, desc="The first Attack card played each turn costs 0 AP instead.", flavor="Hope this email finds you well. It won't.",   fx=None },
            new Def { file="Boomerang",         name="Boomerang",           type=CardType.Attack,   rarity=CardRarity.Uncommon,  character=CardCharacter.All, ap=1, desc="Deal 5 damage. Returns to your hand instead of the discard pile.", flavor="I sent it. They sent it back. I sent it again.", fx=new[]{new E(CardEffectType.DealDamage,5)} },
            new Def { file="UnilateralReply",   name="Unilateral Reply",    type=CardType.Attack,   rarity=CardRarity.Uncommon,  character=CardCharacter.All, ap=3, desc="Deal 12 damage. Remove all enemy buffs.",                   flavor="Didn't ask for input. Gave it anyway.",         fx=new[]{new E(CardEffectType.DealDamage,12)} },
            new Def { file="LegalCC",           name="Legal CC",            type=CardType.Attack,   rarity=CardRarity.Uncommon,  character=CardCharacter.All, ap=2, desc="Deal 16 damage. This damage cannot be reduced by enemy armor.", flavor="Copy: litigation@firm.com",                  fx=new[]{new E(CardEffectType.DealDamage,16)} },

            // ── Rare (All) ────────────────────────────────────────────────────────
            new Def { file="VacationAutoresponder", name="Vacation Autoresponder", type=CardType.Defend,  rarity=CardRarity.Rare, character=CardCharacter.All, ap=2, desc="Gain 20 shield. Deal 5 damage.",                          flavor="Unavailable until further notice. Permanently.",             fx=new[]{new E(CardEffectType.GainShield,20), new E(CardEffectType.DealDamage,5)} },
            new Def { file="RecallEmail",       name="Recall Email",        type=CardType.Special,  rarity=CardRarity.Rare,      character=CardCharacter.All, ap=1, desc="Restore 5 HP. Draw 1 card.",                               flavor="Unsent. Unseen. Unhinged.",                                  fx=new[]{new E(CardEffectType.RestoreHP,5), new E(CardEffectType.DrawCards,1)} },
            new Def { file="StartNewThread",    name="Start New Thread",    type=CardType.Special,  rarity=CardRarity.Rare,      character=CardCharacter.All, ap=2, desc="Deal 12 damage. Draw 2 cards. Gain 1 AP.",                 flavor="Subject: (no subject).",                                     fx=new[]{new E(CardEffectType.DealDamage,12), new E(CardEffectType.DrawCards,2), new E(CardEffectType.GainAP,1)} },
            new Def { file="NuclearOption",     name="Nuclear Option",      type=CardType.Attack,   rarity=CardRarity.Rare,      character=CardCharacter.All, ap=3, desc="Deal 35 damage. You cannot play Defend cards next turn.",   flavor="HR has been notified.",                                      fx=new[]{new E(CardEffectType.DealDamage,35)} },
            new Def { file="TheAttachmentFinal",name="The Attachment (Final)", type=CardType.Attack, rarity=CardRarity.Rare,     character=CardCharacter.All, ap=2, desc="Deal damage equal to your missing HP (min 5, max 40).",    flavor="It was 84 megabytes. It was worth it.",                      fx=new[]{new E(CardEffectType.DealDamage,0)} },
            new Def { file="InboxZeroMoment",   name="Inbox Zero Moment",   type=CardType.Special,  rarity=CardRarity.Rare,      character=CardCharacter.All, ap=3, desc="Restore 15 HP. Draw 4 cards. Gain 2 AP.",                  flavor="6 seconds of peace. Then a new one arrives.",               fx=new[]{new E(CardEffectType.RestoreHP,15), new E(CardEffectType.DrawCards,4), new E(CardEffectType.GainAP,2)} },
            new Def { file="OutOfOffice",       name="Out of Office",       type=CardType.Defend,   rarity=CardRarity.Rare,      character=CardCharacter.All, ap=1, desc="Skip the enemy's next attack. Lose 5 HP.",                 flavor="Worth it.",                                                  fx=new[]{new E(CardEffectType.ApplyStatusToEnemy,1,StatusEffectType.Unread,1)} },
            new Def { file="UnsubscribeAll",    name="Unsubscribe All",     type=CardType.Special,  rarity=CardRarity.Rare,      character=CardCharacter.All, ap=2, desc="Remove all status effects from yourself. Draw 2 cards.",    flavor="A clean slate. For 24 hours.",                               fx=new[]{new E(CardEffectType.DrawCards,2)} },
            new Def { file="EmailBankruptcy",   name="Email Bankruptcy",    type=CardType.Special,  rarity=CardRarity.Rare,      character=CardCharacter.All, ap=0, desc="Discard your entire hand. Draw 5 new cards. Reduce enemy HP by 5 for each card discarded.", flavor="Select all. Archive. No regrets.", fx=new[]{new E(CardEffectType.DrawCards,5)} },
            new Def { file="MandatoryFun",      name="Mandatory Fun",       type=CardType.Special,  rarity=CardRarity.Rare,      character=CardCharacter.All, ap=2, desc="Apply 3 Guilt stacks to enemy. Apply 1 Guilt stack to yourself.", flavor="We hope you'll join us for the team lunch.",            fx=new[]{new E(CardEffectType.ApplyStatusToEnemy,3,StatusEffectType.Guilt,3), new E(CardEffectType.ApplyStatusToPlayer,1,StatusEffectType.Guilt,1)} },
            new Def { file="DataBreachNotice",  name="Data Breach Notice",  type=CardType.Attack,   rarity=CardRarity.Rare,      character=CardCharacter.All, ap=2, desc="Deal 14 damage. Enemy cannot regen HP this turn.",          flavor="We take your security seriously.",                            fx=new[]{new E(CardEffectType.DealDamage,14)} },
            new Def { file="TheReadReceipt",    name="The Read Receipt",    type=CardType.Special,  rarity=CardRarity.Rare,      character=CardCharacter.All, ap=1, desc="Force the enemy to reveal and skip its next action.",        flavor="Seen. 10:47 AM. No reply.",                                  fx=new[]{new E(CardEffectType.ApplyStatusToEnemy,1,StatusEffectType.Unread,1)} },

            // ── Legendary (All) ───────────────────────────────────────────────────
            new Def { file="TheLinkedInPost",   name="The LinkedIn Post",   type=CardType.Special,  rarity=CardRarity.Legendary, character=CardCharacter.All, ap=0, desc="Draw 5 cards. Every enemy in this run gains 1 permanent Guilt stack. (Once per run.)", flavor="'Humbled and honored to announce...'",  fx=new[]{new E(CardEffectType.DrawCards,5), new E(CardEffectType.ApplyStatusToEnemy,1,StatusEffectType.Guilt,-1)} },
            new Def { file="AutoreplyLoop",     name="Autoreply Loop",      type=CardType.Special,  rarity=CardRarity.Legendary, character=CardCharacter.All, ap=2, desc="Apply Unread (3 turns). Every turn it's active, deal 10 damage automatically.", flavor="Replies to itself. Has been going for 3 years.", fx=new[]{new E(CardEffectType.ApplyStatusToEnemy,1,StatusEffectType.Unread,3), new E(CardEffectType.DealDamage,10)} },
            new Def { file="TheResignationEmail",name="The Resignation Email",type=CardType.Attack, rarity=CardRarity.Legendary, character=CardCharacter.All, ap=3, desc="Deal 50 damage. You cannot gain shield for the rest of this combat.", flavor="Last day is Friday. Or right now.",          fx=new[]{new E(CardEffectType.DealDamage,50)} },
            new Def { file="InboxZero",         name="Inbox Zero",          type=CardType.Special,  rarity=CardRarity.Legendary, character=CardCharacter.All, ap=5, desc="Deal damage equal to 2x the enemy's current HP. (Win this combat instantly.)", flavor="It happened. It was real. No one believed you.", fx=new[]{new E(CardEffectType.DealDamage,0)} },
            new Def { file="TheColdEmail",      name="The Cold Email",      type=CardType.Special,  rarity=CardRarity.Legendary, character=CardCharacter.All, ap=1, desc="Apply every status effect in the game to the enemy. (Once per run.)", flavor="Hi [FIRST NAME], I came across your profile and thought-", fx=new[]{new E(CardEffectType.ApplyStatusToEnemy,1,StatusEffectType.Unread,2), new E(CardEffectType.ApplyStatusToEnemy,3,StatusEffectType.Guilt,3), new E(CardEffectType.ApplyStatusToEnemy,1,StatusEffectType.AwaitingReply,2)} },

            // ── Intern (Common) ───────────────────────────────────────────────────
            new Def { file="CopyPaste",         name="Copy-Paste",          type=CardType.Attack,   rarity=CardRarity.Common,    character=CardCharacter.Intern, ap=0, desc="Deal 4 damage. If you played another card this turn, deal 4 extra damage.", flavor="Self-taught.",                              fx=new[]{new E(CardEffectType.DealDamage,4)} },
            new Def { file="AskForClarification",name="Ask for Clarification",type=CardType.Special,rarity=CardRarity.Common,    character=CardCharacter.Intern, ap=1, desc="Apply Awaiting Reply (1 turn) to enemy. Draw 2 cards.", flavor="'Per the spec' - which spec?",                               fx=new[]{new E(CardEffectType.ApplyStatusToEnemy,1,StatusEffectType.AwaitingReply,1), new E(CardEffectType.DrawCards,2)} },
            new Def { file="BeginnersLuck",     name="Beginner's Luck",     type=CardType.Attack,   rarity=CardRarity.Common,    character=CardCharacter.Intern, ap=1, desc="Deal 6-18 damage (random).",                           flavor="They either loved it or said nothing. Same thing.",          fx=new[]{new E(CardEffectType.DealDamage,12)} },
            new Def { file="OvereagerReply",    name="Overeager Reply",     type=CardType.Special,  rarity=CardRarity.Common,    character=CardCharacter.Intern, ap=0, desc="Draw 3 cards. Take 3 damage.",                         flavor="Sent on mobile. Probably shouldn't have.",                   fx=new[]{new E(CardEffectType.DrawCards,3), new E(CardEffectType.ApplyStatusToPlayer,3,StatusEffectType.Guilt,1)} },
            new Def { file="FirstWeekEnergy",   name="First Week Energy",   type=CardType.Special,  rarity=CardRarity.Common,    character=CardCharacter.Intern, ap=2, desc="Your next 3 cards this turn cost 0 AP.",               flavor="This feeling expires on day 8.",                             fx=new[]{new E(CardEffectType.GainAP,3)} },
            new Def { file="InternsInstinct",   name="Intern's Instinct",   type=CardType.Attack,   rarity=CardRarity.Common,    character=CardCharacter.Intern, ap=2, desc="Deal 10 damage. If enemy has more than 50 HP, deal 10 extra.", flavor="The audacity of not knowing any better.",              fx=new[]{new E(CardEffectType.DealDamage,10)} },

            // ── Manager (Uncommon) ────────────────────────────────────────────────
            new Def { file="Delegate",          name="Delegate",            type=CardType.Special,  rarity=CardRarity.Uncommon,  character=CardCharacter.Manager, ap=1, desc="Shuffle 2 copies of 'Someone Else's Problem' (0 AP: deal 5 damage) into your deck.", flavor="I trust you on this.", fx=None },
            new Def { file="StandUpMeeting",    name="Stand-Up Meeting",    type=CardType.Defend,   rarity=CardRarity.Uncommon,  character=CardCharacter.Manager, ap=2, desc="Gain 15 shield. The next 2 Defend cards you play cost 0 AP.", flavor="Let's keep it quick. (90 minutes later.)",          fx=new[]{new E(CardEffectType.GainShield,15)} },
            new Def { file="PerformanceReview", name="Performance Review",  type=CardType.Attack,   rarity=CardRarity.Uncommon,  character=CardCharacter.Manager, ap=2, desc="Deal 12 damage. Apply 2 Guilt stacks to enemy.",        flavor="Exceeds expectations... of failure.",                        fx=new[]{new E(CardEffectType.DealDamage,12), new E(CardEffectType.ApplyStatusToEnemy,2,StatusEffectType.Guilt,2)} },
            new Def { file="OpenDoorPolicy",    name="Open Door Policy",    type=CardType.Defend,   rarity=CardRarity.Uncommon,  character=CardCharacter.Manager, ap=0, desc="Gain 6 shield. If you took damage this turn, gain 12 shield instead.", flavor="The door was already open. They knocked anyway.", fx=new[]{new E(CardEffectType.GainShield,6)} },
            new Def { file="Synergy",           name="Synergy",             type=CardType.Special,  rarity=CardRarity.Uncommon,  character=CardCharacter.Manager, ap=1, desc="Each card played after this one this turn gains +3 damage or +3 shield.", flavor="I want us to really lean into this.",          fx=None },
            new Def { file="QuarterlyReview",   name="Quarterly Review",    type=CardType.Attack,   rarity=CardRarity.Uncommon,  character=CardCharacter.Manager, ap=3, desc="Deal 6 damage for each card in your hand.",             flavor="Numbers don't lie. These numbers do.",               fx=new[]{new E(CardEffectType.DealDamage,6)} },

            // ── Lawyer (Uncommon) ─────────────────────────────────────────────────
            new Def { file="CeaseAndDesist",    name="Cease and Desist",    type=CardType.Attack,   rarity=CardRarity.Uncommon,  character=CardCharacter.Lawyer, ap=2, desc="Deal 15 damage. Enemy cannot apply status effects next turn.", flavor="Please govern yourself accordingly.",             fx=new[]{new E(CardEffectType.DealDamage,15)} },
            new Def { file="WithoutPrejudice",  name="Without Prejudice",   type=CardType.Defend,   rarity=CardRarity.Uncommon,  character=CardCharacter.Lawyer, ap=1, desc="Gain 10 shield. If the enemy is Unread, gain 10 extra shield.", flavor="Off the record. On the record.",                fx=new[]{new E(CardEffectType.GainShield,10)} },
            new Def { file="Discovery",         name="Discovery",           type=CardType.Special,  rarity=CardRarity.Uncommon,  character=CardCharacter.Lawyer, ap=1, desc="Reveal enemy intent for 3 turns. Draw 2 cards.",           flavor="Everything is admissible.",                          fx=new[]{new E(CardEffectType.DrawCards,2)} },
            new Def { file="Precedent",         name="Precedent",           type=CardType.Special,  rarity=CardRarity.Uncommon,  character=CardCharacter.Lawyer, ap=2, desc="The next card you play is played twice.",                  flavor="We've done this before. We'll do it again.",         fx=None },
            new Def { file="ClassAction",       name="Class Action",        type=CardType.Attack,   rarity=CardRarity.Uncommon,  character=CardCharacter.Lawyer, ap=3, desc="Deal 10 damage. Apply 3 Guilt stacks to enemy.",           flavor="You are not alone in this inbox.",                   fx=new[]{new E(CardEffectType.DealDamage,10), new E(CardEffectType.ApplyStatusToEnemy,3,StatusEffectType.Guilt,3)} },
            new Def { file="Injunction",        name="Injunction",          type=CardType.Defend,   rarity=CardRarity.Uncommon,  character=CardCharacter.Lawyer, ap=2, desc="Enemy skips its next two actions.",                        flavor="Court-ordered silence. Finally.",                     fx=new[]{new E(CardEffectType.ApplyStatusToEnemy,1,StatusEffectType.Unread,2)} },

            // ── Dev (Rare) ────────────────────────────────────────────────────────
            new Def { file="RunScript",         name="Run Script",          type=CardType.Special,  rarity=CardRarity.Rare,      character=CardCharacter.Dev, ap=1, desc="Apply a random status effect to the enemy.",             flavor="Works on my machine.",                                       fx=new[]{new E(CardEffectType.ApplyStatusToEnemy,1,StatusEffectType.Guilt,1)} },
            new Def { file="CronJob",           name="Cron Job",            type=CardType.Special,  rarity=CardRarity.Rare,      character=CardCharacter.Dev, ap=2, desc="At the start of each of your next 3 turns, deal 8 damage automatically.", flavor="Scheduled. Unattended. Unstoppable.",        fx=new[]{new E(CardEffectType.DealDamage,8)} },
            new Def { file="RegexFilter",       name="Regex Filter",        type=CardType.Defend,   rarity=CardRarity.Rare,      character=CardCharacter.Dev, ap=1, desc="Gain 8 shield. Block all status effects applied to you this turn.", flavor=".*@.*\\.spam - filtered.",                    fx=new[]{new E(CardEffectType.GainShield,8)} },
            new Def { file="StackOverflow",     name="Stack Overflow",      type=CardType.Attack,   rarity=CardRarity.Rare,      character=CardCharacter.Dev, ap=0, desc="Deal 2 damage for each Special card in your discard pile.", flavor="Answered in 2009. Still relevant.",                         fx=new[]{new E(CardEffectType.DealDamage,2)} },
            new Def { file="DeployToProd",      name="Deploy to Prod",      type=CardType.Attack,   rarity=CardRarity.Rare,      character=CardCharacter.Dev, ap=3, desc="Deal 30 damage. Take 8 damage.",                           flavor="On a Friday. No rollback plan.",                             fx=new[]{new E(CardEffectType.DealDamage,30), new E(CardEffectType.ApplyStatusToPlayer,8,StatusEffectType.Guilt,1)} },
            new Def { file="GitBlame",          name="Git Blame",           type=CardType.Special,  rarity=CardRarity.Rare,      character=CardCharacter.Dev, ap=1, desc="Apply 3 Guilt stacks to the enemy. Draw 1 card.",          flavor="It was written by someone who no longer works here. It was you.", fx=new[]{new E(CardEffectType.ApplyStatusToEnemy,3,StatusEffectType.Guilt,3), new E(CardEffectType.DrawCards,1)} },

            // ── Ghost (Rare) ──────────────────────────────────────────────────────
            new Def { file="ReadReceiptTrap",   name="Read Receipt Trap",   type=CardType.Special,  rarity=CardRarity.Rare,      character=CardCharacter.Ghost, ap=1, desc="Apply 1 Guilt stack to enemy every time they attack this combat.", flavor="They know you saw it.",                        fx=new[]{new E(CardEffectType.ApplyStatusToEnemy,1,StatusEffectType.Guilt,1)} },
            new Def { file="SoftBlock",         name="Soft Block",          type=CardType.Defend,   rarity=CardRarity.Rare,      character=CardCharacter.Ghost, ap=0, desc="Gain 7 shield. The enemy cannot target you with status effects until your next turn.", flavor="Not blocked. Just... unavailable.",    fx=new[]{new E(CardEffectType.GainShield,7)} },
            new Def { file="PassiveNotification",name="Passive Notification",type=CardType.Attack,  rarity=CardRarity.Rare,      character=CardCharacter.Ghost, ap=0, desc="Deal 3 damage. Apply 1 Guilt stack to enemy.",             flavor="A gentle reminder. Endlessly.",                              fx=new[]{new E(CardEffectType.DealDamage,3), new E(CardEffectType.ApplyStatusToEnemy,1,StatusEffectType.Guilt,1)} },
            new Def { file="LeftOnRead",        name="Left on Read",        type=CardType.Special,  rarity=CardRarity.Rare,      character=CardCharacter.Ghost, ap=1, desc="Apply Unread (1 turn) to enemy. Restore 4 HP.",             flavor="Power move. No explanation.",                                fx=new[]{new E(CardEffectType.ApplyStatusToEnemy,1,StatusEffectType.Unread,1), new E(CardEffectType.RestoreHP,4)} },
            new Def { file="DigitalHaunting",   name="Digital Haunting",    type=CardType.Special,  rarity=CardRarity.Rare,      character=CardCharacter.Ghost, ap=2, desc="At the end of each enemy turn, deal 4 damage automatically for 3 turns.", flavor="Still in the thread. Always watching.",         fx=new[]{new E(CardEffectType.DealDamage,4)} },
            new Def { file="Unopened",          name="Unopened",            type=CardType.Attack,   rarity=CardRarity.Rare,      character=CardCharacter.Ghost, ap=3, desc="Deal 25 damage. The enemy cannot heal this turn.",          flavor="The worst thing you can do to someone who needs a reply.",   fx=new[]{new E(CardEffectType.DealDamage,25)} },
        };
    }
}
