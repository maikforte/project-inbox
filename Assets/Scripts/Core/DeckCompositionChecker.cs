using System.Collections.Generic;
using InboxZero.Data;

namespace InboxZero.Core
{
    /// Shared rules for whether a card can be added to the active deck.
    /// Used by CardRewardScreen now and the future DeckBuilder screen.
    public static class DeckCompositionChecker
    {
        public const int MaxDeckSize  = 15;
        public const int MaxUncommons =  4;
        public const int MaxRares     =  2;

        /// Returns whether <paramref name="card"/> can be added to the current deck,
        /// and a short uppercase reason string if it cannot (empty string if it can).
        public static (bool canAdd, string reason) CanAdd(CardData card)
        {
            if (card == null) return (false, "NULL CARD");

            var deck = GetFullDeck();

            // Total size cap applies to all rarities.
            if (deck.Count >= MaxDeckSize)
                return (false, $"DECK FULL ({MaxDeckSize})");

            switch (card.rarity)
            {
                case CardRarity.Uncommon:
                {
                    int count = 0;
                    foreach (var c in deck)
                        if (c.rarity == CardRarity.Uncommon) count++;
                    if (count >= MaxUncommons)
                        return (false, $"MAX {MaxUncommons} UNCOMMONS");
                    break;
                }

                case CardRarity.Rare:
                {
                    int totalRares = 0;
                    int sameRare   = 0;
                    foreach (var c in deck)
                    {
                        if (c.rarity == CardRarity.Rare) totalRares++;
                        if (c == card)                   sameRare++;
                    }
                    if (sameRare >= 1)
                        return (false, "ALREADY IN DECK");
                    if (totalRares >= MaxRares)
                        return (false, $"MAX {MaxRares} RARES");
                    break;
                }
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
