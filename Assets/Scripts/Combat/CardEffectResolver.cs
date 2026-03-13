using InboxZero.Core;
using InboxZero.Data;
using UnityEngine;

namespace InboxZero.Combat
{
    // Implemented by EnemyController (TASK-07).
    public interface ICombatTarget
    {
        void TakeDamage(int amount);
        void ApplyStatus(StatusEffectType type, int duration);
    }

    // Set ActiveTarget before combat starts. EnemyController (TASK-07) sets this.
    public static class CardEffectResolver
    {
        public static ICombatTarget ActiveTarget { get; set; }

        public static void Resolve(CardData card)
        {
            var gm = GameManager.Instance;

            foreach (var effect in card.effects)
            {
                switch (effect.effectType)
                {
                    case CardEffectType.DealDamage:
                        int dmg = effect.value + BonusDamage(card);
                        ActiveTarget?.TakeDamage(dmg);
                        break;

                    case CardEffectType.GainShield:
                        gm.CurrentShield += effect.value;
                        break;

                    case CardEffectType.DrawCards:
                        DeckManager.Instance.DrawCards(effect.value);
                        break;

                    case CardEffectType.GainAP:
                        TurnManager.Instance.RefundAP(effect.value);
                        break;

                    case CardEffectType.ApplyStatusToEnemy:
                        ActiveTarget?.ApplyStatus(effect.statusType, effect.statusDuration);
                        break;

                    case CardEffectType.ApplyStatusToPlayer:
                        // Player status effects handled by CombatManager (future task).
                        break;

                    case CardEffectType.RestoreHP:
                        gm.CurrentHP = Mathf.Min(gm.CurrentHP + effect.value, gm.MaxHP);
                        break;
                }
            }
        }

        // Applies Inbox Zero Badge relic bonus to attack cards.
        static int BonusDamage(CardData card)
        {
            if (card.cardType != CardType.Attack) return 0;
            int bonus = 0;
            foreach (var relic in GameManager.Instance.ActiveRelics)
                if (relic.effectType == RelicEffectType.AttackCardBonusDamage)
                    bonus += relic.effectValue;
            return bonus;
        }
    }
}
