using UnityEngine;

namespace CubeNinja.Gameplay
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioFeedbackPlayer : MonoBehaviour
    {
        [SerializeField] private float masterVolume = 0.65f;
        [SerializeField] private float comboPitchStep = 0.08f;

        private AudioSource audioSource;
        private AudioClip gameStartClip;
        private AudioClip scoreClip;
        private AudioClip lifeLostClip;
        private AudioClip gameOverClip;

        private void Awake()
        {
            EnsureAudioSource();
            BuildClips();
        }

        public void PlayGameStart()
        {
            EnsureAudioSource();
            BuildClips();
            Play(gameStartClip, 1f, 0.8f);
        }

        public void PlayScore(int comboMultiplier)
        {
            EnsureAudioSource();
            BuildClips();

            var pitch = 1f + Mathf.Clamp(comboMultiplier - 1, 0, 12) * comboPitchStep;
            Play(scoreClip, pitch, 0.7f);
        }

        public void PlayLifeLost()
        {
            EnsureAudioSource();
            BuildClips();
            Play(lifeLostClip, 1f, 0.9f);
        }

        public void PlayGameOver()
        {
            EnsureAudioSource();
            BuildClips();
            Play(gameOverClip, 1f, 1f);
        }

        private void EnsureAudioSource()
        {
            if (audioSource != null)
            {
                return;
            }

            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        private void BuildClips()
        {
            if (scoreClip != null)
            {
                return;
            }

            gameStartClip = CreateClip("CubeNinja Start", 0.34f, t =>
            {
                var frequency = t < 0.12f ? 440f : t < 0.23f ? 660f : 880f;
                return Tone(t, frequency, 0.34f, 0.015f, 0.09f);
            });

            scoreClip = CreateClip("CubeNinja Score", 0.11f, t =>
            {
                var frequency = Mathf.Lerp(760f, 1180f, t / 0.11f);
                return Tone(t, frequency, 0.11f, 0.006f, 0.045f);
            });

            lifeLostClip = CreateClip("CubeNinja Life Lost", 0.28f, t =>
            {
                var progress = t / 0.28f;
                var frequency = Mathf.Lerp(260f, 92f, progress);
                var square = Mathf.Sign(Mathf.Sin(Mathf.PI * 2f * frequency * t));
                return square * Envelope(t, 0.28f, 0.01f, 0.11f) * 0.55f;
            });

            gameOverClip = CreateClip("CubeNinja Game Over", 0.64f, t =>
            {
                var progress = t / 0.64f;
                var frequency = Mathf.Lerp(310f, 70f, progress);
                return Tone(t, frequency, 0.64f, 0.02f, 0.2f) * (1f - progress * 0.25f);
            });
        }

        private void Play(AudioClip clip, float pitch, float volumeScale)
        {
            if (clip == null)
            {
                return;
            }

            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, Mathf.Clamp01(masterVolume * volumeScale));
        }

        private static AudioClip CreateClip(string name, float durationSeconds, System.Func<float, float> sampleProvider)
        {
            const int sampleRate = 44100;
            var sampleCount = Mathf.CeilToInt(durationSeconds * sampleRate);
            var samples = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var time = i / (float)sampleRate;
                samples[i] = Mathf.Clamp(sampleProvider(time), -1f, 1f);
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float Tone(float time, float frequency, float duration, float attack, float release)
        {
            var fundamental = Mathf.Sin(Mathf.PI * 2f * frequency * time);
            var harmonic = Mathf.Sin(Mathf.PI * 2f * frequency * 2f * time) * 0.18f;
            return (fundamental + harmonic) * Envelope(time, duration, attack, release) * 0.55f;
        }

        private static float Envelope(float time, float duration, float attack, float release)
        {
            var attackGain = attack <= 0f ? 1f : Mathf.Clamp01(time / attack);
            var releaseGain = release <= 0f ? 1f : Mathf.Clamp01((duration - time) / release);
            return Mathf.Min(attackGain, releaseGain);
        }
    }
}
