using System.Collections;
using System.Collections.Generic;
using OdisseiaVR.Core;
using OdisseiaVR.Tour.Challenges;
using OdisseiaVR.Tour.Data;
using UnityEngine;

namespace OdisseiaVR.Tour
{
    /// <summary>
    /// ORQUESTRADOR (Mediator): conecta repositório, progresso, handlers, view, áudio e overlay.
    /// Não desenha UI, não toca vídeo, não lê controle e não carrega cena por conta própria:
    /// delega tudo a colaboradores com uma responsabilidade cada.
    /// </summary>
    public sealed class TourManager : MonoBehaviour
    {
        [Header("Conteúdo")]
        [SerializeField] private TextAsset tourDataJson;

        [Header("Colaboradores da cena")]
        [SerializeField] private TourUIManager uiManager;
        [SerializeField] private PanoramaPresenter panorama;
        [SerializeField] private VideoPanoramaPlayer videoPlayer;
        [SerializeField] private TourAudio audioHub;
        [Tooltip("Opcional: deixe vazio para testar no Editor sem headset.")]
        [SerializeField] private XrTourInput xrInput;

        [Header("Configurações")]
        [SerializeField] private TourSettings settings = new TourSettings();

        private ITourRepository repository;
        private TourProgress progress;
        private Dictionary<ChallengeKind, IChallengeHandler> handlers;
        private IChallengeHandler activeHandler;
        private bool isTransitioning;

        private IScreenOverlay Overlay => AppContext.Overlay;

        // ---------------- Ciclo de vida ----------------

        private void Awake()
        {
            if (tourDataJson == null)
            {
                Debug.LogError($"[{nameof(TourManager)}] tourDataJson não foi atribuído no Inspector!");
                enabled = false;
                return;
            }

            repository = new JsonTourRepository(tourDataJson);

            var all = new IChallengeHandler[]
            {
                new QuizChallengeHandler(uiManager, panorama, audioHub, settings.feedbackDelay, this),
                new VideoChallengeHandler(uiManager, panorama, videoPlayer, audioHub)
            };

            handlers = new Dictionary<ChallengeKind, IChallengeHandler>();
            foreach (IChallengeHandler handler in all) handlers[handler.Kind] = handler;
        }

        private void OnEnable()
        {
            uiManager.ExitRequested += RequestExit;

            if (xrInput != null)
            {
                xrInput.ExitRequested += RequestExit;
                xrInput.SkipRequested += OnSkipRequested;
            }
        }

        private void OnDisable()
        {
            uiManager.ExitRequested -= RequestExit;

            if (xrInput != null)
            {
                xrInput.ExitRequested -= RequestExit;
                xrInput.SkipRequested -= OnSkipRequested;
            }
        }

        private IEnumerator Start()
        {
            uiManager.SetInteractable(false);

            int lastIndex = Mathf.Max(0, repository.LocationCount - 1);
            int startIndex = Mathf.Clamp(AppContext.Session.SelectedLocationIndex, 0, lastIndex);

            TourLocation location = null;
            yield return repository.LoadLocationAsync(startIndex, loaded => location = loaded);

            if (!IsPlayable(location))
            {
                Debug.LogError($"[{nameof(TourManager)}] Local {startIndex} inválido ou sem desafios.");
                yield return ReturnToLobby();
                yield break;
            }

            progress = new TourProgress(repository.LocationCount);
            progress.EnterLocation(startIndex, location);
            BeginCurrentChallenge();

            // O conteúdo já está pronto: só agora tiramos a tela preta.
            yield return AppContext.SceneLoader.Reveal(settings.introFadeDuration);
        }

        // ---------------- Fluxo dos desafios ----------------

        private void BeginCurrentChallenge()
        {
            if (activeHandler != null) activeHandler.End();

            Challenge challenge = progress.CurrentChallenge;
            activeHandler = handlers[challenge.Kind];
            activeHandler.Begin(new ChallengeContext(challenge, progress.CurrentLocation, OnChallengeCompleted));
        }

        private void OnChallengeCompleted()
        {
            if (isTransitioning) return;
            StartCoroutine(AdvanceRoutine());
        }

        private IEnumerator AdvanceRoutine()
        {
            isTransitioning = true;

            switch (progress.Advance())
            {
                case AdvanceResult.NextChallenge:
                    yield return Overlay.FadeToOpaque(settings.questionFadeDuration);
                    BeginCurrentChallenge();
                    yield return Overlay.FadeToClear(settings.questionFadeDuration);
                    break;

                case AdvanceResult.NextLocation:
                    audioHub.PlayVictory();
                    yield return GoToNextLocation();
                    break;

                case AdvanceResult.TourFinished:
                    audioHub.PlayVictory();
                    yield return ReturnToLobby();
                    break;
            }

            isTransitioning = false;
        }

        private IEnumerator GoToNextLocation()
        {
            yield return Overlay.FadeToOpaque(settings.mapTransitionFadeDuration);

            int next = progress.LocationIndex + 1;
            Overlay.ShowMessage(repository.GetLocationName(next));
            float shownAt = Time.unscaledTime;

            // Aproveita a tela preta para trocar os assets pesados do Quest.
            if (activeHandler != null) activeHandler.End();
            activeHandler = null;
            panorama.Clear();
            audioHub.StopMusic();
            progress.LeaveLocation();
            yield return repository.ReleaseAssetsAsync();

            TourLocation location = null;
            yield return repository.LoadLocationAsync(next, loaded => location = loaded);

            if (!IsPlayable(location))
            {
                Debug.LogError($"[{nameof(TourManager)}] Local {next} inválido ou sem desafios.");
                yield return ReturnToLobby();
                yield break;
            }

            float remaining = settings.blackScreenHold - (Time.unscaledTime - shownAt);
            if (remaining > 0f) yield return new WaitForSecondsRealtime(remaining);

            progress.EnterLocation(next, location);
            BeginCurrentChallenge();

            Overlay.HideMessage();
            yield return Overlay.FadeToClear(settings.mapTransitionFadeDuration);
        }

        // ---------------- Saída ----------------

        private void RequestExit()
        {
            if (isTransitioning) return;
            StartCoroutine(ReturnToLobby());
        }

        private IEnumerator ReturnToLobby()
        {
            isTransitioning = true;
            uiManager.SetInteractable(false);

            if (activeHandler != null) activeHandler.End();
            activeHandler = null;
            audioHub.StopMusic();

            var request = new SceneLoadRequest
            {
                SceneName = settings.lobbySceneName,
                MessageTemplate = "Retornando ao Menu Principal...\nPor favor, aguarde.",
                FadeOutDuration = settings.mapTransitionFadeDuration,
                RevealDuration = settings.mapTransitionFadeDuration,
                HoldSeconds = settings.lobbyLoadHoldSeconds
            };

            yield return AppContext.SceneLoader.LoadScene(request);

            // Só chega aqui se a troca de cena falhou.
            isTransitioning = false;
        }

        private void OnSkipRequested()
        {
            if (isTransitioning) return;

            if (activeHandler is ISkippable skippable) skippable.Skip();
        }

        private static bool IsPlayable(TourLocation location)
        {
            return location != null && location.Challenges.Count > 0;
        }
    }
}
