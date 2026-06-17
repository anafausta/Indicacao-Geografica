using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using OdisseiaVR.Core;
using OdisseiaVR.Tour;

namespace OdisseiaVR.Lobby
{
    // --- ESTRUTURA DE DADOS DE RUNTIME (ESPECÍFICA DO LOBBY) ---
    public class LobbyDadosLocal
{
    public string locationName;
    public Texture mapTexture; // Usamos Texture pois é para RawImage
}

/// <summary>
    /// Gerencia a cena do Lobby (Menu Principal).
    /// Controla o carrossel de seleção de locais, toca a música de fundo
    /// e carrega a cena do Tour com o local selecionado.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class LobbyManager : MonoBehaviour
{
    [Header("Arquivo de Conteúdo")]
    [Tooltip("Arraste aqui o arquivo JSON que contém os dados dos tours.")]
    public TextAsset tourDataJson;

    [Header("Referências da UI (Arraste do Canvas)")]
    [Tooltip("O componente de texto que exibe o nome do local (ex: 'Coliseu, Itália').")]
    public TextMeshProUGUI locationNameText;
    
    // --- MUDANÇA PRINCIPAL ---
    [Tooltip("A RawImage que exibe a textura do mapa. Esta é a parte VISUAL.")]
    public RawImage mapDisplayImage; // Campo 1: A Imagem

    [Tooltip("O Botão (pode ser invisível) que detecta o clique. " +
             "Deve ter o mesmo tamanho e posição da RawImage acima.")]
    public Button mapStartButton; // Campo 2: O Botão
    // --- FIM DA MUDANÇA ---

    [Tooltip("Botão para ir para o próximo local.")]
    public Button nextButton;
    [Tooltip("Botão para ir para o local anterior.")]
    public Button previousButton;

    [Header("Configurações de Cena")]
    [Tooltip("O nome exato da sua cena principal do Tour (ex: 'TourScene').")]
    public string tourSceneName = "TourScene";


    [Header("Áudio")]
    [Tooltip("A música de fundo que tocará no Lobby.")]
    public AudioClip lobbyMusic;
    [Tooltip("Volume da música do lobby (0 a 1).")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;

    // --- Variáveis Privadas ---
    private AudioSource audioSource;
    private List<LobbyDadosLocal> locais = new List<LobbyDadosLocal>();
    private int currentLocationIndex = 0;
    
    // [NOVO] Trava para impedir cliques duplos durante o carregamento
    private bool isLoadingScene = false; 

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (lobbyMusic != null)
        {
            audioSource.clip = lobbyMusic;
            audioSource.volume = musicVolume;
            audioSource.loop = true;
            audioSource.Play();
        }

        // --- VALIDAÇÃO ATUALIZADA ---
        // Verificamos os dois novos campos separados
        if (mapDisplayImage == null || mapStartButton == null || locationNameText == null || nextButton == null || previousButton == null)
        {
            Debug.LogError("Uma ou mais referências de UI (mapDisplayImage, mapStartButton, locationNameText, etc.) " +
                           "não estão atribuídas no Inspector do LobbyManager!");
            return;
        }
        
        // --- LÓGICA DE AUTO-DETECÇÃO FOI REMOVIDA ---
        // Agora confiamos 100% nas referências do Inspector.

        LoadLobbyDataFromJSON();

        // Adiciona os listeners para os cliques
        nextButton.onClick.AddListener(NextLocation);
        previousButton.onClick.AddListener(PreviousLocation);
        mapStartButton.onClick.AddListener(StartTour); // O botão de start ouve o clique

        if (locais.Count > 0)
        {
            UpdateUI();
        }
        else
        {
            Debug.LogError("Nenhum local foi carregado do JSON. Verifique o arquivo.");
            locationNameText.text = "Erro ao carregar dados";
        }
    }

    /// <summary>
    /// Lê o JSON e popula a lista 'locais' com nomes e Texturas.
    /// (Esta função pode permanecer síncrona, pois Texturas de UI
    /// carregadas do Resources são geralmente pequenas).
    /// </summary>
    void LoadLobbyDataFromJSON()
    {
        if (tourDataJson == null)
        {
            Debug.LogError("ERRO CRÍTICO: O 'tourDataJson' não foi atribuído no Inspector!");
            return;
        }

        // Usa 'TourDataJson' (a classe global definida no TourDataManager.cs)
        OdisseiaVR.Tour.TourDataJson dataFromJson;
        try
        {
            dataFromJson = JsonUtility.FromJson<OdisseiaVR.Tour.TourDataJson>(tourDataJson.text);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Falha ao desserializar o JSON: {ex.Message}");
            return;
        }

        if (dataFromJson == null || dataFromJson.locais == null)
        {
            Debug.LogError("Falha ao desserializar o JSON. Verifique a estrutura do arquivo.");
            return;
        }
        
        // Itera pelos locais lidos do JSON
        foreach (var localJson in dataFromJson.locais)
        {
            LobbyDadosLocal novoLocal = new LobbyDadosLocal
            {
                locationName = localJson.locationName,
                // Carrega a Textura da pasta 'Resources' usando o caminho do JSON
                // (Ex: "TexturasMapas/Coliseu_Map")
                mapTexture = Resources.Load<Texture>(localJson.mapImagePath) 
            };

            // Aviso de erro se a textura não for encontrada
            if (novoLocal.mapTexture == null && !string.IsNullOrEmpty(localJson.mapImagePath))
            {
                Debug.LogWarning($"Textura do mapa não encontrada em 'Assets/Resources/{localJson.mapImagePath}' para o local '{localJson.locationName}'");
            }
            
            locais.Add(novoLocal);
        }
    }

    /// <summary>
    /// Atualiza o texto e a textura do carrossel com base no 'currentLocationIndex'.
    /// </summary>
    void UpdateUI()
    {
        if (locais.Count == 0) return;

        LobbyDadosLocal current = locais[currentLocationIndex];
        
        // Atualiza o texto
        locationNameText.text = current.locationName;

        // Atualiza a textura na RawImage (que agora é uma referência pública)
        if (mapDisplayImage != null && current.mapTexture != null)
        {
            mapDisplayImage.texture = current.mapTexture;
        }
    }

    /// <summary>
    /// Chamado pelo botão "Próxima".
    /// </summary>
    public void NextLocation()
    {
        // [MODIFICADO] Impede a troca se já estiver carregando
        if (isLoadingScene) return; 
        if (locais == null || locais.Count == 0) return; // Proteção contra divisão por zero
        
        currentLocationIndex = (currentLocationIndex + 1) % locais.Count;
        UpdateUI();
    }

    /// <summary>
    /// Chamado pelo botão "Anterior".
    /// </summary>
    public void PreviousLocation()
    {
        // [MODIFICADO] Impede a troca se já estiver carregando
        if (isLoadingScene) return; 
        if (locais == null || locais.Count == 0) return; // Proteção caso não haja locais

        currentLocationIndex--;
        if (currentLocationIndex < 0)
        {
            currentLocationIndex = locais.Count - 1;
        }
        UpdateUI();
    }

    /// <summary>
    /// Chamado pelo 'mapStartButton'.
    /// [MODIFICADO] Agora inicia uma Corrotina.
    /// </summary>
    public void StartTour()
    {
        // [MODIFICADO] Trava para impedir cliques duplos
        if (isLoadingScene) return;
        isLoadingScene = true;

        // Desativa os botões para o usuário não clicar novamente
        mapStartButton.interactable = false;
        nextButton.interactable = false;
        previousButton.interactable = false;
        
        // Inicia a rotina de carregamento assíncrono
        StartCoroutine(LoadTourSceneAsync());
    }

    /// <summary>
    /// [NOVA CORROTINA]
    /// Carrega a cena do Tour de forma assíncrona para não travar a thread principal.
    /// </summary>
    private IEnumerator LoadTourSceneAsync()
    {
        // Usa o GameSettings (Singleton) para passar o índice para a próxima cena
        GameSettings settings = GameSettings.Instance;
        if (settings == null)
        {
            Debug.LogWarning("GameSettings.Instance não encontrado. Criando um novo objeto GameSettings.");
            GameObject settingsObj = new GameObject("_GameSettings");
            settings = settingsObj.AddComponent<GameSettings>();
        }

        settings.selectedLocationIndex = currentLocationIndex;

        // Garante um overlay persistente preto durante o carregamento para evitar flashes brancos
        PersistentFade.EnsureExists();
        PersistentFade.Instance.ShowImmediateOpaque();

        // Mostra mensagem de transição sobre a tela preta (aparecerá durante o carregamento)
        string nextName = null;
        if (locais != null && currentLocationIndex >= 0 && currentLocationIndex < locais.Count)
            nextName = locais[currentLocationIndex].locationName;
        string message = $"Você agora está sendo transportado para:\n\n<size=140%><color=#FFD700>{nextName}</color></size>";
        PersistentFade.Instance.SetMessage(message);

        // Faz com que o PersistentFade dê fade-out automaticamente assim que a nova cena for carregada
        PersistentFade.Instance.SetFadeOutOnNextSceneLoad(0.25f);

        // 1. Inicia o carregamento assíncrono
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(tourSceneName);

        // 2. Espera até que o carregamento da cena esteja completo
        //    (A tela ficará preta durante essa transição, o que é normal)
        while (!asyncLoad.isDone)
        {
            // (Opcional: você poderia exibir o asyncLoad.progress em uma UI de loading)
            yield return null; // Espera pelo próximo frame
        }
        
        // O 'isLoadingScene' não precisa ser resetado para 'false',
        // pois este objeto será destruído (ou a cena será descarregada).
    }
}
}