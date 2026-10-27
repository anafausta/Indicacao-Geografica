using System;
using System.Collections.Generic;

namespace OdisseiaVR.Tour
{
    /// <summary>
    /// Contrato da UI do Tour. Os handlers e o TourManager falam só com esta interface,
    /// sem mexer em botões ou textos (Lei de Demeter), e a view avisa por eventos (Observer).
    /// </summary>
    public interface ITourView
    {
        event Action<int> AnswerSelected;
        event Action ExitRequested;

        /// <summary>Mostra pergunta + respostas, restaura cores e habilita os botões.</summary>
        void ShowQuiz(string question, IReadOnlyList<string> answers);

        /// <summary>Esconde pergunta, respostas e botão de menu (modo vídeo).</summary>
        void HideQuiz();

        void SetInteractable(bool interactable);

        void MarkAnswer(int index, bool isCorrect);
    }
}
