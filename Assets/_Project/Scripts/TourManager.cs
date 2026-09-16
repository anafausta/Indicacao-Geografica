using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Video;
using TMPro;
using OdisseiaVR.Core;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(VideoPlayer))]
public class TourManager : MonoBehaviour
{
    [Header("Componentes (Auto-detecta no Awake)")]
    [SerializeField] private TourDataManager dataManager;
    [SerializeField] private TourUIManager uiManager;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private VideoPlayer videoPlayer;
    
    [Header("Efeitos Sonoros (SFX)")]
    [Tooltip("Som em MP3 para resposta correta")]
    public AudioClip correctSFX;
    [Tooltip("Som em MP3 para resposta incorreta")]
    public AudioClip incorrectSFX;
    [Tooltip("Som em MP3 ao concluir a fase")]
    public AudioClip victorySFX;

    [Header("Referências da Cena")]
    [Tooltip("O objeto Renderer da esfera que exibirá o panorama 360°.")]
    public Renderer panoramaSphereRenderer;
    
    [Header("Material dedicado ao vídeo (shader PanoramaUnlit_URP, textura vazia)")]
    public Material videoMaterialTemplate;
    private Material videoMaterial;

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
    private RenderTexture videoRenderTexture;
    private void Awake()
    {
        if (dataManager == null) dataManager = GetComponent<TourDataManager>();
        if (uiManager == null) uiManager = GetComponent<TourUIManager>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();

        // Cria a textura dinamicamente para o Android/Quest
        videoRenderTexture = new RenderTexture(2048, 1024, 0, RenderTextureFormat.ARGB32);
        videoRenderTexture.Create();

        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoRenderTexture;

        videoRenderTexture = new RenderTexture(2048, 1024, 0, RenderTextureFormat.ARGB32);
        videoRenderTexture.Create();

        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoRenderTexture;

        if (videoMaterialTemplate != null)
        {
            videoMaterial = new Material(videoMaterialTemplate);
            videoMaterial.mainTexture = videoRenderTexture;
        }

        PersistentFade.EnsureExists(); // garante que existe mesmo se a cena Tour for aberta direto no Editor

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
        uiManager.BindAnswerListeners(this);

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

        yield return StartCoroutine(uiManager.FadeIn(0f));
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
        if (uiManager.menuButton != null) uiManager.menuButton.gameObject.SetActive(true);
    
        if (!audioSource.isPlaying && locais[localAtualIndex].backgroundMusic != null)
        {
            audioSource.clip = locais[localAtualIndex].backgroundMusic;
            audioSource.Play();
        }

        if (desafio.panoramaMaterial != null)
        {
            panoramaSphereRenderer.sharedMaterial = desafio.panoramaMaterial;
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
        if (uiManager.menuButton != null)
        {
            uiManager.menuButton.gameObject.SetActive(false);
        }

        if (videoMaterial != null)
        {
            panoramaSphereRenderer.sharedMaterial = videoMaterial;
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

        // Toca o efeito de acerto sem parar a música
        if (correctSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(correctSFX);
        }

        yield return new WaitForSeconds(feedbackDelay);

        AvancarParaProximoDesafio();
    }

    private void AvancarParaProximoDesafio()
    {
        desafioAtualIndex++;

        if (desafioAtualIndex < locais[localAtualIndex].desafios.Count)
        {
            StartCoroutine(TransitionToNextQuestion());
            return;
        }

        if (victorySFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(victorySFX);
        }

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

    private IEnumerator HandleIncorrectAnswer(int selectedIndex)
    {
        uiManager.ApplyButtonFeedback(selectedIndex, false);

        // Toca o efeito de erro
        if (incorrectSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(incorrectSFX);
        }

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

       AvancarParaProximoDesafio();
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

        yield return StartCoroutine(PersistentFade.Instance.FadeToOpaque(mapTransitionFadeDuration));
        PersistentFade.Instance.SetMessage("Retornando ao Menu Principal...\nPor favor, aguarde.");
        PersistentFade.Instance.ShowProgressBar(true);
        PersistentFade.Instance.SetProgress(0f);

        audioSource.Stop();

        if (dataManager != null)
        {
            dataManager.LimparAssetsCarregados();
        }

        if (string.IsNullOrEmpty(lobbySceneName))
        {
            Debug.LogError("[TourManager] Nome da cena do Lobby inválido!");
            yield break;
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(lobbySceneName);
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            float progresso = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            PersistentFade.Instance.SetProgress(progresso);

            if (asyncLoad.progress >= 0.9f)
            {
                PersistentFade.Instance.SetProgress(1.0f);

                // Aguarda um tempo fixo para a mensagem "Retornando ao Menu Principal..." ser lida
                yield return new WaitForSeconds(2.0f);

                PersistentFade.Instance.SetFadeOutOnNextSceneLoad(mapTransitionFadeDuration);
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }
    // --- CONTROLE DE INPUTS VR (BOTÕES A e B) ---
    private bool lastButtonAState = false;
    private bool lastButtonBState = false;

    private void Update()
    {
        // Captura o controle da mão direita
        var rightHandDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);

        // Botão A (Primary Button) -> Pula o vídeo atual
        if (rightHandDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool isAPressed))
        {
            if (isAPressed && !lastButtonAState)
            {
                PularVideoAtual();
            }
            lastButtonAState = isAPressed;
        }

        // Botão B (Secondary Button) -> Volta para o Lobby
        if (rightHandDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool isBPressed))
        {
            if (isBPressed && !lastButtonBState)
            {
                RequestExitToLobby();
            }
            lastButtonBState = isBPressed;
        }
    }

    /// <summary>
    /// Interrompe a reprodução do vídeo atual e avança imediatamente para o próximo nó.
    /// </summary>
    public void PularVideoAtual()
    {
        if (locais == null || localAtualIndex >= locais.Count || desafioAtualIndex >= locais[localAtualIndex].desafios.Count) return;

        Desafio desafioAtual = locais[localAtualIndex].desafios[desafioAtualIndex];

        // Só executa o pulo se o desafio atual for realmente um vídeo
        if (desafioAtual.IsVideo)
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
                videoPlayer.clip = null;
            }

            AvancarParaProximoDesafio();
        }
    }
}