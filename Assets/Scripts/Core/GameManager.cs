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

        // Applies incoming damage: shield absorbs first, remainder hits HP.
        public void TakeDamage(int amount)
        {
            int absorbed = Mathf.Min(CurrentShield, amount);
            CurrentShield -= absorbed;
            CurrentHP -= amount - absorbed;
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
        }
    }
}
