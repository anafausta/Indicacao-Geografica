using System;
using OdisseiaVR.Tour.Data;

namespace OdisseiaVR.Tour.Challenges
{
    /// <summary>
    /// Executa um vídeo 360. A flag "completed" garante conclusão ÚNICA: antes, o botão A (pular)
    /// e o evento de fim do vídeo podiam ambos chamar AvancarParaProximoDesafio().
    /// </summary>
    public sealed class VideoChallengeHandler : IChallengeHandler, ISkippable
    {
        private readonly ITourView view;
        private readonly PanoramaPresenter panorama;
        private readonly VideoPanoramaPlayer player;
        private readonly TourAudio audio;

        private Action onCompleted;
        private bool completed;

        public ChallengeKind Kind => ChallengeKind.Video;

        public VideoChallengeHandler(ITourView view, PanoramaPresenter panorama,
                                     VideoPanoramaPlayer player, TourAudio audio)
        {
            this.view = view;
            this.panorama = panorama;
            this.player = player;
            this.audio = audio;
        }

        public void Begin(ChallengeContext context)
        {
            var video = (VideoChallenge)context.Challenge;
            onCompleted = context.OnCompleted;
            completed = false;

            view.HideQuiz();
            audio.StopMusic();
            panorama.Show(player.PanoramaMaterial, video.InitialYRotation);

            player.Finished += Complete;
            player.Play(video.Clip);
        }

        public void Skip()
        {
            if (completed) return;

            player.Stop(); // corta imagem e áudio imediatamente
            Complete();
        }

        public void End()
        {
            player.Finished -= Complete;
            player.Stop();
            onCompleted = null;
        }

        private void Complete()
        {
            if (completed) return;
            completed = true;

            onCompleted?.Invoke();
        }
    }
}
