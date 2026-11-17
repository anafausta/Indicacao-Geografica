using UnityEngine;
using TMPro;
using OdisseiaVR.Core;

namespace OdisseiaVR.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedUIText : MonoBehaviour
    {
        [Header("Traduções")]
        [TextArea(1, 3)] public string textPT;
        [TextArea(1, 3)] public string textEN;
        [TextArea(1, 3)] public string textES;

        private TextMeshProUGUI tmpText;

        private void Awake()
        {
            tmpText = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            // Atualiza o texto imediatamente ao ligar o objeto
            UpdateText(LocalizationManager.CurrentLanguage);
            // Inscreve-se no evento de mudança
            LocalizationManager.OnLanguageChanged += UpdateText;
        }

        private void OnDisable()
        {
            // Desinscreve-se para evitar vazamento de memória
            LocalizationManager.OnLanguageChanged -= UpdateText;
        }

        private void UpdateText(AppLanguage lang)
        {
            if (tmpText == null) return;

            switch (lang)
            {
                case AppLanguage.PT: tmpText.text = textPT; break;
                case AppLanguage.EN: tmpText.text = textEN; break;
                case AppLanguage.ES: tmpText.text = textES; break;
            }
        }
    }
}