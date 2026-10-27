using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace OdisseiaVR.Tour
{
    /// <summary>
    /// View do quiz. Não conhece o TourManager: apenas dispara eventos.
    /// O fade e o texto de transição agora são do PersistentFade (overlay único).
    /// Campos antigos (fadeScreen, transitionTextUI, loadingTextUI, loadingPanel) foram removidos.
    /// </summary>
    public sealed class TourUIManager : MonoBehaviour, ITourView
    {
        [Header("Referências da UI - Quiz")]
        public TextMeshProUGUI questionTextUI;
        public List<Button> answerButtons;

        [Header("Referências da UI - Navegação")]
        public Button menuButton;

        [Header("Feedback Visual")]
        public Color correctColor = new Color(0.1f, 0.7f, 0.2f);
        public Color incorrectColor = new Color(0.8f, 0.2f, 0.1f);

        public event Action<int> AnswerSelected;
        public event Action ExitRequested;

        private readonly Dictionary<Button, Color> originalColors = new Dictionary<Button, Color>();
        private readonly List<TMP_Text> answerLabels = new List<TMP_Text>();
        private UnityAction[] answerActions;
        private UnityAction exitAction;

        private void Awake()
        {
            answerActions = new UnityAction[answerButtons.Count];

            for (int i = 0; i < answerButtons.Count; i++)
            {
                Button button = answerButtons[i];
                answerLabels.Add(button != null ? button.GetComponentInChildren<TMP_Text>(true) : null);

                int index = i; // captura estável para o closure
                answerActions[i] = () => AnswerSelected?.Invoke(index);

                if (button != null) originalColors[button] = button.image.color;
            }

            exitAction = () => ExitRequested?.Invoke();
        }

        private void OnEnable()
        {
            for (int i = 0; i < answerButtons.Count; i++)
            {
                if (answerButtons[i] != null) answerButtons[i].onClick.AddListener(answerActions[i]);
            }
            if (menuButton != null) menuButton.onClick.AddListener(exitAction);
        }

        private void OnDisable()
        {
            for (int i = 0; i < answerButtons.Count; i++)
            {
                if (answerButtons[i] != null) answerButtons[i].onClick.RemoveListener(answerActions[i]);
            }
            if (menuButton != null) menuButton.onClick.RemoveListener(exitAction);
        }

        public void ShowQuiz(string question, IReadOnlyList<string> answers)
        {
            if (answers.Count > answerButtons.Count)
            {
                Debug.LogWarning($"[TourUIManager] {answers.Count} respostas, mas só {answerButtons.Count} botões.");
            }

            questionTextUI.gameObject.SetActive(true);
            questionTextUI.text = question;
            if (menuButton != null) menuButton.gameObject.SetActive(true);

            for (int i = 0; i < answerButtons.Count; i++)
            {
                Button button = answerButtons[i];
                if (button == null) continue;

                bool hasAnswer = i < answers.Count;
                button.gameObject.SetActive(hasAnswer);
                if (!hasAnswer) continue;

                if (answerLabels[i] != null) answerLabels[i].text = answers[i];
                button.image.color = originalColors[button];
            }

            SetInteractable(true);
        }

        public void HideQuiz()
        {
            questionTextUI.gameObject.SetActive(false);
            foreach (Button button in answerButtons)
            {
                if (button != null) button.gameObject.SetActive(false);
            }
            if (menuButton != null) menuButton.gameObject.SetActive(false);
        }

        public void SetInteractable(bool interactable)
        {
            foreach (Button button in answerButtons)
            {
                if (button != null && button.gameObject.activeSelf) button.interactable = interactable;
            }
            if (menuButton != null) menuButton.interactable = interactable;
        }

        public void MarkAnswer(int index, bool isCorrect)
        {
            if (index < 0 || index >= answerButtons.Count || answerButtons[index] == null) return;
            answerButtons[index].image.color = isCorrect ? correctColor : incorrectColor;
        }
    }
}
