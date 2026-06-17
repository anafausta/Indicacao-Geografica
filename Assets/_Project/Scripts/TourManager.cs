using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Video;
using OdisseiaVR.Core;
using OdisseiaVR.Tour;

namespace OdisseiaVR.Tour
{
    /// <summary>
    /// RESPONSABILIDADE: Orquestrar o fluxo do tour.
    /// Gerencia o estado (local/desafio atual), a lógica do quiz (CheckAnswer),
    /// o áudio e as transições de cena.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(TourDataManager))]
    [RequireComponent(typeof(TourUIManager))]
    public class InteractiveTourManager : MonoBehaviour
{
    [Header("Componentes (Arraste ou Auto-detecta)")]
    [SerializeField] private TourDataManager dataManager;
    [SerializeField] private TourUIManager uiManager;
    [Tooltip("VideoPlayer usado para reprodução de vídeos nos desafios.")]
    [SerializeField] private VideoPlayer videoPlayer;
    [Tooltip("Forçar usar RenderTexture em vez de MaterialOverride (útil para URP/shaders).")]
    [SerializeField] private bool forceRenderTexture = true;
    
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

    [Tooltip("Tempo extra na tela preta entre mapas (para ler o texto 'Próxima Parada').")]
    public float waitOnBlackScreenDelay = 3.0f;

    [Header("Áudio")]
    [Tooltip("Som ao completar um local.")]
    public AudioClip locationVictorySound;
    [Range(0f, 1f)] public float backgroundMusicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1.0f;
    public AudioClip correctAnswerSound;
    public AudioClip incorrectAnswerSound;
    
    // --- Variáveis Privadas ---
    private List<DadosLocal> locais = new List<DadosLocal>();
    private int currentLocalIndex = 0;
    private int currentDesafioIndex = 0;
    private bool isAnswering = false; // Trava de cliques
    private AudioSource audioSource;
    private AudioSource videoAudioSource = null;
    private bool pausedBackgroundAudio = false;
    private Desafio desafioAtual; 
    private bool isPlayingVideo = false;
    private RenderTexture videoRenderTexture = null;
    private bool lastPrimaryButtonState = false;
    private bool lastSecondaryButtonState = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Auto-detecta componentes se esquecer de arrastar
        if (dataManager == null) dataManager = GetComponent<TourDataManager>();
        if (uiManager == null) uiManager = GetComponent<TourUIManager>();

        // Valida componentes obrigatórios
        if (dataManager == null)
        {
            Debug.LogError("TourDataManager não encontrado. Desabilitando TourManager.");
            enabled = false;
            return;
        }
        if (uiManager == null)
        {
            Debug.LogError("TourUIManager não encontrado. Desabilitando TourManager.");
            enabled = false;
            return;
        }

        // Inscreve nos eventos da UI
        uiManager.OnAnswerButtonClicked += CheckAnswer;
        uiManager.OnMenuButtonClicked += HandleMenuButtonClick;

        DisablePlayerMovement();
        
        // Verifica se veio do Lobby via Singleton
        GameSettings settings = GameSettings.Instance;
        if (settings != null)
        {
            currentLocalIndex = settings.selectedLocationIndex;
            Destroy(settings.gameObject);
        }
        else
        {
            currentLocalIndex = 0;
        }

        StartCoroutine(InitializeTour());
    }

    void DisablePlayerMovement()
    {
        var moveProvider = FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.ContinuousMoveProviderBase>();
        if (moveProvider != null) moveProvider.enabled = false;

        var teleportProvider = FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider>();
        if (teleportProvider != null) teleportProvider.enabled = false;
    }

    // --- INICIALIZAÇÃO ---

    private IEnumerator InitializeTour()
    {
        // 1. Espera carregar o JSON e Assets
        if (uiManager != null)
            uiManager.ShowStatusMessage("Carregando tour...");

        yield return StartCoroutine(dataManager.LoadTourDataFromJSONAsync());
        locais = dataManager.Locais;
        
        // 2. Inicia o primeiro tour
        if (locais.Count > 0)
        {
            // Carrega os dados sem fade out (já estamos carregando)
            CarregarDadosDoLocal(currentLocalIndex);
            
            if (uiManager != null)
                uiManager.HideStatusMessage();
            
            // Faz apenas o Fade In inicial (lento para ser suave)
            yield return StartCoroutine(uiManager.FadeIn(mapTransitionFadeDuration));
        }
        else
        {
            Debug.LogError("Nenhum local carregado. Verifique o JSON.");
        }
    }

