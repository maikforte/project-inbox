using System.Collections.Generic;
using UnityEngine;

namespace InboxZero.Data
{
    [System.Serializable]
    public struct CardEffect
    {
        public CardEffectType effectType;
        public int value;
        public StatusEffectType statusType;
        public int statusDuration;
    }

    [CreateAssetMenu(fileName = "NewCard", menuName = "InboxZero/Card")]
    public class CardData : ScriptableObject
    {
        public string cardName;
        public CardType cardType;
        public CardRarity rarity;
        public CardCharacter character;
        public int apCost;
        [TextArea] public string effectDescription;
        [TextArea] public string flavorText;
        public List<CardEffect> effects;
        public Sprite icon;
    }
}
