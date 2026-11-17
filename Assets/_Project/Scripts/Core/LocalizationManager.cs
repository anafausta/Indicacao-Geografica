using System;

namespace OdisseiaVR.Core
{
    public enum AppLanguage { PT, EN, ES }

    public static class LocalizationManager
    {
        // Idioma padrão ao abrir o jogo
        public static AppLanguage CurrentLanguage { get; private set; } = AppLanguage.PT;
        
        // Evento disparado sempre que o idioma muda
        public static event Action<AppLanguage> OnLanguageChanged;

        public static void SetLanguage(AppLanguage newLang)
        {
            if (CurrentLanguage == newLang) return;
            
            CurrentLanguage = newLang;
            OnLanguageChanged?.Invoke(newLang);
        }
    }
}