    // --- LÓGICA PRINCIPAL ---

    void CarregarDadosDoLocal(int localIndex)
    {
        if(locais.Count == 0 || localIndex >= locais.Count) return;
        
        currentLocalIndex = localIndex;
        currentDesafioIndex = 0; 

        // Gerencia Música
        if (locais[currentLocalIndex].backgroundMusic != null)
        {
            audioSource.clip = locais[currentLocalIndex].backgroundMusic;
            audioSource.volume = backgroundMusicVolume;
            audioSource.loop = true;
            audioSource.Play();
        } else {
            audioSource.Stop();
        }
        
        ApresentarDesafioAtual();
    }

    void ApresentarDesafioAtual()
    {
        // Validação de segurança
        if (locais == null || locais.Count == 0) return;
        if (currentLocalIndex >= locais.Count) return;
        if (locais[currentLocalIndex].desafios == null || currentDesafioIndex >= locais[currentLocalIndex].desafios.Count) return;

        desafioAtual = locais[currentLocalIndex].desafios[currentDesafioIndex];

        // Embaralha as respostas se for quiz (não vídeo) e houver mais de uma opção
        if (desafioAtual != null && desafioAtual.videoClip == null && desafioAtual.answers != null && desafioAtual.answers.Count > 1)
        {
            ShuffleAnswers(desafioAtual);
        }

        // Atualiza visual (Esfera 360)
        if (panoramaSphereRenderer != null && desafioAtual != null)
        {
            panoramaSphereRenderer.transform.rotation = Quaternion.Euler(0, desafioAtual.initialYRotation, 0);
            if (desafioAtual.panoramaMaterial != null)
                panoramaSphereRenderer.material = desafioAtual.panoramaMaterial;
        }
        else if (panoramaSphereRenderer == null)
        {
            Debug.LogWarning("panoramaSphereRenderer não está atribuído no TourManager.");
        }

        // Atualiza UI (Texto e Botões) ou reproduz vídeo
        if (desafioAtual != null && desafioAtual.videoClip != null)
        {
            // É vídeo: esconde painel de perguntas e reproduz
            if (uiManager != null)
                uiManager.ShowQuizPanel(false);
            PlayVideo(desafioAtual.videoClip);
        }
        else
        {
            // Normal (quiz): mostra painel e apresenta perguntas
            // Retoma a música de fundo apenas se estava tocando antes do vídeo (com fade)
            if (audioSource != null && pausedBackgroundAudio)
            {
                audioSource.volume = 0f;
                audioSource.UnPause();
                StartCoroutine(FadeInBackgroundMusic(0.18f));
                pausedBackgroundAudio = false;
            }
            if (uiManager != null)
            {
                uiManager.ShowQuizPanel(true);
                uiManager.ApresentarDesafio(desafioAtual);
            }
        }
        
        // Destrava interações
        isAnswering = false;
    }

    // --- EVENTOS DA UI ---

    public void CheckAnswer(int selectedIndex)
    {
        if (isAnswering) return;
        if (desafioAtual == null)
        {
            Debug.LogWarning("CheckAnswer chamado sem desafio atual.");
            isAnswering = false;
            return;
        }
        isAnswering = true;
        if (uiManager != null)
            uiManager.SetAllButtonsInteractable(false, desafioAtual.answers != null ? desafioAtual.answers.Count : 0);

        if (selectedIndex == desafioAtual.correctAnswerIndex)
        {
            StartCoroutine(HandleCorrectAnswer(selectedIndex));
        }
        else
        {
            StartCoroutine(HandleIncorrectAnswer(selectedIndex));
        }
    }

