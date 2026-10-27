using System;
using UnityEngine;

namespace OdisseiaVR.Tour
{
    /// <summary>Parâmetros de ajuste do tour, agrupados em um só bloco no Inspector.</summary>
    [Serializable]
    public sealed class TourSettings
    {
        [Header("Cenas")]
        public string lobbySceneName = "LobbyScene";

        [Header("Feedback")]
        [Tooltip("Tempo (s) que o feedback de cor permanece no botão.")]
        public float feedbackDelay = 1.5f;

        [Header("Transições")]
        [Tooltip("Fade RÁPIDO entre desafios do mesmo local.")]
        public float questionFadeDuration = 0.5f;

        [Tooltip("Fade LENTO e dramático ao trocar de MAPA ou voltar ao Lobby.")]
        public float mapTransitionFadeDuration = 2f;

        [Tooltip("Tempo mínimo na tela preta entre mapas (para ler o nome do local).")]
        public float blackScreenHold = 2.5f;

        [Tooltip("Fade-in inicial se a cena Tour for aberta direto (sem vir do Lobby).")]
        public float introFadeDuration = 2f;

        [Tooltip("Tempo de espera em 100% antes de voltar ao Lobby.")]
        public float lobbyLoadHoldSeconds = 2f;
    }
}
