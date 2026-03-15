using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using InboxZero.Data;

namespace InboxZero.Core
{
    public class GameManager : MonoBehaviour
    {
        // Fired once when player HP first drops to 0 or below.
        public UnityEvent OnPlayerDeath = new UnityEvent();
        public static GameManager Instance { get; private set; }

        [Header("Player Stats")]
        public int MaxHP = 50;
        public int CurrentHP;
        public int MaxAP = 3;
        public int CurrentAP;
        public int HandSize = 5;
        public const int MaxHandSize = 7;

        [Header("Deck State")]
        public List<CardData> DrawPile = new List<CardData>();
        public List<CardData> Hand = new List<CardData>();
        public List<CardData> DiscardPile = new List<CardData>();

        [Header("Relics")]
        public List<RelicData> ActiveRelics = new List<RelicData>();

        [Header("Combat State")]
        public int CurrentShield;

        [Header("Run State")]
        public int CurrentFloor;
        public int CurrentRoom;

        [Header("Run Stats")]
        public int CardsPlayed;
        public int DamageDealt;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Applies incoming damage: relic reduction first, then shield absorbs, remainder hits HP.
        public void TakeDamage(int amount)
        {
            if (RelicManager.Instance != null)
                amount = Mathf.Max(0, amount - RelicManager.Instance.GetDamageReduction());
            int absorbed = Mathf.Min(CurrentShield, amount);
            CurrentShield -= absorbed;
            CurrentHP -= amount - absorbed;

            if (absorbed > 0)
                AudioManager.Instance?.PlayShieldBlock();
            else if (amount > 0)
                AudioManager.Instance?.PlayDamageHit();

            if (CurrentHP <= 0)
            {
                CurrentHP = 0;
                OnPlayerDeath.Invoke();
            }
        }

        public void InitRun()
        {
            CurrentHP = MaxHP;
            CurrentAP = MaxAP;
            CurrentFloor = 1;
            CurrentRoom = 1;
            CurrentShield = 0;
            DrawPile.Clear();
            Hand.Clear();
            DiscardPile.Clear();
            ActiveRelics.Clear();
            CardsPlayed = 0;
            DamageDealt = 0;
        }
    }
}
