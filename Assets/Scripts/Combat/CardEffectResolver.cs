using InboxZero.Core;
using InboxZero.Data;
using InboxZero.Enemies;
using UnityEngine;

namespace InboxZero.Combat
{
    // Implemented by EnemyController (TASK-07).
    public interface ICombatTarget
    {
        void TakeDamage(int amount);
        void ApplyStatus(StatusEffectType type, int duration);
    }

    /// Who is playing the card — determines effect targets.
    public enum EffectExecutor { Player, Enemy }

    // Set ActiveTarget before combat starts. EnemyController (TASK-07) sets this.
    public static class CardEffectResolver
    {
        public static ICombatTarget ActiveTarget { get; set; }

        /// Full EnemyController reference — needed for GainShield / HealSelf on enemy executor.
        public static EnemyController ActiveEnemy { get; set; }

        public static void Resolve(CardData card, EffectExecutor executor = EffectExecutor.Player)
        {
            var gm = GameManager.Instance;

            foreach (var effect in card.effects)
            {
                switch (effect.effectType)
                {
                    case CardEffectType.DealDamage:
                        int dmg = effect.value + (executor == EffectExecutor.Player ? BonusDamage(card) : 0);
                        if (executor == EffectExecutor.Player)
                        {
                            ActiveTarget?.TakeDamage(dmg);
                            gm.DamageDealt += dmg;
                        }
                        else
                        {
                            gm.TakeDamage(dmg);
                        }
                        break;

                    case CardEffectType.GainShield:
                        if (executor == EffectExecutor.Player)
                        {
                            gm.CurrentShield += effect.value;
                            AudioManager.Instance?.PlayGainShield();
                        }
                        else
                            ActiveEnemy?.GainShield(effect.value);
                        break;

                    case CardEffectType.DrawCards:
                        if (executor == EffectExecutor.Player)
                            DeckManager.Instance.DrawCards(effect.value);
                        break;

                    case CardEffectType.GainAP:
                        if (executor == EffectExecutor.Player)
                            TurnManager.Instance.RefundAP(effect.value);
                        break;

                    case CardEffectType.ApplyStatusToEnemy:
                        if (executor == EffectExecutor.Player)
                            ActiveTarget?.ApplyStatus(effect.statusType, effect.statusDuration);
                        else
                            PlayerStatusManager.Instance?.ApplyStatus(effect.statusType, effect.statusDuration);
                        break;

                    case CardEffectType.ApplyStatusToPlayer:
                        if (executor == EffectExecutor.Player)
                            PlayerStatusManager.Instance?.ApplyStatus(effect.statusType, effect.statusDuration);
                        else
                            ActiveTarget?.ApplyStatus(effect.statusType, effect.statusDuration);
                        break;

                    case CardEffectType.RestoreHP:
                        if (executor == EffectExecutor.Player)
                        {
                            gm.CurrentHP = Mathf.Min(gm.CurrentHP + effect.value, gm.MaxHP);
                            AudioManager.Instance?.PlayRestoreHP();
                        }
                        break;

                    case CardEffectType.HealSelf:
                        if (executor == EffectExecutor.Enemy)
                            ActiveEnemy?.Heal(effect.value);
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
