using System.Collections.Generic;
using UnityEngine;

namespace InboxZero.Data
{
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "InboxZero/Enemy")]
    public class EnemyData : ScriptableObject
    {
        public string enemyName;
        public string enemyType;
        public int maxHP;
        public int damagePerTurn;
        public int regenPerTurn;
        public StatusEffectType statusAppliedOnAttack;
        public int statusDuration;
        public RewardTier rewardTier;
        [TextArea] public string flavorText;

        [Header("Visuals")]
        public Sprite portrait;

        [Header("Signature Unlock")]
        [Tooltip("Card permanently unlocked the first time this enemy is defeated. Leave null if none.")]
        public CardData signatureUnlockCard;

        [Header("Card Drop (legacy — unused since TASK-38)")]
        [Range(0f, 1f)] public float dropChance = 0.5f;
        public CardRarity dropRarity = CardRarity.Common;

        [Header("Intent Deck")]
        [Tooltip("Cards this enemy cycles through on its turns. Leave empty to use legacy flat damage.")]
        public List<CardData> intentDeck = new List<CardData>();
    }
}
