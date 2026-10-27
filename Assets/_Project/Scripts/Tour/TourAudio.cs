using UnityEngine;

namespace OdisseiaVR.Tour
{
    /// <summary>
    /// Fachada de áudio do Tour. Música e SFX usam AudioSources distintos,
    /// então tocar um efeito nunca interfere na trilha (e vice-versa).
    /// </summary>
    public sealed class TourAudio : MonoBehaviour
    {
        [Header("Fontes de áudio")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Efeitos Sonoros (SFX)")]
        [SerializeField] private AudioClip correctSfx;
        [SerializeField] private AudioClip incorrectSfx;
        [SerializeField] private AudioClip victorySfx;

        /// <summary>Toca a trilha do local; não reinicia se a mesma já está tocando.</summary>
        public void PlayMusic(AudioClip clip)
        {
            if (clip == null) return;
            if (musicSource.isPlaying && musicSource.clip == clip) return;

            musicSource.clip = clip;
            musicSource.Play();
        }

        public void StopMusic()
        {
            musicSource.Stop();
            musicSource.clip = null; // solta a referência para o descarregamento de assets
        }

        public void PlayCorrect() => PlaySfx(correctSfx);

        public void PlayIncorrect() => PlaySfx(incorrectSfx);

        public void PlayVictory() => PlaySfx(victorySfx);

        private void PlaySfx(AudioClip clip)
        {
            if (clip != null) sfxSource.PlayOneShot(clip);
        }
    }
}
