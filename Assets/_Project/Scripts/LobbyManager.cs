using System.Collections;
using System.Collections.Generic;
using OdisseiaVR.Core;
using OdisseiaVR.Tour.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OdisseiaVR.Lobby
{
    /// <summary>
    /// RESPONSABILIDADE: apresentar o menu e reagir aos botões.
    /// Parse de JSON ficou no repositório; transição de cena ficou no SceneLoader.
    /// Os nomes dos campos serializados foram mantidos para não perder as ligações no Inspector.
    /// </summary>
    public sealed class LobbyManager : MonoBehaviour
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

        [Header("Transição")]
        public float fadeDuration = 5f;
        public float loadHoldSeconds = 2f;

        [Header("Configurações de Cena")]
        public string tourSceneName = "TourScene";

        private ITourRepository repository;
        private IReadOnlyList<LocationSummary> locations = new List<LocationSummary>();
        private int currentIndex;
        private bool isLoadingScene;

        private void Awake()
        {
            if (tourDataJson != null)
            {
                repository = new JsonTourRepository(tourDataJson);
            }
        }

        private void OnEnable()
        {
            if (nextButton != null) nextButton.onClick.AddListener(ShowNext);
            if (previousButton != null) previousButton.onClick.AddListener(ShowPrevious);
            if (startTourButton != null) startTourButton.onClick.AddListener(StartSelectedTour);
        }

        private void OnDisable()
        {
            if (nextButton != null) nextButton.onClick.RemoveListener(ShowNext);
            if (previousButton != null) previousButton.onClick.RemoveListener(ShowPrevious);
            if (startTourButton != null) startTourButton.onClick.RemoveListener(StartSelectedTour);
        }

        private IEnumerator Start()
        {
            SetInteractable(false);

            if (repository == null)
            {
                Debug.LogError($"[{nameof(LobbyManager)}] tourDataJson não foi atribuído no Lobby!");
                yield break;
            }

            yield return repository.LoadSummariesAsync(loaded => locations = loaded);

            currentIndex = 0;
            Refresh();
            SetInteractable(locations.Count > 0);

            // Só revela a tela quando o menu já está com conteúdo.
            yield return AppContext.SceneLoader.Reveal(fadeDuration);
        }

        private void ShowNext() => Step(+1);

        private void ShowPrevious() => Step(-1);

        private void Step(int delta)
        {
            if (isLoadingScene || locations.Count == 0) return;

            currentIndex = (currentIndex + delta + locations.Count) % locations.Count;
            Refresh();
        }

        private void Refresh()
        {
            if (currentIndex < 0 || currentIndex >= locations.Count) return;

            LocationSummary location = locations[currentIndex];
            if (locationNameText != null) locationNameText.text = location.Name;
            if (mapDisplayImage != null) mapDisplayImage.texture = location.MapTexture;
        }

        public void StartSelectedTour()
        {
            if (isLoadingScene || locations.Count == 0) return;

            isLoadingScene = true;
            SetInteractable(false);
            AppContext.Session.SelectedLocationIndex = currentIndex;

            StartCoroutine(LoadTourScene());
        }

        private IEnumerator LoadTourScene()
        {
            var request = new SceneLoadRequest
            {
                SceneName = tourSceneName,
                MessageTemplate = $"A carregar portal para:\n{locations[currentIndex].Name}\nProgresso: {{progress}}%",
                FadeOutDuration = fadeDuration,
                RevealDuration = fadeDuration,
                HoldSeconds = loadHoldSeconds
            };

            yield return AppContext.SceneLoader.LoadScene(request);

            // Só chega aqui se a troca de cena falhou: devolve o controle ao usuário.
            isLoadingScene = false;
            SetInteractable(true);
        }

        private void SetInteractable(bool value)
        {
            if (nextButton != null) nextButton.interactable = value;
            if (previousButton != null) previousButton.interactable = value;
            if (startTourButton != null) startTourButton.interactable = value;
        }
    }
}
