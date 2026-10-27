using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OdisseiaVR.Core
{
    /// <summary>Parâmetros de uma transição de cena.</summary>
    public sealed class SceneLoadRequest
    {
        public string SceneName;
        /// <summary>Use o token {progress} para inserir a porcentagem.</summary>
        public string MessageTemplate = "{progress}%";
        public float FadeOutDuration = 1f;
        public float HoldSeconds = 2f;
        /// <summary>Duração do fade-in que a cena de destino executará ao chamar Reveal.</summary>
        public float RevealDuration = 1f;
    }

    /// <summary>
    /// Único lugar que sabe transitar entre cenas com fade + barra de progresso.
    /// Antes essa lógica estava copiada no LobbyManager e no TourManager.
    /// </summary>
    public sealed class SceneLoader
    {
        private const string ProgressToken = "{progress}";
        private const float ActivationThreshold = 0.9f;

        private readonly IScreenOverlay overlay;
        private float pendingRevealDuration = -1f;

        public SceneLoader(IScreenOverlay overlay)
        {
            this.overlay = overlay;
        }

        public IEnumerator LoadScene(SceneLoadRequest request)
        {
            if (string.IsNullOrEmpty(request.SceneName))
            {
                Debug.LogError("[SceneLoader] Nome da cena inválido.");
                yield break;
            }

            yield return overlay.FadeToOpaque(request.FadeOutDuration);
            overlay.ShowProgressBar(true);
            Report(request, 0f, -1);

            AsyncOperation operation = SceneManager.LoadSceneAsync(request.SceneName);
            if (operation == null)
            {
                Debug.LogError($"[SceneLoader] Cena '{request.SceneName}' não está no Build Settings.");
                overlay.HideMessage();
                overlay.ShowProgressBar(false);
                yield return overlay.FadeToClear(request.FadeOutDuration);
                yield break;
            }

            operation.allowSceneActivation = false;

            int lastPercent = -1;
            while (operation.progress < ActivationThreshold)
            {
                lastPercent = Report(request, operation.progress / ActivationThreshold, lastPercent);
                yield return null;
            }

            Report(request, 1f, lastPercent);
            yield return new WaitForSecondsRealtime(request.HoldSeconds);

            pendingRevealDuration = request.RevealDuration;
            operation.allowSceneActivation = true;

            // Em caso de sucesso, o chamador é destruído junto com a cena antiga e este
            // ponto nunca é retomado. Só continua em falhas, permitindo ao chamador se recuperar.
            yield return operation;
        }

        /// <summary>
        /// Chamado pela cena recém-carregada quando o conteúdo está pronto.
        /// Evita mostrar uma esfera/menu vazio antes dos assets terminarem de carregar.
        /// </summary>
        public IEnumerator Reveal(float fallbackDuration)
        {
            float duration = pendingRevealDuration >= 0f ? pendingRevealDuration : fallbackDuration;
            pendingRevealDuration = -1f;

            overlay.HideMessage();
            overlay.ShowProgressBar(false);
            yield return overlay.FadeToClear(duration);
        }

        private int Report(SceneLoadRequest request, float progress01, int lastPercent)
        {
            overlay.SetProgress(progress01);

            int percent = Mathf.RoundToInt(progress01 * 100f);
            if (percent != lastPercent)
            {
                overlay.ShowMessage(request.MessageTemplate.Replace(ProgressToken, percent.ToString()));
            }
            return percent;
        }
    }
}