    public void HandleMenuButtonClick()
    {
        if (isAnswering) return;
        isAnswering = true;

        int answerCount = 0;
        if (desafioAtual != null && desafioAtual.answers != null)
            answerCount = desafioAtual.answers.Count;
        else if (uiManager != null && uiManager.answerButtons != null)
            answerCount = uiManager.answerButtons.Count;

        if (uiManager != null)
            uiManager.SetAllButtonsInteractable(false, answerCount);

        StartCoroutine(ReturnToLobby());
    }
    
    // --- CORROTINAS DE FLUXO DO JOGO ---

    private IEnumerator HandleCorrectAnswer(int correctButtonIndex)
    {
        // 1. Feedback Positivo Visual e Sonoro
        if (uiManager != null) uiManager.SetButtonFeedback(correctButtonIndex, uiManager.correctColor);
        if (correctAnswerSound != null && audioSource != null) audioSource.PlayOneShot(correctAnswerSound, sfxVolume); 
        
        yield return new WaitForSeconds(feedbackDelay);

        // 2. Avança o índice
        currentDesafioIndex++;
        
        // 3. Decide o próximo passo
        if (currentDesafioIndex >= locais[currentLocalIndex].desafios.Count)
        {
            // ACABOU O LOCAL ATUAL -> Toca som de vitória
            audioSource.Stop(); 
            if (locationVictorySound != null) 
                audioSource.PlayOneShot(locationVictorySound, sfxVolume);

            int proximoLocalIndex = currentLocalIndex + 1;
            
            if (proximoLocalIndex < locais.Count)
            {
                // Ainda tem mapa: Transição Lenta com Texto
                yield return StartCoroutine(TransitionToNextMap(proximoLocalIndex));
            }
            else
            {
                // Acabou tudo: Volta pro Lobby
                yield return StartCoroutine(ReturnToLobby());
            }
        }
        else
        {
            // CONTINUA NO MESMO LOCAL -> Transição Rápida
            yield return StartCoroutine(TransitionToNextQuestion());
        }
    }

    private IEnumerator HandleIncorrectAnswer(int incorrectButtonIndex)
    {
        if (uiManager != null)
            uiManager.SetButtonFeedback(incorrectButtonIndex, uiManager.incorrectColor);
        if (incorrectAnswerSound != null && audioSource != null) audioSource.PlayOneShot(incorrectAnswerSound, sfxVolume);
        
        yield return new WaitForSeconds(feedbackDelay);
        
        // Retry com fade rápido
        if (uiManager != null)
        {
            yield return StartCoroutine(uiManager.FadeOut(questionFadeDuration));
            uiManager.ResetButtonsToNormal(desafioAtual != null && desafioAtual.answers != null ? desafioAtual.answers.Count : uiManager.answerButtons.Count);
            yield return StartCoroutine(uiManager.FadeIn(questionFadeDuration));
        }

        isAnswering = false;
    }

    // --- CORROTINAS DE TRANSIÇÃO ESPECÍFICAS ---

    /// <summary>
    /// Transição RÁPIDA: Apenas escurece, troca o material/pergunta e clareia.
    /// </summary>
    private IEnumerator TransitionToNextQuestion()
    {
        // Guarda estado da esfera
        bool sphereWasActive = panoramaSphereRenderer != null && panoramaSphereRenderer.enabled;

        // Fade Out Rápido (escurece primeiro)
        yield return StartCoroutine(uiManager.FadeOut(questionFadeDuration));

        // Desabilita a esfera temporariamente para evitar vazamento (já está preto)
        if (panoramaSphereRenderer != null)
            panoramaSphereRenderer.enabled = false;

        // Troca conteúdo
        ApresentarDesafioAtual();

        // Reativa a esfera
        if (sphereWasActive && panoramaSphereRenderer != null)
            panoramaSphereRenderer.enabled = true;

        // Fade In Rápido
        yield return StartCoroutine(uiManager.FadeIn(questionFadeDuration));
    }

