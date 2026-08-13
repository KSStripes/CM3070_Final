using UnityEngine;

namespace CM3070.Dungeon1
{
    // Scene audio hub for UI/gameplay one-shots and background music.
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioManager : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.75f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;

        [Header("Sound Effects")]
        [SerializeField] private AudioClip buttonClick;
        [SerializeField] private AudioClip itemPickup;
        [SerializeField] private AudioClip taskComplete;
        [SerializeField] private AudioClip exitUnlocked;
        [SerializeField] private AudioClip resolveDamage;
        [SerializeField] private AudioClip resolveWarning;
        [SerializeField] private AudioClip dayComplete;
        [SerializeField] private AudioClip gameOver;
        [SerializeField] private AudioClip gameWon;

        [Header("Music")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip[] dayMusic;

        private AudioSource sfxSource;

        public static AudioManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            sfxSource = GetComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            musicSource ??= sfxSource;
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
        }

        public void PlayButtonClick() => Play(buttonClick);
        public void PlayItemPickup() => Play(itemPickup);
        public void PlayTaskComplete() => Play(taskComplete);
        public void PlayExitUnlocked() => Play(exitUnlocked);
        public void PlayResolveDamage() => Play(resolveDamage);
        public void PlayResolveWarning() => Play(resolveWarning);
        public void PlayDayComplete() => Play(dayComplete);
        public void PlayGameOver() => Play(gameOver);
        public void PlayGameWon() => Play(gameWon);
        public void PlayMenuMusic() => PlayMusic(menuMusic);
        public void PlayGameplayMusic(int day)
        {
            int index = day - 1;
            if (dayMusic == null
                || index < 0
                || index >= dayMusic.Length)
            {
                return;
            }

            PlayMusic(dayMusic[index]);
        }

        private void Play(AudioClip clip)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, sfxVolume);
            }
        }

        private void PlayMusic(AudioClip clip)
        {
            if (clip == null || musicSource == null || musicSource.clip == clip)
            {
                return;
            }

            musicSource.clip = clip;
            musicSource.volume = musicVolume;
            musicSource.loop = true;
            musicSource.Play();
        }
    }
}
