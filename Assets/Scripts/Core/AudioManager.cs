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
        public AudioClip cardDraw;
        public AudioClip damageHit;
        public AudioClip shieldBlock;
        public AudioClip gainShield;
        public AudioClip restoreHP;
        public AudioClip statusApplied;
        public AudioClip enemyDeath;
        public AudioClip gameOver;
        public AudioClip turnEnd;
        public AudioClip buttonClick;

        AudioSource _sfxSource;
        AudioSource _musicSource;

        // ── Volume properties (with PlayerPrefs persistence) ──────────────────

        public float MasterVolume
        {
            get => AudioListener.volume;
            set { AudioListener.volume = value; PlayerPrefs.SetFloat("vol_master", value); }
        }

        public float MusicVolume
        {
            get => _musicSource != null ? _musicSource.volume : musicVolume;
            set { musicVolume = value; if (_musicSource != null) _musicSource.volume = value; PlayerPrefs.SetFloat("vol_music", value); }
        }

        public float SfxVolume
        {
            get => _sfxSource != null ? _sfxSource.volume : 1f;
            set { if (_sfxSource != null) _sfxSource.volume = value; PlayerPrefs.SetFloat("vol_sfx", value); }
        }

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

            // Apply saved volumes
            AudioListener.volume     = PlayerPrefs.GetFloat("vol_master", 1f);
            _musicSource.volume      = PlayerPrefs.GetFloat("vol_music", musicVolume);
            _sfxSource.volume        = PlayerPrefs.GetFloat("vol_sfx", 1f);
        }

        public void PlayBattleMusic()
        {
            if (battleMusic == null) return;
            if (_musicSource.isPlaying) return;
            _musicSource.clip = battleMusic;
            _musicSource.Play();
        }

        public void StopMusic() => _musicSource.Stop();

        public void PlayCardPlay()      => Play(cardPlay);
        public void PlayCardDraw()      => Play(cardDraw);
        public void PlayDamageHit()     => Play(damageHit);
        public void PlayShieldBlock()   => Play(shieldBlock);
        public void PlayGainShield()    => Play(gainShield);
        public void PlayRestoreHP()     => Play(restoreHP);
        public void PlayStatusApplied() => Play(statusApplied);
        public void PlayEnemyDeath()    => Play(enemyDeath);
        public void PlayGameOver()      => Play(gameOver);
        public void PlayTurnEnd()       => Play(turnEnd);
        public void PlayButtonClick()   => Play(buttonClick);

        void Play(AudioClip clip)
        {
            if (clip != null) _sfxSource.PlayOneShot(clip);
        }
    }
}
