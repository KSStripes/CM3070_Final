using UnityEngine;

namespace CM3070.Dungeon1
{
    // One-shot audio hub for UI and office feedback sounds.
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioManager : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float volume = 0.75f;
        [SerializeField] private AudioClip buttonClick;
        [SerializeField] private AudioClip itemPickup;
        [SerializeField] private AudioClip taskComplete;
        [SerializeField] private AudioClip exitUnlocked;
        [SerializeField] private AudioClip resolveDamage;
        [SerializeField] private AudioClip resolveWarning;
        [SerializeField] private AudioClip dayComplete;
        [SerializeField] private AudioClip gameOver;
        [SerializeField] private AudioClip gameWon;

        private AudioSource source;

        public static AudioManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
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

        private void Play(AudioClip clip)
        {
            if (clip != null && source != null)
            {
                source.PlayOneShot(clip, volume);
            }
        }
    }
}