    /// <summary>
    /// Transição LENTA: Fade Out -> Mostra Texto -> Espera -> Carrega -> Esconde Texto -> Fade In.
    /// </summary>
    private IEnumerator TransitionToNextMap(int nextMapIndex)
    {
        // Guarda estado da esfera
        bool sphereWasActive = panoramaSphereRenderer != null && panoramaSphereRenderer.enabled;

        // 1. Fade Out Lento (Escurece a tela PRIMEIRO)
        yield return StartCoroutine(uiManager.FadeOut(mapTransitionFadeDuration));

        // Desabilita a esfera temporariamente para evitar vazamento (já está preto)
        if (panoramaSphereRenderer != null)
            panoramaSphereRenderer.enabled = false;

        // 2. Agora que está tudo preto, mostramos o texto explicativo
        string nomeProximo = locais[nextMapIndex].locationName;
        uiManager.ShowTransitionText(nomeProximo);

        // 3. Espera na tela preta (lendo a mensagem)
        yield return new WaitForSeconds(waitOnBlackScreenDelay);

        // 4. Esconde o texto antes de começar a clarear
        uiManager.HideTransitionText();
        
        // 5. Carrega os dados (Textura, Música, etc)
        CarregarDadosDoLocal(nextMapIndex);

        // 6. Reativa a esfera antes de fazer fade in
        if (sphereWasActive && panoramaSphereRenderer != null)
            panoramaSphereRenderer.enabled = true;

        // 7. Fade In Lento (Clareia a tela revelando o novo local)
        yield return StartCoroutine(uiManager.FadeIn(mapTransitionFadeDuration));
    }
    
    private IEnumerator ReturnToLobby()
    {
        if (uiManager != null)
            uiManager.ShowStatusMessage("Retornando ao lobby...");

        yield return StartCoroutine(uiManager.FadeOut(mapTransitionFadeDuration));
        audioSource.Stop();
        
        if (uiManager != null)
            uiManager.HideStatusMessage();
        
        // Aguarda o fade local completar se existir
        if (uiManager.HasFadeScreen)
            yield return new WaitForSeconds(1.0f);

        // Garante um overlay persistente e faz fade longo para preto antes de ativar a mensagem
        PersistentFade.EnsureExists();
        yield return StartCoroutine(PersistentFade.Instance.FadeToOpaque(mapTransitionFadeDuration));
        // Mostra mensagem de transição sobre a tela preta
        PersistentFade.Instance.SetMessage("Retornando ao lobby...");
        PersistentFade.Instance.SetFadeOutOnNextSceneLoad(0.25f);

        if (!string.IsNullOrEmpty(lobbySceneName))
            SceneManager.LoadScene(lobbySceneName);
    }

    private void PlayVideo(VideoClip clip)
    {

        // Reencaminha para uma corrotina que prepara e liga o VideoPlayer ao material/RenderTexture.
        StartCoroutine(PlayVideoCoroutine(clip));
    }

    private IEnumerator PlayVideoCoroutine(VideoClip clip)
    {
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer não atribuído e não foi encontrado no GameObject.");
            yield break;
        }

        // Pausa música de fundo (se estava tocando) e prepara áudio do vídeo em AudioSource separado
        pausedBackgroundAudio = (audioSource != null && audioSource.isPlaying);
        if (pausedBackgroundAudio)
        {
            audioSource.Pause();
        }

        // Configura um AudioSource dedicado para o vídeo para evitar conflitos com a música de fundo
        if (videoAudioSource == null)
        {
            videoAudioSource = gameObject.AddComponent<AudioSource>();
            videoAudioSource.playOnAwake = false;
            videoAudioSource.loop = false;
            videoAudioSource.spatialBlend = 0f; // 2D audio
        }

