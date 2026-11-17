using UnityEngine;
using UnityEngine.UI;
using OdisseiaVR.Core;

namespace OdisseiaVR.UI
{
    /// <summary>
    /// Controla a interface de seleção de idioma.
    /// </summary>
    public class LanguageSelectorUI : MonoBehaviour
    {
        [Header("Botões de Idioma")]
        [SerializeField] private Button ptButton;
        [SerializeField] private Button enButton;
        [SerializeField] private Button esButton;

        private void Awake()
        {
            if (ptButton != null)
                ptButton.onClick.AddListener(() => SetLanguage(AppLanguage.PT));
            if (enButton != null)
                enButton.onClick.AddListener(() => SetLanguage(AppLanguage.EN));
            if (esButton != null)
                esButton.onClick.AddListener(() => SetLanguage(AppLanguage.ES));
        }

        private void OnDestroy()
        {
            if (ptButton != null)
                ptButton.onClick.RemoveAllListeners();
            if (enButton != null)
                enButton.onClick.RemoveAllListeners();
            if (esButton != null)
                esButton.onClick.RemoveAllListeners();
        }

        private void SetLanguage(AppLanguage lang)
        {
            LocalizationManager.SetLanguage(lang);
        }
    }
}