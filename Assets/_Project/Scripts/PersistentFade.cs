using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OdisseiaVR.Core
{
    /// <summary>
    /// View do overlay de fade. Só desenha e anima: não conhece cenas, nem quem o chama.
    /// Criado pelo AppContext e persistente (DontDestroyOnLoad).
    ///
    /// MUDANÇA IMPORTANTE: o objeto NÃO é mais filho da câmera da cena. Ser reparentado para
    /// uma câmera de cena o move para dentro dessa cena e ele pode ser destruído junto com ela.
    /// Agora ele permanece na raiz persistente e segue a câmera no LateUpdate.
    /// </summary>
    public sealed class PersistentFade : MonoBehaviour, IScreenOverlay
    {
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 0f, 0.4f);
        [SerializeField] private float worldScale = 0.0008f; // ajuste testando no Quest

        private static readonly Vector2 CanvasSize = new Vector2(1920f, 1080f);

        private Canvas canvas;
        private Image fadeImage;
        private TextMeshProUGUI messageText;
        private GameObject progressBar;
        private Image progressFill;

        // Canvases de cena escondidos durante a tela preta, para restaurar depois.
        private readonly List<Canvas> hiddenCanvases = new List<Canvas>();

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            BuildUi();
            SetVisible(false);
        }

        private void LateUpdate()
        {
            if (!canvas.enabled) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            if (canvas.worldCamera != cam) canvas.worldCamera = cam;
            transform.SetPositionAndRotation(cam.transform.TransformPoint(cameraOffset), cam.transform.rotation);
        }

        // ---------------- IScreenOverlay ----------------

        public IEnumerator FadeToOpaque(float duration)
        {
            SetVisible(true);
            yield return Fade(1f, duration);
            HideAllSceneUi();
        }

        public IEnumerator FadeToClear(float duration)
        {
            RestoreAllSceneUi();

            if (fadeImage.color.a <= 0f)
            {
                SetVisible(false);
                yield break;
            }

            yield return Fade(0f, duration);
            SetVisible(false);
        }

        public void ShowMessage(string message)
        {
            messageText.text = message ?? string.Empty;
            messageText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        public void HideMessage() => ShowMessage(null);

        public void ShowProgressBar(bool show) => progressBar.SetActive(show);

        public void SetProgress(float value01) => progressFill.fillAmount = Mathf.Clamp01(value01);

        // ---------------- Internos ----------------

        private IEnumerator Fade(float targetAlpha, float duration)
        {
            float startAlpha = fadeImage.color.a;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration));
                yield return null;
            }
            SetAlpha(targetAlpha);
        }

        private void SetAlpha(float alpha)
        {
            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;
        }

        // Desabilitar só o Canvas (e não o GameObject) mantém corrotinas e LateUpdate funcionando.
        private void SetVisible(bool visible) => canvas.enabled = visible;

        private void HideAllSceneUi()
        {
            hiddenCanvases.Clear();
            foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (c == canvas || !c.enabled) continue;
                c.enabled = false;
                hiddenCanvases.Add(c);
            }
        }

        private void RestoreAllSceneUi()
        {
            foreach (Canvas c in hiddenCanvases)
            {
                if (c != null) c.enabled = true;
            }
            hiddenCanvases.Clear();
        }

        // ---------------- Construção da UI em runtime ----------------

        private void BuildUi()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 99999;
            ((RectTransform)transform).sizeDelta = CanvasSize;
            transform.localScale = Vector3.one * worldScale;

            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            RectTransform fadeRect = CreateChild("FadeImage");
            fadeImage = fadeRect.gameObject.AddComponent<Image>();
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
            Stretch(fadeRect);

            RectTransform messageRect = CreateChild("FadeMessage");
            messageText = messageRect.gameObject.AddComponent<TextMeshProUGUI>();
            messageText.color = Color.white;
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.enableWordWrapping = true;
            messageText.raycastTarget = false;
            messageText.fontSize = 48;
            messageRect.sizeDelta = new Vector2(1000f, 400f);
            messageText.gameObject.SetActive(false);

            RectTransform barRect = CreateChild("ProgressBarBackground");
            Image barBackground = barRect.gameObject.AddComponent<Image>();
            barBackground.color = new Color(1f, 1f, 1f, 0.15f);
            barBackground.raycastTarget = false;
            barRect.sizeDelta = new Vector2(800f, 40f);
            barRect.anchoredPosition = new Vector2(0f, -260f);
            progressBar = barRect.gameObject;

            var fillRect = new GameObject("ProgressBarFill", typeof(RectTransform)).GetComponent<RectTransform>();
            fillRect.SetParent(barRect, false);
            progressFill = fillRect.gameObject.AddComponent<Image>();
            progressFill.raycastTarget = false;
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillAmount = 0f;
            Stretch(fillRect);

            progressBar.SetActive(false);
        }

        private RectTransform CreateChild(string childName)
        {
            var rect = new GameObject(childName, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
