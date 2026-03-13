using System.Collections.Generic;
using UnityEngine;
using InboxZero.Data;

namespace InboxZero.Core
{
    public class DeckManager : MonoBehaviour
    {
        public static DeckManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // Load a full card list into the draw pile and shuffle.
        public void InitDeck(List<CardData> cards)
        {
            var gm = GameManager.Instance;
            gm.DrawPile = new List<CardData>(cards);
            gm.Hand.Clear();
            gm.DiscardPile.Clear();
            Shuffle(gm.DrawPile);
        }

        // Draw N cards from draw pile into hand. Reshuffles discard if draw pile runs out.
        public void DrawCards(int count)
        {
            var gm = GameManager.Instance;
            for (int i = 0; i < count; i++)
            {
                if (gm.DrawPile.Count == 0)
                {
                    if (gm.DiscardPile.Count == 0) break;
                    ReshuffleDiscard();
                }
                gm.Hand.Add(gm.DrawPile[0]);
                gm.DrawPile.RemoveAt(0);
            }
        }

        // Move a played card from hand to discard.
        public void PlayCard(CardData card)
        {
            var gm = GameManager.Instance;
            gm.Hand.Remove(card);
            gm.DiscardPile.Add(card);
        }

        // Discard entire hand (end of turn).
        public void DiscardHand()
        {
            var gm = GameManager.Instance;
            gm.DiscardPile.AddRange(gm.Hand);
            gm.Hand.Clear();
        }

        // Move discard pile back into draw pile and shuffle.
        void ReshuffleDiscard()
        {
            var gm = GameManager.Instance;
            gm.DrawPile.AddRange(gm.DiscardPile);
            gm.DiscardPile.Clear();
            Shuffle(gm.DrawPile);
        }

        void Shuffle(List<CardData> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
