using UnityEngine;

namespace InboxZero.Data
{
    [CreateAssetMenu(fileName = "NewRelic", menuName = "InboxZero/Relic")]
    public class RelicData : ScriptableObject
    {
        public string relicName;
        [TextArea] public string flavorText;
        public string effectDescription;
        public RelicEffectType effectType;
        public int effectValue;
    }
}
