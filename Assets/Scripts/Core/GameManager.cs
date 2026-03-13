using System.Collections.Generic;
using UnityEngine;
using InboxZero.Data;

namespace InboxZero.Core
{
    public class GameManager : MonoBehaviour
    {
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

        public void InitRun()
        {
            CurrentHP = MaxHP;
            CurrentAP = MaxAP;
            CurrentFloor = 1;
            CurrentRoom = 1;
            DrawPile.Clear();
            Hand.Clear();
            DiscardPile.Clear();
            ActiveRelics.Clear();
        }
    }
}
