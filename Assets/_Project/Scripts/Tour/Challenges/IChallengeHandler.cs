using System;
using OdisseiaVR.Tour.Data;

namespace OdisseiaVR.Tour.Challenges
{
    /// <summary>Tudo que um handler precisa para executar um desafio.</summary>
    public readonly struct ChallengeContext
    {
        public Challenge Challenge { get; }
        public TourLocation Location { get; }
        public Action OnCompleted { get; }

        public ChallengeContext(Challenge challenge, TourLocation location, Action onCompleted)
        {
            Challenge = challenge;
            Location = location;
            OnCompleted = onCompleted;
        }
    }

    /// <summary>
    /// Strategy: cada tipo de desafio sabe se apresentar e avisar quando terminou.
    /// Um novo tipo (ex.: puzzle) vira uma nova classe + 1 linha de registro no TourManager,
    /// sem alterar código existente (OCP).
    /// </summary>
    public interface IChallengeHandler
    {
        ChallengeKind Kind { get; }

        void Begin(ChallengeContext context);

        /// <summary>Encerra e limpa tudo (listeners, corrotinas, mídia). Seguro de chamar mais de uma vez.</summary>
        void End();
    }

    /// <summary>Capacidade opcional: desafios que o usuário pode pular (ex.: vídeo).</summary>
    public interface ISkippable
    {
        void Skip();
    }
}
