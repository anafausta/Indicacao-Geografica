using System.Collections;

namespace OdisseiaVR.Core
{
    /// <summary>
    /// Abstração da tela preta com mensagem e barra de progresso.
    /// Quem orquestra fluxo depende disto, não do PersistentFade concreto (DIP).
    /// </summary>
    public interface IScreenOverlay
    {
        IEnumerator FadeToOpaque(float duration);
        IEnumerator FadeToClear(float duration);
        void ShowMessage(string message);
        void HideMessage();
        void ShowProgressBar(bool show);
        void SetProgress(float value01);
    }
}