        // Direciona o áudio do VideoPlayer para o AudioSource do vídeo
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, videoAudioSource);

        // Configura e para qualquer reprodução anterior
        videoPlayer.Stop();
        videoPlayer.clip = clip;
        videoPlayer.isLooping = false;

        // Tenta renderizar direto no material da esfera (MaterialOverride)
        string propertyName = null;
        if (panoramaSphereRenderer != null)
        {
            var mat = panoramaSphereRenderer.material;
            if (mat != null)
            {
                if (mat.HasProperty("_MainTex")) propertyName = "_MainTex";
                else if (mat.HasProperty("_BaseMap")) propertyName = "_BaseMap";
            }
        }

        if (!forceRenderTexture && panoramaSphereRenderer != null && propertyName != null)
        {
            videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
            videoPlayer.targetMaterialRenderer = panoramaSphereRenderer;
            videoPlayer.targetMaterialProperty = propertyName;
            videoPlayer.targetTexture = null;
            Debug.Log($"VideoPlayer: usando MaterialOverride -> propriedade '{propertyName}'");
        }
        else
        {
            // Força usar RenderTexture (fallback atual) — preferível em URP/shaders não compatíveis com MaterialOverride
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            int w = clip != null ? (int)clip.width : 1920;  // Cast explícito
            int h = clip != null ? (int)clip.height : 1080;
            if (w <= 0) w = 1920;  // Validação de segurança
            if (h <= 0) h = 1080;
            if (videoRenderTexture == null)
            {
                videoRenderTexture = new RenderTexture(w, h, 0);
                videoRenderTexture.Create();
            }
            videoPlayer.targetTexture = videoRenderTexture;
            if (panoramaSphereRenderer != null)
            {
                if (panoramaSphereRenderer.material.HasProperty("_MainTex"))
                    panoramaSphereRenderer.material.SetTexture("_MainTex", videoRenderTexture);
                else if (panoramaSphereRenderer.material.HasProperty("_BaseMap"))
                    panoramaSphereRenderer.material.SetTexture("_BaseMap", videoRenderTexture);
            }
            Debug.Log("VideoPlayer: usando RenderTexture (forçado) como fallback");
        }

        // Prepara e espera (timeout curto)
        videoPlayer.Prepare();
        float timer = 0f;
        float timeout = 5f;
        while (!videoPlayer.isPrepared && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        if (!videoPlayer.isPrepared)
        {
            Debug.LogWarning("VideoPlayer não preparado no tempo limite; tentando reproduzir mesmo assim.");
        }

        // Inicia reprodução e subscreve evento
        if (uiManager != null && uiManager.menuButton != null)
            uiManager.menuButton.gameObject.SetActive(false);
        isPlayingVideo = true;
        isAnswering = true;
        videoPlayer.Play();
        videoPlayer.loopPointReached += OnVideoEnded;
    }

    private void StopVideoPlaybackAndAdvance()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoEnded;
            if (videoPlayer.isPlaying) videoPlayer.Stop();

            // Detach audio output from VideoPlayer to avoid audio leakage
            try { videoPlayer.SetTargetAudioSource(0, null); } catch { }
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

            // Cleanup de RenderTexture/MaterialOverride se usado
            if (videoPlayer.targetTexture != null)
            {
                // Remove a textura aplicada ao material da esfera (se aplicável)
                if (panoramaSphereRenderer != null && panoramaSphereRenderer.material != null)
                {
                    if (panoramaSphereRenderer.material.HasProperty("_MainTex"))
                        panoramaSphereRenderer.material.SetTexture("_MainTex", null);
                    if (panoramaSphereRenderer.material.HasProperty("_BaseMap"))
                        panoramaSphereRenderer.material.SetTexture("_BaseMap", null);
                }

                videoPlayer.targetTexture.Release();
                videoPlayer.targetTexture = null;

                if (videoRenderTexture != null)
                {
                    Destroy(videoRenderTexture);
                    videoRenderTexture = null;
                }
            }

            if (videoPlayer.targetMaterialRenderer != null)
            {
                videoPlayer.targetMaterialRenderer = null;
                videoPlayer.targetMaterialProperty = null;
            }
        }
        
        // Stop and destroy dedicated video audio source if used
        if (videoAudioSource != null)
        {
            if (videoAudioSource.isPlaying) videoAudioSource.Stop();
            Destroy(videoAudioSource);
            videoAudioSource = null;
        }

        
        isPlayingVideo = false;
        isAnswering = false;
        // Avança para o próximo desafio (mesma lógica de HandleCorrectAnswer sem feedback)
        currentDesafioIndex++;
        if (currentDesafioIndex >= locais[currentLocalIndex].desafios.Count)
        {
            audioSource.Stop();
            if (locationVictorySound != null)
                audioSource.PlayOneShot(locationVictorySound, sfxVolume);

            int proximoLocalIndex = currentLocalIndex + 1;
            if (proximoLocalIndex < locais.Count)
            {
                StartCoroutine(TransitionToNextMap(proximoLocalIndex));
            }
            else
            {
                StartCoroutine(ReturnToLobby());
            }
        }
        else
        {
            StartCoroutine(TransitionToNextQuestion());
        }
    }

    private void OnVideoEnded(VideoPlayer vp)
    {
        StopVideoPlaybackAndAdvance();
    }

    private IEnumerator FadeInBackgroundMusic(float duration = 0.18f)
    {
        if (audioSource == null) yield break;
        float target = backgroundMusicVolume;
        float t = 0f;
        audioSource.volume = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, target, t / duration);
            yield return null;
        }
        audioSource.volume = target;
    }

    private bool CheckPrimaryButtonDown()
    {
        // Oculus Integration (se presente)
        #if OCULUS_INTEGRATION
        if (OVRInput.GetDown(OVRInput.Button.One))
            return true;
        #endif

        // XR InputDevices (CommonUsages.primaryButton)
        InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (!device.isValid)
            device = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        if (device.isValid)
        {
            bool currentPressed = false;
            if (device.TryGetFeatureValue(CommonUsages.primaryButton, out currentPressed))
            {
                if (currentPressed && !lastPrimaryButtonState)
                {
                    lastPrimaryButtonState = currentPressed;
                    return true;
                }
                lastPrimaryButtonState = currentPressed;
            }
        }

        return false;
    }

    private bool CheckSecondaryButtonDown()
    {
        // Oculus Integration (se presente)
        #if OCULUS_INTEGRATION
        if (OVRInput.GetDown(OVRInput.Button.Two))
            return true;
        #endif

        // XR InputDevices (CommonUsages.secondaryButton)
        InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (!device.isValid)
            device = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        if (device.isValid)
        {
            bool currentPressed = false;
            if (device.TryGetFeatureValue(CommonUsages.secondaryButton, out currentPressed))
            {
                if (currentPressed && !lastSecondaryButtonState)
                {
                    lastSecondaryButtonState = currentPressed;
                    return true;
                }
                lastSecondaryButtonState = currentPressed;
            }
        }

        return false;
    }

    void Update()
    {
        // Primary button (A) behavior during video playback
        if (isPlayingVideo)
        {
            if (CheckPrimaryButtonDown())
            {
                StopVideoPlaybackAndAdvance();
            }
            else if (CheckSecondaryButtonDown())
            {
                // Allow immediate return to lobby during video playback
                StartCoroutine(ReturnToLobby());
            }
        }
        else
        {
            // Secondary button (B) should return to lobby during images/panorama when not answering
            if (CheckSecondaryButtonDown())
            {
                if (!isAnswering)
                {
                    HandleMenuButtonClick();
                }
            }
        }
    }

    void OnDestroy()
    {
        if (uiManager != null)
        {
            uiManager.OnAnswerButtonClicked -= CheckAnswer;
            uiManager.OnMenuButtonClicked -= HandleMenuButtonClick;
        }
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoEnded;
    }

    private void ShuffleAnswers(Desafio desafio)
    {
        if (desafio == null || desafio.answers == null || desafio.answers.Count <= 1)
            return;

        // Cria lista de índices
        List<int> indices = new List<int>();
        for (int i = 0; i < desafio.answers.Count; i++)
            indices.Add(i);

        // Embaralha os índices usando Fisher-Yates shuffle
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = indices[i];
            indices[i] = indices[randomIndex];
            indices[randomIndex] = temp;
        }

        // Cria nova lista de respostas embaralhadas
        List<string> shuffledAnswers = new List<string>();
        int newCorrectIndex = -1;

        for (int i = 0; i < indices.Count; i++)
        {
            shuffledAnswers.Add(desafio.answers[indices[i]]);
            if (indices[i] == desafio.correctAnswerIndex)
                newCorrectIndex = i;
        }

        // Atualiza o desafio com respostas embaralhadas
        desafio.answers = shuffledAnswers;
        desafio.correctAnswerIndex = newCorrectIndex;
    }
}
}