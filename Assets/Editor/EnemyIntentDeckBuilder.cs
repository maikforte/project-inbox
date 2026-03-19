using System.Collections.Generic;
using InboxZero.Data;
using UnityEditor;
using UnityEngine;

/// Run via: InboxZero → Rebuild Enemy Intent Decks
/// Creates all intent CardData assets via AssetDatabase so Unity manages GUIDs
/// correctly, then wires each enemy's intentDeck list.
public static class EnemyIntentDeckBuilder
{
    const string IntentFolder  = "Assets/Data/Cards/EnemyIntents";
    const string EnemiesFolder = "Assets/Data/Enemies";

    [MenuItem("InboxZero/Rebuild Enemy Intent Decks")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(IntentFolder))
            AssetDatabase.CreateFolder("Assets/Data/Cards", "EnemyIntents");

        // ── Create intent cards ───────────────────────────────────────────────

        // Newsletter Flood
        var nfSpam  = Card("NF_SpamAttack",      "Spam Attack",      CardType.Attack, "Deal 5 damage.",  Dmg(5));
        var nfBlast = Card("NF_BulkBlast",        "Bulk Blast",       CardType.Attack, "Deal 8 damage.",  Dmg(8));

        // Calendar Invite
        var ciReq = Card("CI_MeetingRequest",  "Meeting Request",  CardType.Attack, "Deal 7 damage.",  Dmg(7));
        var ciRec = Card("CI_RecurringInvite", "Recurring Invite", CardType.Attack,
            "Deal 7 damage. Apply AWAITING REPLY (1 turn).",
            Dmg(7), Status(CardEffectType.ApplyStatusToPlayer, StatusEffectType.AwaitingReply, 1));

        // Reply-All Demon
        var radAll   = Card("RAD_ReplyAll",   "Reply All",   CardType.Attack, "Deal 9 damage.",  Dmg(9));
        var radChain = Card("RAD_ChainReply", "Chain Reply", CardType.Attack, "Deal 14 damage.", Dmg(14));

        // Auto-CC Manager
        var accCC      = Card("ACC_CCEveryone",  "CC Everyone",  CardType.Attack, "Deal 8 damage.",   Dmg(8));
        var accPassive = Card("ACC_PassiveLoop", "Passive Loop", CardType.Defend, "Gain 6 shield.",   Shield(6));

        // Out-of-Office Loop
        var oooBounce  = Card("OOO_BounceBack",   "Bounce Back",   CardType.Attack,
            "Deal 10 damage. Apply AWAITING REPLY (1 turn).",
            Dmg(10), Status(CardEffectType.ApplyStatusToPlayer, StatusEffectType.AwaitingReply, 1));
        var oooInfinite = Card("OOO_InfiniteLoop", "Infinite Loop", CardType.Attack, "Deal 14 damage.", Dmg(14));

        // Passive-Aggressive Karen
        var pakPer = Card("PAK_PerMyLastEmail", "Per My Last Email", CardType.Attack, "Deal 11 damage.", Dmg(11));
        var pakAs  = Card("PAK_AsPerAbove",     "As Per Above",      CardType.Attack,
            "Deal 11 damage. Apply GUILT (2 turns).",
            Dmg(11), Status(CardEffectType.ApplyStatusToPlayer, StatusEffectType.Guilt, 2));

        // Thread That Never Ends
        var ttneEternal = Card("TTNE_EternalReply",    "Eternal Reply",    CardType.Attack, "Deal 14 damage.", Dmg(14));
        var ttneBump    = Card("TTNE_ThreadBump",       "Thread Bump",      CardType.Defend, "Gain 8 shield.",  Shield(8));
        var ttne78      = Card("TTNE_78Participants",   "78 Participants",  CardType.Attack, "Deal 20 damage.", Dmg(20));

        // ── Wire intent decks ─────────────────────────────────────────────────

        Deck("NewsletterFlood",       nfSpam, nfSpam, nfBlast);
        Deck("CalendarInvite",        ciReq, ciReq, ciRec);
        Deck("ReplyAllDemon",         radAll, radAll, radChain);
        Deck("AutoCCManager",         accCC, accCC, accPassive);
        Deck("OutOfOfficeLoop",       oooBounce, oooBounce, oooInfinite);
        Deck("PassiveAggressiveKaren",pakPer, pakPer, pakAs);
        Deck("ThreadThatNeverEnds",   ttneEternal, ttneEternal, ttneBump, ttne78);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[EnemyIntentDeckBuilder] Done — all intent decks rebuilt.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static CardData Card(string assetName, string cardName, CardType type,
        string desc, params CardEffect[] effects)
    {
        string path = $"{IntentFolder}/{assetName}.asset";
        AssetDatabase.DeleteAsset(path);          // remove stale asset if present

        var data = ScriptableObject.CreateInstance<CardData>();
        data.cardName         = cardName;
        data.cardType         = type;
        data.rarity           = CardRarity.Starter;
        data.apCost           = 0;
        data.effectDescription = desc;
        data.effects          = new List<CardEffect>(effects);

        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    static void Deck(string enemyFile, params CardData[] cards)
    {
        string path = $"{EnemiesFolder}/{enemyFile}.asset";
        var enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        if (enemy == null) { Debug.LogWarning($"[EnemyIntentDeckBuilder] Missing enemy: {path}"); return; }

        enemy.intentDeck = new List<CardData>(cards);
        EditorUtility.SetDirty(enemy);
    }

    static CardEffect Dmg(int v) =>
        new CardEffect { effectType = CardEffectType.DealDamage, value = v };

    static CardEffect Shield(int v) =>
        new CardEffect { effectType = CardEffectType.GainShield, value = v };

    static CardEffect Status(CardEffectType type, StatusEffectType status, int duration) =>
        new CardEffect { effectType = type, statusType = status, statusDuration = duration };
}
