using System.Collections.Generic;
using UnityEngine;

namespace InboxZero.Data
{
    /// Global registry of every card in the game.
    /// Assign all CardData SOs here so AllMailScreen can display the full compendium.
    [CreateAssetMenu(fileName = "AllCardsRegistry", menuName = "InboxZero/All Cards Registry")]
    public class AllCardsRegistry : ScriptableObject
    {
        public List<CardData> allCards = new List<CardData>();
    }
}
