using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class LobbyDadosLocal
{
    public string locationName;
    public Texture mapTexture; 
}

/// <summary>
/// RESPONSABILIDADE: Gerenciar a cena do Lobby (Menu Principal) de forma reativa.
/// Otimizado para o Meta Quest 2 para evitar picos de I/O ao alternar mapas
/// e fornecer feedback visual contínuo durante a mudança assíncrona de cena.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class LobbyManager : MonoBehaviour
{
    [Header("Arquivo de Conteúdo")]
    [Tooltip("Arraste aqui o arquivo JSON que contém os dados dos tours.")]
    public TextAsset tourDataJson;

    [Header("Referências da UI (Canvas XR)")]
    public TextMeshProUGUI locationNameText;
    public RawImage mapDisplayImage;
    public Button nextButton;
    public Button previousButton;
    public Button startTourButton;

    [Header("Feedback de Carregamento VR")]
    [Tooltip("Texto que avisa o utilizador que a cena está a ser carregada em background.")]
    public TextMeshProUGUI loadingTextUI;
    [Tooltip("Painel ou imagem de fade para escurecer o menu durante o loading.")]
    public GameObject loadingPanel;
    [Tooltip("Barra de progresso visual (Slider UI) para o carregamento assíncrono.")]
    public UnityEngine.UI.Slider progressBar;

    [Header("Configurações de Cena")]
    public string tourSceneName = "TourScene";

    // --- ESTADO INTERNO ---
    private List<LobbyDadosLocal> locaisNoLobby = new List<LobbyDadosLocal>();
    private int currentLocationIndex = 0;
    private bool isLoadingScene = false;

    private void Awake()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (loadingTextUI != null) loadingTextUI.gameObject.SetActive(false);
        
        ConfigurarBotoesIniciais();
    }

    private void Start()
    {
        if (tourDataJson != null)
        {
            StartCoroutine(ParseJsonECarregarPrimeiroAsync());
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] tourDataJson não foi atribuído no Lobby!");
        }
    }

    private void ConfigurarBotoesIniciais()
    {
        if (nextButton != null) nextButton.onClick.AddListener(AvancarLocal);
        if (previousButton != null) previousButton.onClick.AddListener(RetrocederLocal);
        if (startTourButton != null) startTourButton.onClick.AddListener(IniciarTourSelecionado);
    }

    /// <summary>
    /// Lê a estrutura do JSON e faz o pré-carregamento assíncrono do primeiro elemento
    /// para evitar travamentos de frame na inicialização do menu VR.
    /// </summary>
    private IEnumerator ParseJsonECarregarPrimeiroAsync()
    {
        SetInteratividadeInterface(false);

        TourDataJson dadosJson = JsonUtility.FromJson<TourDataJson>(tourDataJson.text);
        
        foreach (var localJson in dadosJson.locais)
        {
            LobbyDadosLocal novoLocal = new LobbyDadosLocal
            {
                locationName = localJson.locationName
            };

            // Carrega a textura miniatura do mapa de forma assíncrona
            if (!string.IsNullOrEmpty(localJson.mapImagePath))
            {
                ResourceRequest request = Resources.LoadAsync<Texture>(localJson.mapImagePath);
                yield return request;
                novoLocal.mapTexture = request.asset as Texture;
            }

            locaisNoLobby.Add(novoLocal);
        }

        SetInteratividadeInterface(true);
        AtualizarDisplayInterface();
    }

    private void AvancarLocal()
    {
        if (locaisNoLobby.Count == 0 || isLoadingScene) return;

        currentLocationIndex = (currentLocationIndex + 1) % locaisNoLobby.Count;
        AtualizarDisplayInterface();
    }

    private void RetrocederLocal()
    {
        if (locaisNoLobby.Count == 0 || isLoadingScene) return;

        currentLocationIndex--;
        if (currentLocationIndex < 0)
        {
            currentLocationIndex = locaisNoLobby.Count - 1;
        }
        AtualizarDisplayInterface();
    }

    private void AtualizarDisplayInterface()
    {
        if (currentLocationIndex < 0 || currentLocationIndex >= locaisNoLobby.Count) return;

        LobbyDadosLocal localAtual = locaisNoLobby[currentLocationIndex];

        if (locationNameText != null) locationNameText.text = localAtual.locationName;
        if (mapDisplayImage != null)  mapDisplayImage.texture = localAtual.mapTexture;
    }

    private void SetInteratividadeInterface(bool estado)
    {
        if (nextButton != null) nextButton.interactable = estado;
        if (previousButton != null) previousButton.interactable = estado;
        if (startTourButton != null) startTourButton.interactable = estado;
    }

    public void IniciarTourSelecionado()
    {
        if (isLoadingScene) return;
        isLoadingScene = true; // Impede múltiplos cliques
        SetInteratividadeInterface(false);
        
        // Agora chama a corrotina unificada que preenche a barra de progresso!
        StartCoroutine(LoadTourSceneAsync());
    }

    /// <summary>
    /// Fluxo controlado de transição: Ativa mensagens de feedback para o utilizador VR,
    /// desativa os inputs da UI para evitar cliques duplos e gerencia a memória antes da troca.
    /// </summary>
    private IEnumerator LoadTourSceneAsync()
    {
        GameSettings settings = GameSettings.Instance;
        if (settings == null)
        {
            Debug.LogWarning("[LobbyManager] GameSettings não encontrado. Criando instância de emergência.");
            GameObject settingsObj = new GameObject("_GameSettings");
            settings = settingsObj.AddComponent<GameSettings>();
        }

        settings.selectedLocationIndex = currentLocationIndex;
        
        // Ativa o painel visual de carregamento no Canvas XR antes de iniciar o peso do I/O
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (progressBar != null) progressBar.value = 0f;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(tourSceneName);
        asyncLoad.allowSceneActivation = false; 

        while (!asyncLoad.isDone)
        {
            // Mapeia o progresso da Unity (0 a 0.9) para escala real (0 a 1)
            float progressoReal = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            
            if (progressBar != null) 
            {
                progressBar.value = progressoReal;
            }

            if (loadingTextUI != null)
            {
                int percentagem = Mathf.RoundToInt(progressoReal * 100f);
                loadingTextUI.text = $"A carregar portal para:\n{locaisNoLobby[currentLocationIndex].locationName}\nProgresso: {percentagem}%";
            }

            // Quando o Quest 2 terminar de colocar a cena inteira na memória RAM
            if (asyncLoad.progress >= 0.9f)
            {
                // Pequena folga de segurança para o usuário ler o texto de feedback em VR
                yield return new WaitForSeconds(0.5f);
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
