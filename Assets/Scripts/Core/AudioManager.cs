using UnityEngine;

namespace InboxZero.Core
{
    /// Centralised SFX playback. Wire AudioClip references in the inspector.
    /// Clips are optional — if null the call is a no-op until assets are added.
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Music")]
        public AudioClip battleMusic;
        [Range(0f, 1f)] public float musicVolume = 0.5f;

        [Header("SFX Clips")]
        public AudioClip cardPlay;
        public AudioClip damageHit;
        public AudioClip shieldBlock;
        public AudioClip enemyDeath;
        public AudioClip turnEnd;

        AudioSource _sfxSource;
        AudioSource _musicSource;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.volume = musicVolume;
        }

        public void PlayBattleMusic()
        {
            if (battleMusic == null) return;
            if (_musicSource.isPlaying) return;
            _musicSource.clip = battleMusic;
            _musicSource.Play();
        }

        public void StopMusic() => _musicSource.Stop();

        public void PlayCardPlay()    => Play(cardPlay);
        public void PlayDamageHit()   => Play(damageHit);
        public void PlayShieldBlock() => Play(shieldBlock);
        public void PlayEnemyDeath()  => Play(enemyDeath);
        public void PlayTurnEnd()     => Play(turnEnd);

        void Play(AudioClip clip)
        {
            if (clip != null) _sfxSource.PlayOneShot(clip);
        }
    }
}
