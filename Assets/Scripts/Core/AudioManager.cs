using UnityEngine;

namespace InboxZero.Core
{
    /// Centralised SFX playback. Wire AudioClip references in the inspector.
    /// Clips are optional — if null the call is a no-op until assets are added.
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("SFX Clips")]
        public AudioClip cardPlay;
        public AudioClip damageHit;
        public AudioClip shieldBlock;
        public AudioClip enemyDeath;
        public AudioClip turnEnd;

        AudioSource _source;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
        }

        public void PlayCardPlay()    => Play(cardPlay);
        public void PlayDamageHit()   => Play(damageHit);
        public void PlayShieldBlock() => Play(shieldBlock);
        public void PlayEnemyDeath()  => Play(enemyDeath);
        public void PlayTurnEnd()     => Play(turnEnd);

        void Play(AudioClip clip)
        {
            if (clip != null) _source.PlayOneShot(clip);
        }
    }
}
