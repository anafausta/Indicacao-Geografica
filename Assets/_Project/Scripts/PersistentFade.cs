using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

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
        private Image progressBarBackground;
        private Image progressBarFill;
        private float pendingFadeOutDuration = -1f;

        // Guarda os Canvas que foram desativados durante a transição para restaurá-los depois.
        private readonly List<Canvas> hiddenCanvases = new List<Canvas>();

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
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 99999;
            RectTransform canvasRt = canvas.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(1920, 1080);

            // CanvasScaler para escalar corretamente em telas diferentes
            CanvasScaler cs = gameObject.GetComponent<CanvasScaler>();
            if (cs == null) cs = gameObject.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

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
            // Fundo da barra de progresso
            GameObject barBgGO = new GameObject("ProgressBarBackground");
            barBgGO.transform.SetParent(this.transform, false);
            progressBarBackground = barBgGO.AddComponent<Image>();
            progressBarBackground.color = new Color(1f, 1f, 1f, 0.15f);
            progressBarBackground.raycastTarget = false;

            RectTransform barBgRt = barBgGO.GetComponent<RectTransform>();
            barBgRt.anchorMin = new Vector2(0.5f, 0.5f);
            barBgRt.anchorMax = new Vector2(0.5f, 0.5f);
            barBgRt.sizeDelta = new Vector2(800f, 40f);
            barBgRt.anchoredPosition = new Vector2(0f, -260f); // abaixo da mensagem

            // Preenchimento da barra
            GameObject barFillGO = new GameObject("ProgressBarFill");
            barFillGO.transform.SetParent(barBgGO.transform, false);
            progressBarFill = barFillGO.AddComponent<Image>();
            progressBarFill.color = Color.white;
            progressBarFill.raycastTarget = false;
            progressBarFill.type = Image.Type.Filled;
            progressBarFill.fillMethod = Image.FillMethod.Horizontal;
            progressBarFill.fillAmount = 0f;

            RectTransform barFillRt = barFillGO.GetComponent<RectTransform>();
            barFillRt.anchorMin = Vector2.zero;
            barFillRt.anchorMax = Vector2.one;
            barFillRt.offsetMin = Vector2.zero;
            barFillRt.offsetMax = Vector2.zero;

            barBgGO.SetActive(false); // começa escondida; ShowProgressBar(true) ativa quando precisar
        }

        /// <summary>
        /// Esconde todos os outros Canvas ativos da cena (exceto o próprio Canvas do fade).
        /// </summary>
        private void HideAllSceneUI()
        {
            hiddenCanvases.Clear();

            Canvas[] todosOsCanvas = Object.FindObjectsOfType<Canvas>();
            foreach (Canvas c in todosOsCanvas)
            {
                if (c == null || c == canvas) continue;
                if (c.enabled)
                {
                    c.enabled = false;
                    hiddenCanvases.Add(c);
                }
            }
        }

        /// <summary>
        /// Restaura a visibilidade de todos os Canvas escondidos por HideAllSceneUI().
        /// </summary>
        private void RestoreAllSceneUI()
        {
            foreach (Canvas c in hiddenCanvases)
            {
                if (c != null) c.enabled = true;
            }
            hiddenCanvases.Clear();
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

            HideAllSceneUI();
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

        public void ShowProgressBar(bool show)
        {
            if (progressBarBackground == null) return;
            progressBarBackground.gameObject.SetActive(show);
        }

        public void SetProgress(float value01)
        {
            if (progressBarFill == null) return;
            progressBarFill.fillAmount = Mathf.Clamp01(value01);
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
            RestoreAllSceneUI();
            AttachToActiveCamera();
            ClearMessage();

            if (pendingFadeOutDuration >= 0f)
            {
                StartCoroutine(FadeToTransparentAndDisableCoroutine(pendingFadeOutDuration));
                pendingFadeOutDuration = -1f;
            }
        }

        private void AttachToActiveCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[PersistentFade] Nenhuma câmera com tag MainCamera encontrada na cena.");
                return;
            }

            canvas.worldCamera = cam;
            transform.SetParent(cam.transform, false);
            transform.localPosition = new Vector3(0f, 0f, 0.4f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * 0.0008f; // ajuste testando no Quest
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

            RestoreAllSceneUI();
            // Garante que a mensagem esteja escondida antes de desativar
            ClearMessage();
            ShowProgressBar(false);
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

            HideAllSceneUI();
        }
    }
}