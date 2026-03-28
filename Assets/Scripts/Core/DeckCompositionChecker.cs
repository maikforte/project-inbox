using System.Collections.Generic;
using InboxZero.Data;

namespace InboxZero.Core
{
    /// Shared rules for whether a card can be added to the active deck.
    /// Used by CardRewardScreen and any future deck-editing UI.
    public static class DeckCompositionChecker
    {
        public const int MaxDeckSize   = 15;
        public const int MaxUncommons  = 4;
        public const int MaxRares      = 2;
        public const int MaxLegendary  = 1;

        /// Returns whether <paramref name="card"/> can be added to the current deck,
        /// and a short uppercase reason string if it cannot (empty string if it can).
        public static (bool canAdd, string reason) CanAdd(CardData card)
        {
            if (card == null) return (false, "NULL CARD");

            var deck = GetFullDeck();

            if (deck.Count >= MaxDeckSize)
                return (false, $"DECK FULL ({MaxDeckSize})");

            if (card.rarity == CardRarity.Uncommon)
            {
                int count = 0;
                foreach (var c in deck)
                    if (c != null && c.rarity == CardRarity.Uncommon) count++;
                if (count >= MaxUncommons)
                    return (false, $"MAX UNCOMMONS ({MaxUncommons})");
            }

            if (card.rarity == CardRarity.Rare)
            {
                int count = 0;
                foreach (var c in deck)
                    if (c != null && c.rarity == CardRarity.Rare) count++;
                if (count >= MaxRares)
                    return (false, $"MAX RARES ({MaxRares})");
            }

            if (card.rarity == CardRarity.Legendary)
            {
                foreach (var c in deck)
                    if (c == card) return (false, "ALREADY OWNED");
                int count = 0;
                foreach (var c in deck)
                    if (c != null && c.rarity == CardRarity.Legendary) count++;
                if (count >= MaxLegendary)
                    return (false, $"MAX LEGENDARY ({MaxLegendary})");
            }

            return (true, string.Empty);
        }

        /// All cards currently in the run's deck (draw pile + hand + discard pile combined).
        static List<CardData> GetFullDeck()
        {
            var gm  = GameManager.Instance;
            var all = new List<CardData>(gm.DrawPile.Count + gm.Hand.Count + gm.DiscardPile.Count);
            all.AddRange(gm.DrawPile);
            all.AddRange(gm.Hand);
            all.AddRange(gm.DiscardPile);
            return all;
        }
    }
}
