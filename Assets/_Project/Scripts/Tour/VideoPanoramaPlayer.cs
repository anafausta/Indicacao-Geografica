using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;

namespace OdisseiaVR.Tour
{
    /// <summary>
    /// Encapsula o VideoPlayer para vídeo 360. Cria a RenderTexture UMA vez (o código antigo criava
    /// duas e nunca liberava) e a libera no OnDestroy. Expõe um evento Finished (Observer).
    /// </summary>
    public sealed class VideoPanoramaPlayer : MonoBehaviour
    {
        [SerializeField] private VideoPlayer player;
        [SerializeField] private AudioSource audioSource;

        [Tooltip("Material com shader PanoramaUnlit_URP e textura vazia.")]
        [SerializeField] private Material materialTemplate;

        [SerializeField] private int textureWidth = 2048;
        [SerializeField] private int textureHeight = 1024;

        public event Action Finished;

        /// <summary>Material pronto para a esfera exibir o vídeo.</summary>
        public Material PanoramaMaterial { get; private set; }

        private RenderTexture renderTexture;
        private Coroutine pendingRoutine;

        private void Awake()
        {
            if (player == null) player = GetComponent<VideoPlayer>();

            renderTexture = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32);
            renderTexture.Create();

            player.playOnAwake = false;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = renderTexture;
            player.audioOutputMode = VideoAudioOutputMode.AudioSource;
            player.SetTargetAudioSource(0, audioSource);

            if (materialTemplate != null)
            {
                PanoramaMaterial = new Material(materialTemplate) { mainTexture = renderTexture };
            }
            else
            {
                Debug.LogError("[VideoPanoramaPlayer] materialTemplate não atribuído.");
            }
        }

        private void OnEnable()
        {
            player.loopPointReached += OnLoopPointReached;
            player.errorReceived += OnError;
        }

        private void OnDisable()
        {
            player.loopPointReached -= OnLoopPointReached;
            player.errorReceived -= OnError;
        }

        private void OnDestroy()
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
            if (PanoramaMaterial != null) Destroy(PanoramaMaterial);
        }

        public void Play(VideoClip clip)
        {
            Stop();

            if (clip == null)
            {
                Debug.LogError("[VideoPanoramaPlayer] VideoClip nulo: pulando o desafio.");
                pendingRoutine = StartCoroutine(FinishNextFrame());
                return;
            }

            player.clip = clip;
            player.Prepare();
            pendingRoutine = StartCoroutine(PlayWhenPrepared());
        }

        public void Stop()
        {
            if (pendingRoutine != null)
            {
                StopCoroutine(pendingRoutine);
                pendingRoutine = null;
            }

            player.Stop();
            player.clip = null; // solta o asset para o Garbage Collector
        }

        private IEnumerator PlayWhenPrepared()
        {
            while (!player.isPrepared) yield return null;

            pendingRoutine = null;
            player.Play();
        }

        private IEnumerator FinishNextFrame()
        {
            yield return null;
            pendingRoutine = null;
            Finished?.Invoke();
        }

        private void OnLoopPointReached(VideoPlayer source) => Finished?.Invoke();

        // Sem isto, um vídeo corrompido travaria o tour para sempre.
        private void OnError(VideoPlayer source, string message)
        {
            Debug.LogError($"[VideoPanoramaPlayer] Erro no vídeo: {message}");
            Finished?.Invoke();
        }
    }
}
