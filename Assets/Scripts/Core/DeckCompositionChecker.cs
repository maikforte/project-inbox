using System.Collections.Generic;
using InboxZero.Data;

namespace InboxZero.Core
{
    /// Shared rules for whether a card can be added to the active deck.
    /// Used by CardRewardScreen now and the future DeckBuilder screen.
    public static class DeckCompositionChecker
    {
        public const int MaxDeckSize   = 99;
        public const int MaxUncommons  = 99;
        public const int MaxRares      = 99;
        public const int MaxLegendary  = 99;

        /// Returns whether <paramref name="card"/> can be added to the current deck,
        /// and a short uppercase reason string if it cannot (empty string if it can).
        public static (bool canAdd, string reason) CanAdd(CardData card)
        {
            if (card == null) return (false, "NULL CARD");

            var deck = GetFullDeck();

            // Total size cap applies to all rarities.
            if (deck.Count >= MaxDeckSize)
                return (false, $"DECK FULL ({MaxDeckSize})");

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
