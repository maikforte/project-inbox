namespace InboxZero.Data
{
    public enum CardType { Attack, Defend, Special }

    public enum CardRarity { Starter, Common, Uncommon, Rare, Legendary }

    public enum CardEffectType
    {
        DealDamage,
        GainShield,
        DrawCards,
        GainAP,
        ApplyStatusToEnemy,
        ApplyStatusToPlayer,
        RestoreHP,
        HealSelf
    }

    public enum StatusEffectType { None, Unread, Guilt, AwaitingReply }

    public enum RewardTier { Common, Uncommon, Rare, Boss }

    public enum CardCharacter { All, Intern, Manager, Lawyer, Dev, Ghost }

    public enum RelicEffectType
    {
        BonusAPOnCombatStart,
        HealOnTurnStart,
        AttackCardBonusDamage,
        BonusDrawPerTurn,
        ReduceIncomingDamage,
        BonusMaxHP
    }
}
