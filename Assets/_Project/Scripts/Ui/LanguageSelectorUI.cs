using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using OdisseiaVR.Core;

namespace OdisseiaVR.UI
{
    /// <summary>
    /// Controla a seleção de idioma, move o destaque ciano para o botão ativo
    /// e garante feedback visual de hover com o laser VR.
    /// </summary>
    public class LanguageSelectorUI : MonoBehaviour
    {
        [Header("Botões de Idioma")]
        [SerializeField] private Button ptButton;
        [SerializeField] private Button enButton;
        [SerializeField] private Button esButton;

        // Cores do Design System OdisseiaVR
        private static readonly Color32 ActiveNormalColor   = new Color32(0, 184, 212, 245);   // Ciano selecionado
        private static readonly Color32 ActiveHoverColor    = new Color32(0, 128, 152, 255);   // Ciano mais escuro no hover
        private static readonly Color32 ActiveBorderColor   = new Color32(140, 248, 255, 255); // Borda ciano brilhante

        private static readonly Color32 InactiveNormalColor = new Color32(32, 40, 54, 235);    // Escuro padrão
        private static readonly Color32 InactiveHoverColor  = new Color32(10, 14, 20, 255);    // Bem mais escuro no hover
        private static readonly Color32 InactiveBorderColor = new Color32(255, 255, 255, 45);  // Borda sutil
        private static readonly Color32 HoverBorderColor    = new Color32(0, 217, 245, 255);   // Borda ciano ao apontar o raio

        private static readonly Color32 TextActiveColor     = new Color32(255, 255, 255, 255);
        private static readonly Color32 TextInactiveColor   = new Color32(235, 238, 245, 255);

        private LanguageButtonVisual ptVisual;
        private LanguageButtonVisual enVisual;
        private LanguageButtonVisual esVisual;

        private void Awake()
        {
            FixNestedCanvasRaycast();
            AutoFindButtonsIfMissing();

            ptVisual = SetupButton(ptButton, AppLanguage.PT);
            enVisual = SetupButton(enButton, AppLanguage.EN);
            esVisual = SetupButton(esButton, AppLanguage.ES);
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += UpdateAllVisuals;
            UpdateAllVisuals(LocalizationManager.CurrentLanguage);
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= UpdateAllVisuals;
        }

        private void OnDestroy()
        {
            if (ptButton != null) ptButton.onClick.RemoveAllListeners();
            if (enButton != null) enButton.onClick.RemoveAllListeners();
            if (esButton != null) esButton.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// Remove Canvas/Raycaster duplicados em Canvas_Language para que o laser VR
        /// do Canvas_LeftPanel (World Space) atinja os botões normalmente.
        /// </summary>
        private void FixNestedCanvasRaycast()
        {
            if (transform.parent == null) return;
            Canvas parentCanvas = transform.parent.GetComponentInParent<Canvas>();
            if (parentCanvas == null) return;

            var nestedRaycaster = GetComponent<BaseRaycaster>();
            if (nestedRaycaster != null) Destroy(nestedRaycaster);

            var nestedScaler = GetComponent<CanvasScaler>();
            if (nestedScaler != null) Destroy(nestedScaler);

            var nestedCanvas = GetComponent<Canvas>();
            if (nestedCanvas != null) Destroy(nestedCanvas);
        }

        private void AutoFindButtonsIfMissing()
        {
            if (ptButton != null && enButton != null && esButton != null) return;

            foreach (var btn in GetComponentsInChildren<Button>(true))
            {
                string n = btn.gameObject.name.ToLowerInvariant();
                if (ptButton == null && n.Contains("pt")) ptButton = btn;
                else if (enButton == null && n.Contains("en")) enButton = btn;
                else if (esButton == null && n.Contains("es")) esButton = btn;
            }
        }

        private LanguageButtonVisual SetupButton(Button btn, AppLanguage lang)
        {
            if (btn == null) return null;

            Navigation nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;

            btn.onClick.AddListener(() => SelectLanguage(lang));

            var visual = btn.GetComponent<LanguageButtonVisual>();
            if (visual == null)
                visual = btn.gameObject.AddComponent<LanguageButtonVisual>();

            visual.Initialize(btn);
            return visual;
        }

        private void SelectLanguage(AppLanguage lang)
        {
            LocalizationManager.SetLanguage(lang);

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);

            UpdateAllVisuals(LocalizationManager.CurrentLanguage);
        }

        private void UpdateAllVisuals(AppLanguage activeLang)
        {
            ptVisual?.SetSelectedState(activeLang == AppLanguage.PT);
            enVisual?.SetSelectedState(activeLang == AppLanguage.EN);
            esVisual?.SetSelectedState(activeLang == AppLanguage.ES);
        }

        private sealed class LanguageButtonVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            private Image targetImage;
            private Outline outline;
            private TextMeshProUGUI label;
            private bool isSelected;
            private bool isHovered;

            public void Initialize(Button button)
            {
                targetImage = button.targetGraphic as Image;
                if (targetImage == null)
                    targetImage = GetComponent<Image>() ?? GetComponentInChildren<Image>(true);

                label = GetComponentInChildren<TextMeshProUGUI>(true);

                outline = GetComponent<Outline>();
                if (outline == null)
                    outline = gameObject.AddComponent<Outline>();

                outline.effectDistance = new Vector2(2f, -2f);

                button.transition = Selectable.Transition.None;

                if (targetImage != null)
                {
                    targetImage.raycastTarget = true;
                    // Reseta o tint residual do CanvasRenderer deixado pelo ColorBlock
                    targetImage.CrossFadeColor(Color.white, 0f, true, true);
                }
            }

            public void SetSelectedState(bool selected)
            {
                isSelected = selected;
                ApplyVisuals();
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                isHovered = true;
                ApplyVisuals();
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                isHovered = false;
                ApplyVisuals();
            }

            private void ApplyVisuals()
            {
                if (targetImage == null) return;

                if (isSelected)
                {
                    targetImage.color = isHovered ? ActiveHoverColor : ActiveNormalColor;
                    if (outline != null) outline.effectColor = ActiveBorderColor;
                    if (label != null) label.color = TextActiveColor;
                }
                else
                {
                    targetImage.color = isHovered ? InactiveHoverColor : InactiveNormalColor;
                    if (outline != null) outline.effectColor = isHovered ? HoverBorderColor : InactiveBorderColor;
                    if (label != null) label.color = TextInactiveColor;
                }
            }
        }
    }
}