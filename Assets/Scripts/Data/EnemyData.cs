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

        [Header("Card Drop")]
        [Range(0f, 1f)] public float dropChance = 0.5f;
        public CardRarity dropRarity = CardRarity.Common;
    }
}
