using System.Collections.Generic;
using InboxZero.Data;
using UnityEngine;

namespace InboxZero.Core
{
    /// Handles card drops on enemy death.
    /// Assign all reward cards to cardPool in the inspector.
    public class CardDropManager : MonoBehaviour
    {
        public static CardDropManager Instance { get; private set; }

        [Header("Card Pool")]
        [Tooltip("All cards that can be dropped. Filtered by rarity at roll time.")]
        public List<CardData> cardPool = new List<CardData>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// Rolls against the enemy's dropChance. On success, picks a random card
        /// of the enemy's dropRarity, adds it to CardCollection, and returns it.
        /// Returns null if no drop.
        public CardData RollDrop(EnemyData enemy)
        {
            if (enemy == null) return null;
            if (Random.value > enemy.dropChance) return null;

            var eligible = new List<CardData>();
            foreach (var card in cardPool)
                if (card != null && card.rarity == enemy.dropRarity)
                    eligible.Add(card);

            if (eligible.Count == 0) return null;

            var dropped = eligible[Random.Range(0, eligible.Count)];
            GameManager.Instance.CardCollection.Add(dropped);
            return dropped;
        }
    }
}
