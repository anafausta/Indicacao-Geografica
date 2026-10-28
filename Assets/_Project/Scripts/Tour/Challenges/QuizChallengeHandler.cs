using System;
using System.Collections;
using OdisseiaVR.Tour.Data;
using UnityEngine;

namespace OdisseiaVR.Tour.Challenges
{
    /// <summary>Executa um desafio de pergunta e resposta sobre um panorama 360, gerando hotspots interativos.</summary>
    public sealed class QuizChallengeHandler : IChallengeHandler
    {
        private readonly ITourView view;
        private readonly PanoramaPresenter panorama;
        private readonly TourAudio audio;
        private readonly HotspotManager hotspotManager; // NOVO
        private readonly MonoBehaviour coroutineHost;
        private readonly float feedbackDelay;

        private QuizChallenge quiz;
        private Action onCompleted;
        private Coroutine feedbackRoutine;

        public ChallengeKind Kind => ChallengeKind.Quiz;

        public QuizChallengeHandler(ITourView view, PanoramaPresenter panorama, TourAudio audio,
                                    HotspotManager hotspotManager, float feedbackDelay, MonoBehaviour coroutineHost)
        {
            this.view = view;
            this.panorama = panorama;
            this.audio = audio;
            this.hotspotManager = hotspotManager; // NOVO
            this.feedbackDelay = feedbackDelay;
            this.coroutineHost = coroutineHost;
        }

        public void Begin(ChallengeContext context)
        {
            quiz = (QuizChallenge)context.Challenge;
            onCompleted = context.OnCompleted;

            audio.PlayMusic(context.Location.BackgroundMusic);
            panorama.Show(quiz.Panorama, quiz.InitialYRotation);

            // Instancia os Hotspots 3D cadastrados para este quiz
            if (hotspotManager != null && quiz.Hotspots != null)
            {
                hotspotManager.SpawnHotspots(quiz.Hotspots, quiz.InitialYRotation);
            }

            view.AnswerSelected += OnAnswerSelected;
            view.ShowQuiz(quiz.Question, quiz.Answers);
        }

        public void End()
        {
            view.AnswerSelected -= OnAnswerSelected;

            // Limpa os hotspots do cenário ao encerrar o desafio
            if (hotspotManager != null)
            {
                hotspotManager.ClearHotspots();
            }

            if (feedbackRoutine != null && coroutineHost != null)
            {
                coroutineHost.StopCoroutine(feedbackRoutine);
            }

            feedbackRoutine = null;
            quiz = null;
            onCompleted = null;
        }

        private void OnAnswerSelected(int index)
        {
            // Ignora cliques enquanto o feedback do clique anterior ainda está em andamento.
            if (quiz == null || feedbackRoutine != null) return;

            feedbackRoutine = coroutineHost.StartCoroutine(ShowFeedback(index));
        }

        private IEnumerator ShowFeedback(int index)
        {
            bool isCorrect = quiz.IsCorrect(index);

            view.SetInteractable(false);
            view.MarkAnswer(index, isCorrect);
            if (isCorrect) audio.PlayCorrect(); else audio.PlayIncorrect();

            yield return new WaitForSeconds(feedbackDelay);

            feedbackRoutine = null;

            if (isCorrect)
            {
                // Limpa os hotspots antes de avançar para a próxima etapa
                if (hotspotManager != null)
                {
                    hotspotManager.ClearHotspots();
                }

                onCompleted?.Invoke();
            }
            else
            {
                // Errou: reapresenta a mesma pergunta (restaura cores e botões).
                view.ShowQuiz(quiz.Question, quiz.Answers);
            }
        }
    }
}