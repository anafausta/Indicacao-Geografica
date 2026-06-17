using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

namespace OdisseiaVR.Core
{
    /// <summary>
    /// Overlay de fade que persiste entre cenas para evitar flashes brancos no carregamento.
    /// Cria um Canvas + Image em runtime e se marca como DontDestroyOnLoad.
    /// </summary>
    public class PersistentFade : MonoBehaviour
    {
        public static PersistentFade Instance { get; private set; }

        private Canvas canvas;
        private Image image;
        private TextMeshProUGUI messageText;
        private GameObject messageGO;
        private float pendingFadeOutDuration = -1f;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
            CreateCanvasAndImage();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Instance = null;
            }
        }

        private void CreateCanvasAndImage()
        {
            // Canvas no GameObject raiz
            canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 99999;

            // CanvasScaler para escalar corretamente em telas diferentes
            CanvasScaler cs = gameObject.GetComponent<CanvasScaler>();
            if (cs == null) cs = gameObject.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            // Adiciona GraphicRaycaster para garantir renderização UI correta
            if (gameObject.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Image preenchendo toda a tela
            GameObject imgGO = new GameObject("FadeImage");
            imgGO.transform.SetParent(this.transform, false);
            image = imgGO.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);

            RectTransform rt = imgGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Texto da mensagem centralizado sobre a imagem
            messageGO = new GameObject("FadeMessage");
            messageGO.transform.SetParent(this.transform, false);
            messageText = messageGO.AddComponent<TextMeshProUGUI>();
            messageText.text = "";
            messageText.color = Color.white;
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.enableWordWrapping = true;
            messageText.raycastTarget = false;
            messageText.fontSize = 48;
            messageText.gameObject.SetActive(false);

            RectTransform mrt = messageGO.GetComponent<RectTransform>();
            mrt.anchorMin = new Vector2(0.5f, 0.5f);
            mrt.anchorMax = new Vector2(0.5f, 0.5f);
            mrt.sizeDelta = new Vector2(1000f, 400f);
            mrt.anchoredPosition = Vector2.zero;

            // Garante que a mensagem fique acima da imagem de fade
            messageGO.transform.SetAsLastSibling();
        }

        /// <summary>
        /// Garante que uma instância exista no projeto (cria em runtime se necessário).
        /// </summary>
        public static void EnsureExists()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("PersistentFade");
                go.AddComponent<PersistentFade>();
            }
        }

        /// <summary>
        /// Mostra imediatamente a tela preta opaca.
        /// </summary>
        public void ShowImmediateOpaque()
        {
            if (image == null) CreateCanvasAndImage();
            image.color = new Color(0f, 0f, 0f, 1f);
            gameObject.SetActive(true);
            if (canvas != null) canvas.sortingOrder = 99999;
        }

        /// <summary>
        /// Exibe uma mensagem central sobre a tela preta. Use rich text (TMP) se desejar formatação.
        /// </summary>
        public void SetMessage(string message)
        {
            if (messageText == null) CreateCanvasAndImage();
            if (messageText == null) return;
            messageText.text = message ?? string.Empty;
            messageText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        /// <summary>
        /// Limpa a mensagem exibida (esconde o objeto de texto).
        /// </summary>
        public void ClearMessage()
        {
            if (messageText == null) return;
            messageText.text = "";
            messageText.gameObject.SetActive(false);
        }

        /// <summary>
        /// Marca para dar fade out automaticamente após o próximo carregamento de cena.
        /// </summary>
        public void SetFadeOutOnNextSceneLoad(float duration)
        {
            pendingFadeOutDuration = duration;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Esconde a mensagem assim que a nova cena foi carregada
            ClearMessage();

            if (pendingFadeOutDuration >= 0f)
            {
                StartCoroutine(FadeToTransparentAndDisableCoroutine(pendingFadeOutDuration));
                pendingFadeOutDuration = -1f;
            }
        }

        /// <summary>
        /// Inicia o fade para transparente e desativa o objeto ao final. (Chamado pelo próprio objeto.)
        /// </summary>
        public void StartFadeToTransparentAndDisable(float duration)
        {
            StartCoroutine(FadeToTransparentAndDisableCoroutine(duration));
        }

        private IEnumerator FadeToTransparentAndDisableCoroutine(float duration)
        {
            if (image == null) yield break;
            float start = image.color.a;
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(timer / Mathf.Max(duration, 0.0001f));
                float a = Mathf.Lerp(start, 0f, progress);
                Color c = image.color;
                c.a = a;
                image.color = c;
                yield return null;
            }
            Color final = image.color;
            final.a = 0f;
            image.color = final;

            // Garante que a mensagem esteja escondida antes de desativar
            ClearMessage();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Faz fade da overlay de transparente para opaco ao longo de 'duration'.
        /// Use 'yield return StartCoroutine(PersistentFade.Instance.FadeToOpaque(duration));' para aguardar.
        /// </summary>
        public IEnumerator FadeToOpaque(float duration)
        {
            if (image == null) CreateCanvasAndImage();
            gameObject.SetActive(true);
            float start = image.color.a;
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(timer / Mathf.Max(duration, 0.0001f));
                float a = Mathf.Lerp(start, 1f, progress);
                Color c = image.color;
                c.a = a;
                image.color = c;
                yield return null;
            }
            Color final = image.color;
            final.a = 1f;
            image.color = final;
        }
    }
}
