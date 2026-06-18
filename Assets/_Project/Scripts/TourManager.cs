using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Video;
using TMPro;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(TourDataManager))]
[RequireComponent(typeof(TourUIManager))]
[RequireComponent(typeof(VideoPlayer))]
public class TourManager : MonoBehaviour
{
    [Header("Componentes (Auto-detecta no Awake)")]
    [SerializeField] private TourDataManager dataManager;
    [SerializeField] private TourUIManager uiManager;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private VideoPlayer videoPlayer;
    
    [Header("Referências da Cena")]
    [Tooltip("O objeto Renderer da esfera que exibirá o panorama 360°.")]
    public Renderer panoramaSphereRenderer;
    
    [Header("Configurações de Cena")]
    public string lobbySceneName = "LobbyScene";
    
    [Header("Configurações de Feedback")]
    [Tooltip("Tempo (em segundos) que o feedback de cor permanece no botão.")]
    public float feedbackDelay = 1.5f;

    [Header("Configurações de Transição (Fades)")]
    [Tooltip("Fade RÁPIDO entre perguntas do mesmo local.")]
    public float questionFadeDuration = 0.5f; 
    
    [Tooltip("Fade LENTO e dramático ao trocar de MAPA.")]
    public float mapTransitionFadeDuration = 2.0f;

    [Tooltip("Tempo extra na tela preta entre mapas (para ler o texto descritivo).")]
    public float waitOnBlackScreenDelay = 2.5f;

    private int localAtualIndex = 0;
    private int desafioAtualIndex = 0;
    private List<DadosLocal> locais;
    private bool isProcessingAnswer = false;

    private void Awake()
    {
        if (dataManager == null) dataManager = GetComponent<TourDataManager>();
        if (uiManager == null) uiManager = GetComponent<TourUIManager>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();

        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
        videoPlayer.targetMaterialRenderer = panoramaSphereRenderer;
        videoPlayer.targetMaterialProperty = "_MainTex";

        videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }

    private void CarregarDadosDoLocal(int index)
    {
        if (locais == null || locais.Count == 0) return;
        if (index < 0 || index >= locais.Count) index = 0;
        localAtualIndex = index;
        desafioAtualIndex = 0;
        InicializarDesafioAtual();
    }

    private IEnumerator Start()
    {
        uiManager.SetButtonsInteractable(false);
        yield return StartCoroutine(uiManager.FadeOut(0.0f));

        while (dataManager == null || !dataManager.IsDataLoaded)
        {
            yield return null;
        }

        locais = dataManager.Locais;

        if (GameSettings.Instance != null)
        {
            localAtualIndex = GameSettings.Instance.selectedLocationIndex;
            if (localAtualIndex < 0 || localAtualIndex >= locais.Count)
            {
                localAtualIndex = 0;
            }
        }
        else
        {
            localAtualIndex = 0;
        }

        desafioAtualIndex = 0;
        CarregarDadosDoLocal(localAtualIndex);

        yield return StartCoroutine(uiManager.FadeIn(mapTransitionFadeDuration));
    }

    private void CarregarDesafioAtual()
    {
        Desafio desafio = locais[localAtualIndex].desafios[desafioAtualIndex];

        if (desafio.IsVideo)
        {
            ExecutarFluxoVideo(desafio);
        }
        else
        {
            // Se houver vídeo anterior tocando, limpa a memória gráfica dele imediatamente
            if (videoPlayer.isPlaying || videoPlayer.clip != null)
            {
                videoPlayer.Stop();
                videoPlayer.clip = null; 
            }

            panoramaSphereRenderer.sharedMaterial = desafio.panoramaMaterial;
            // CORREÇÃO: Força a rotação correta definida no JSON também para imagens estáticas
            panoramaSphereRenderer.transform.rotation = Quaternion.Euler(0, desafio.initialYRotation, 0);
            
            uiManager.SetupQuiz(desafio.questionText, desafio.answers, this);
        }
    }

