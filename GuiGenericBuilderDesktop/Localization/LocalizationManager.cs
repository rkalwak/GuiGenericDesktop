using System.Collections.Generic;
using System.Globalization;

namespace GuiGenericBuilderDesktop.Localization
{
    /// <summary>
    /// Manages localization strings for the application
    /// Default language: Polish (pl-PL)
    /// </summary>
    public static class LocalizationManager
    {
        private static string _currentLanguage = "pl"; // Polish as default (SHORT CODE to match builder.json)
        private static Dictionary<string, Dictionary<string, string>> _translations;

        static LocalizationManager()
        {
            InitializeTranslations();
            SetLanguage("pl");
        }

        /// <summary>
        /// Gets the current language code
        /// </summary>
        public static string CurrentLanguage => _currentLanguage;

        /// <summary>
        /// Sets the application language
        /// </summary>
        public static void SetLanguage(string languageCode)
        {
            // Normalize language code to short form (pl, en) to match builder.json
            var shortCode = languageCode;
            if (languageCode.Contains("-"))
            {
                shortCode = languageCode.Split('-')[0]; // "pl-PL" -> "pl", "en-US" -> "en"
            }
            
            if (_translations.ContainsKey(shortCode))
            {
                _currentLanguage = shortCode;
                
                try
                {
                    // Try to set culture with full code if available, otherwise use short code
                    CultureInfo culture;
                    if (shortCode == "pl")
                        culture = new CultureInfo("pl-PL");
                    else if (shortCode == "en")
                        culture = new CultureInfo("en-US");
                    else
                        culture = new CultureInfo(shortCode);
                        
                    CultureInfo.CurrentUICulture = culture;
                    CultureInfo.CurrentCulture = culture;
                }
                catch
                {
                    // Fallback to default Polish
                    _currentLanguage = "pl";
                }
            }
        }

        /// <summary>
        /// Gets a localized string by key
        /// </summary>
        public static string Get(string key)
        {
            if (_translations.ContainsKey(_currentLanguage) && 
                _translations[_currentLanguage].ContainsKey(key))
            {
                return _translations[_currentLanguage][key];
            }
            
            // Fallback to Polish
            if (_translations.ContainsKey("pl") && 
                _translations["pl"].ContainsKey(key))
            {
                return _translations["pl"][key];
            }
            
            return $"[{key}]"; // Return key in brackets if not found
        }

        /// <summary>
        /// Gets a localized string with format arguments
        /// </summary>
        public static string GetFormat(string key, params object[] args)
        {
            var format = Get(key);
            try
            {
                return string.Format(format, args);
            }
            catch
            {
                return format;
            }
        }

        /// <summary>
        /// Gets available languages
        /// </summary>
        public static List<LanguageOption> GetAvailableLanguages()
        {
            return new List<LanguageOption>
            {
                new LanguageOption { Code = "pl", Name = "Polski", NativeName = "Polski" },
                new LanguageOption { Code = "en", Name = "English", NativeName = "English" }
            };
        }

        private static void InitializeTranslations()
        {
            _translations = new Dictionary<string, Dictionary<string, string>>
            {
                ["pl"] = PolishTranslations.GetTranslations(),
                ["en"] = EnglishTranslations.GetTranslations()
            };
        }
    }

    /// <summary>
    /// Represents a language option
    /// </summary>
    public class LanguageOption
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string NativeName { get; set; }

        public override string ToString() => NativeName;
    }
}
