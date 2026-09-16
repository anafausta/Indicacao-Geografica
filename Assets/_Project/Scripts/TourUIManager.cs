using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// RESPONSABILIDADE: Controlar todos os elementos visuais (UI) da cena do Tour.
/// Modificado para suportar feedbacks de carregamento preventivos (evitando telas pretas vazias em VR)
/// e suavizar o cálculo de transição sob oscilações de taxa de quadros no Meta Quest 2.
/// </summary>
public class TourUIManager : MonoBehaviour
{
    [Header("Referências da UI - Quiz")]
    [Tooltip("O componente de texto para exibir a pergunta do quiz.")]
    public TextMeshProUGUI questionTextUI;
    [Tooltip("A lista de botões que servirão como opções de resposta.")]
    public List<Button> answerButtons;
    
    [Header("Referências da UI - Navegação")]
    [Tooltip("Botão para retornar ao Lobby (Menu).")]
    public Button menuButton;

    [Header("Referências da UI - Transição")]
    [Tooltip("Uma imagem preta (Image UI) para o fade.")]
    public Image fadeScreen;
    [Tooltip("Texto que aparece SOBRE a tela preta indicando carregamentos ou próximos locais.")]
    public TextMeshProUGUI transitionTextUI; 

    [Header("Configurações de Feedback Visual")]
    [Tooltip("Cor que o botão de resposta assume ao acertar.")]
    public Color correctColor = new Color(0.1f, 0.7f, 0.2f);
    [Tooltip("Cor que o botão de resposta assume ao errar.")]
    public Color incorrectColor = new Color(0.8f, 0.2f, 0.1f);
    
    [Header("Feedback de Carregamento VR")]
    [Tooltip("Texto que avisa o utilizador que a cena está a ser carregada em background.")]
    public TextMeshProUGUI loadingTextUI;
    [Tooltip("Painel ou imagem de fade para escurecer o menu durante o loading.")]
    public GameObject loadingPanel;

    // Cache de cores originais dos botões para evitar alocações em runtime
    private Dictionary<Button, Color> originalButtonColors = new Dictionary<Button, Color>();

    private void Awake()
    {
        // Salva as cores originais dos botões para resetar posteriormente
        foreach (Button btn in answerButtons)
        {
            if (btn != null)
            {
                originalButtonColors[btn] = btn.image.color;
            }
        }
        
        // Garante que a tela de fade comece ativa para não vazar frames sem carregamento
        if (fadeScreen != null)
        {
            fadeScreen.gameObject.SetActive(true);
            Color c = fadeScreen.color;
            c.a = 1f;
            fadeScreen.color = c;
        }

        // Assegura que o texto de transição esteja escondido por padrão; só aparece entre mapas
        if (transitionTextUI != null)
        {
            transitionTextUI.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Vincula os botões de resposta ao TourManager. Chamado UMA VEZ no início do tour
    /// (o listener não muda entre desafios, então não precisa ser refeito a cada pergunta).
    /// </summary>
    public void BindAnswerListeners(TourManager manager)
    {
        for (int i = 0; i < answerButtons.Count; i++)
        {
            if (answerButtons[i] == null) continue;

            int indexInvariante = i;
            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => manager.CheckAnswer(indexInvariante));
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(() => manager.RequestExitToLobby());
        }

        // Garantir que o texto de transição não apareça durante perguntas/quiz
        HideTransitionText();
    }

    public void SetButtonsInteractable(bool interactable)
    {
        foreach (Button btn in answerButtons)
        {
            if (btn != null && btn.gameObject.activeSelf)
            {
                btn.interactable = interactable;
            }
        }
        if (menuButton != null) menuButton.interactable = interactable;
    }

    public void ApplyButtonFeedback(int index, bool foiCorreto)
    {
        if (index >= 0 && index < answerButtons.Count && answerButtons[index] != null)
        {
            answerButtons[index].image.color = foiCorreto ? correctColor : incorrectColor;
        }
    }

    public void ResetButtonColors()
    {
        foreach (Button btn in answerButtons)
        {
            if (btn != null && originalButtonColors.ContainsKey(btn))
            {
                btn.image.color = originalButtonColors[btn];
            }
        }
    }

    /// <summary>
    /// Exibe uma mensagem centralizada na tela de transição (ex: carregamentos ou nomes de mapas).
    /// </summary>
    public void ShowTransitionText(string mensagem)
    {
        if (transitionTextUI != null)
        {
            transitionTextUI.text = mensagem;
            transitionTextUI.gameObject.SetActive(true);
        }
    }

    public void HideTransitionText()
    {
        if (transitionTextUI != null)
        {
            transitionTextUI.gameObject.SetActive(false);
        }
    }

    // --- CORROTINAS DE TRANSÇÃO VISUAL (FADES DE PRETO) ---

    public IEnumerator FadeIn(float duration)
    {
        if (duration <= 0f)
        {
            SetFadeAlpha(0f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Correção principal aplicada
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            SetFadeAlpha(alpha);
            yield return null;
        }
        SetFadeAlpha(0f);
    }

    public IEnumerator FadeOut(float duration)
    {
        if (duration <= 0f)
        {
            SetFadeAlpha(1f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Correção principal aplicada
            float alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            SetFadeAlpha(alpha);
            yield return null;
        }
        SetFadeAlpha(1f);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeScreen == null) return;
        Color c = fadeScreen.color;
        c.a = Mathf.Clamp01(alpha);
        fadeScreen.color = c;
        // ativa o objeto apenas quando houver opacidade para evitar blocos invisíveis na hierarquia
        fadeScreen.gameObject.SetActive(c.a > 0f);
    }
}