    // --- SISTEMA DE ORQUESTRAÇÃO INTEGRADO (MANTENDO SEUS MÉTODOS) ---

    private void InicializarDesafioAtual()
    {
        Desafio desafio = locais[localAtualIndex].desafios[desafioAtualIndex];

        // Se o desafio ATUAL for uma imagem, limpamos o vídeo ANTES para liberar RAM/VRAM
        if (!desafio.IsVideo)
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
                videoPlayer.clip = null; // Corta a referência do asset para liberar o Garbage Collector
            }
        }

        if (desafio.IsVideo)
        {
            ExecutarFluxoVideo(desafio);
        }
        else
        {
            ExecutarFluxoImagemQuiz(desafio);
        }
    }

    private void ExecutarFluxoImagemQuiz(Desafio desafio)
    {
        // Certifica-se de que o player está completamente limpo e parado
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        videoPlayer.clip = null; // Garantia dupla de liberação de memória para o Quest

        uiManager.questionTextUI.gameObject.SetActive(true);
        
        if (!audioSource.isPlaying && locais[localAtualIndex].backgroundMusic != null)
        {
            audioSource.clip = locais[localAtualIndex].backgroundMusic;
            audioSource.Play();
        }

        if (desafio.panoramaMaterial != null)
        {
            panoramaSphereRenderer.material = desafio.panoramaMaterial;
        }

        panoramaSphereRenderer.transform.rotation = Quaternion.Euler(0, desafio.initialYRotation, 0);

        ConfigurarDesafioAtual(desafio);
    }

    private void ExecutarFluxoVideo(Desafio desafio)
    {
        if (audioSource.isPlaying) audioSource.Stop();

        uiManager.questionTextUI.gameObject.SetActive(false);
        foreach (var botao in uiManager.answerButtons)
        {
            botao.gameObject.SetActive(false);
        }

        videoPlayer.clip = desafio.videoClip;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, audioSource);

        panoramaSphereRenderer.transform.rotation = Quaternion.Euler(0, desafio.initialYRotation, 0);
        
        // Inicia o preparo nativo do Quest e delega para a Corrotina de espera segura
        videoPlayer.Prepare();
        StartCoroutine(AguardarEPlayVideo());
    }

    private IEnumerator AguardarEPlayVideo()
    {
        // Bloqueia a execução frame a frame até que o buffer do vídeo no hardware esteja pronto
        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }
        
        videoPlayer.Play();
    }



    private void ConfigurarDesafioAtual(Desafio desafio)
    {
        uiManager.questionTextUI.text = desafio.questionText;

        for (int i = 0; i < uiManager.answerButtons.Count; i++)
        {
            if (i < desafio.answers.Count)
            {
                uiManager.answerButtons[i].gameObject.SetActive(true);
                uiManager.ResetButtonColors();

                TextMeshProUGUI btnText = uiManager.answerButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = desafio.answers[i];
                }
            }
            else
            {
                uiManager.answerButtons[i].gameObject.SetActive(false);
            }
        }

        uiManager.SetButtonsInteractable(true);
        isProcessingAnswer = false;
    }

    public void CheckAnswer(int selectedIndex)
    {
        if (isProcessingAnswer) return;
        isProcessingAnswer = true;

        uiManager.SetButtonsInteractable(false);
        Desafio desafio = locais[localAtualIndex].desafios[desafioAtualIndex];

        if (selectedIndex == desafio.correctAnswerIndex)
        {
            StartCoroutine(HandleCorrectAnswer(selectedIndex));
        }
        else
        {
            StartCoroutine(HandleIncorrectAnswer(selectedIndex));
        }
    }

    private IEnumerator HandleCorrectAnswer(int selectedIndex)
    {
        uiManager.ApplyButtonFeedback(selectedIndex, true);
        yield return new WaitForSeconds(feedbackDelay);

        desafioAtualIndex++;

        if (desafioAtualIndex < locais[localAtualIndex].desafios.Count)
        {
            StartCoroutine(TransitionToNextQuestion());
        }
        else
        {
            int proximoMapaIndex = localAtualIndex + 1;
            if (proximoMapaIndex < locais.Count)
            {
                desafioAtualIndex = 0;
                StartCoroutine(TransitionToNextMap(proximoMapaIndex));
            }
            else
            {
                StartCoroutine(ReturnToLobby());
            }
        }
    }

    private IEnumerator HandleIncorrectAnswer(int selectedIndex)
    {
        uiManager.ApplyButtonFeedback(selectedIndex, false);
        yield return new WaitForSeconds(feedbackDelay);

        Desafio desafio = locais[localAtualIndex].desafios[desafioAtualIndex];
        ConfigurarDesafioAtual(desafio);
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        // BARREIRA DE SEGURANÇA: Valida se o índice está no escopo e se o desafio atual é realmente um vídeo
        if (locais == null || localAtualIndex >= locais.Count || desafioAtualIndex >= locais[localAtualIndex].desafios.Count) return;
        
        Desafio desafioAtual = locais[localAtualIndex].desafios[desafioAtualIndex];
        
        // Se o vídeo disparar o evento por engano em background mas o nó atual não for vídeo, aborta o avanço
        if (!desafioAtual.IsVideo)
        {
            Debug.LogWarning("[TourManager] loopPointReached disparado em background, mas o desafio atual não é um vídeo. Abortando avanço duplo.");
            return;
        }

        // Limpa o clipe imediatamente ao encerrar a reprodução bem-sucedida
        videoPlayer.Stop();
        videoPlayer.clip = null;

        desafioAtualIndex++;

        if (desafioAtualIndex < locais[localAtualIndex].desafios.Count)
        {
            StartCoroutine(TransitionToNextQuestion());
        }
        else
        {
            int proximoMapaIndex = localAtualIndex + 1;
            if (proximoMapaIndex < locais.Count)
            {
                desafioAtualIndex = 0;
                StartCoroutine(TransitionToNextMap(proximoMapaIndex));
            }
            else
            {
                StartCoroutine(ReturnToLobby());
            }
        }
    }

    private IEnumerator TransitionToNextQuestion()
    {
        yield return StartCoroutine(uiManager.FadeOut(questionFadeDuration));
        InicializarDesafioAtual();
        yield return StartCoroutine(uiManager.FadeIn(questionFadeDuration));
    }

    private IEnumerator TransitionToNextMap(int nextMapIndex)
    {
        yield return StartCoroutine(uiManager.FadeOut(mapTransitionFadeDuration));

        string nomeProximo = locais[nextMapIndex].locationName;
        uiManager.ShowTransitionText(nomeProximo);

        yield return new WaitForSeconds(waitOnBlackScreenDelay);

        uiManager.HideTransitionText(); 
        
        localAtualIndex = nextMapIndex;
        CarregarDadosDoLocal(localAtualIndex);

        yield return StartCoroutine(uiManager.FadeIn(mapTransitionFadeDuration));
    }
    
    public void RequestExitToLobby()
    {
        if (isProcessingAnswer) return;
        StartCoroutine(ReturnToLobby());
    }

    private IEnumerator ReturnToLobby()
    {
        uiManager.SetButtonsInteractable(false);
        yield return StartCoroutine(uiManager.FadeOut(mapTransitionFadeDuration));
        
        audioSource.Stop();
        uiManager.ShowTransitionText("Retornando ao Menu Principal...\nPor favor, aguarde.");

        yield return new WaitForSeconds(0.1f);

        if (dataManager != null)
        {
            dataManager.LimparAssetsCarregados();
        }

        if (!string.IsNullOrEmpty(lobbySceneName))
        {
            SceneManager.LoadScene(lobbySceneName);
        }
        else
        {
            Debug.LogError("[TourManager] Nome da cena do Lobby inválido!");
        }
    }
}