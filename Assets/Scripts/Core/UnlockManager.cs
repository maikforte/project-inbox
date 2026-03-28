using System.Collections.Generic;
using InboxZero.Data;
using UnityEngine;

namespace InboxZero.Core
{
    /// Tracks which cards are unlocked and which are enabled for drop rewards.
    /// Starter + Common are always unlocked. Others unlock via Unlock() / UnlockRarity().
    /// Toggle state and unlocks persist across runs via PlayerPrefs.
    public class UnlockManager : MonoBehaviour
    {
        public static UnlockManager Instance { get; private set; }

        const string UnlockedKey = "IZ_Unlocked";
        const string DisabledKey = "IZ_Disabled";

        // Pool minimums — cannot disable below these counts.
        public const int MinCommon   = 4;
        public const int MinUncommon = 3;
        public const int MinRare     = 1;

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

        /// Returns true if disabling this card would NOT violate the pool minimums.
        /// <paramref name="reason"/> is set to an uppercase message when it would violate.
        public bool CanDisable(CardData card, AllCardsRegistry registry, out string reason)
        {
            reason = string.Empty;
            if (card.rarity == CardRarity.Starter) { reason = "STARTER ALWAYS ON"; return false; }
            if (!IsUnlocked(card))                 { reason = "LOCKED";             return false; }
            if (_disabled.Contains(card.cardName)) return true; // already disabled, toggling on is always fine

            int min = MinForRarity(card.rarity);
            if (min <= 0) return true; // no minimum for this rarity (e.g. Legendary)

            // Count how many enabled cards of this rarity remain after hypothetically disabling this one.
            int enabledCount = 0;
            if (registry != null)
            {
                foreach (var c in registry.allCards)
                    if (c != null && c.rarity == card.rarity && IsEnabled(c) && c != card)
                        enabledCount++;
            }

            if (enabledCount < min)
            {
                reason = $"NEED {min} {card.rarity.ToString().ToUpper()} ENABLED";
                return false;
            }
            return true;
        }

        // ── Mutations ─────────────────────────────────────────────────────────

        public void Unlock(CardData card)
        {
            if (card == null) return;
            if (_unlocked.Add(card.cardName)) Save();
        }

        /// Unlocks all cards of the given rarity that exist in the registry.
        public void UnlockRarity(CardRarity rarity, AllCardsRegistry registry)
        {
            if (registry == null) return;
            bool changed = false;
            foreach (var card in registry.allCards)
                if (card != null && card.rarity == rarity)
                    changed |= _unlocked.Add(card.cardName);
            if (changed) Save();
        }

        /// Enable or disable a card in the drop pool. Has no effect on Starter cards.
        /// Does NOT enforce minimums — call CanDisable() first if you need to guard.
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

        // ── Helpers ───────────────────────────────────────────────────────────

        static int MinForRarity(CardRarity rarity) => rarity switch
        {
            CardRarity.Common   => MinCommon,
            CardRarity.Uncommon => MinUncommon,
            CardRarity.Rare     => MinRare,
            _                   => 0,
        };
    }
}
