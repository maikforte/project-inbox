using System.Collections.Generic;
using InboxZero.Data;
using UnityEngine;

namespace InboxZero.Core
{
    /// Tracks which cards are unlocked and which are enabled for drop rewards.
    /// Starter + Common are always unlocked. Others unlock via Unlock().
    /// Toggle state and unlocks persist across runs via PlayerPrefs.
    public class UnlockManager : MonoBehaviour
    {
        public static UnlockManager Instance { get; private set; }

        const string UnlockedKey = "IZ_Unlocked";
        const string DisabledKey = "IZ_Disabled";

        HashSet<string> _unlocked = new();
        HashSet<string> _disabled = new();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        // ── Queries ───────────────────────────────────────────────────────────

        public bool IsUnlocked(CardData card)
        {
            if (card.rarity == CardRarity.Starter || card.rarity == CardRarity.Common)
                return true;
            return _unlocked.Contains(card.cardName);
        }

        /// Returns true if the card can appear as a run reward.
        public bool IsEnabled(CardData card)
        {
            if (!IsUnlocked(card)) return false;
            if (card.rarity == CardRarity.Starter) return true;
            return !_disabled.Contains(card.cardName);
        }

        // ── Mutations ─────────────────────────────────────────────────────────

        public void Unlock(CardData card)
        {
            if (_unlocked.Add(card.cardName)) Save();
        }

        /// Enable or disable a card in the drop pool. Has no effect on Starter cards.
        public void SetEnabled(CardData card, bool enabled)
        {
            if (card.rarity == CardRarity.Starter) return;
            bool changed = enabled ? _disabled.Remove(card.cardName) : _disabled.Add(card.cardName);
            if (changed) Save();
        }

        // ── Persistence ───────────────────────────────────────────────────────

        void Save()
        {
            PlayerPrefs.SetString(UnlockedKey, string.Join(",", _unlocked));
            PlayerPrefs.SetString(DisabledKey, string.Join(",", _disabled));
            PlayerPrefs.Save();
        }

        void Load()
        {
            var u = PlayerPrefs.GetString(UnlockedKey, "");
            var d = PlayerPrefs.GetString(DisabledKey, "");
            _unlocked = string.IsNullOrEmpty(u)
                ? new HashSet<string>()
                : new HashSet<string>(u.Split(','));
            _disabled = string.IsNullOrEmpty(d)
                ? new HashSet<string>()
                : new HashSet<string>(d.Split(','));
        }
    }
}
