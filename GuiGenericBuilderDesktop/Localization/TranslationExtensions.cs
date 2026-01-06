using CompilationLib;

namespace GuiGenericBuilderDesktop.Localization
{
    /// <summary>
    /// Extension methods for translating BuildFlagItem and Parameter display text
    /// </summary>
    public static class TranslationExtensions
    {
        /// <summary>
        /// Gets the localized name for a BuildFlagItem based on current language
        /// </summary>
        public static string GetLocalizedName(this BuildFlagItem flag)
        {
            if (flag == null)
                return string.Empty;

            var currentLang = LocalizationManager.CurrentLanguage;
            
            // Check if translations exist and if current language has a translation
            if (flag.Translations != null && 
                flag.Translations.TryGetValue(currentLang, out var translation) &&
                !string.IsNullOrEmpty(translation.Name))
            {
                return translation.Name;
            }

            // Fallback to default FlagName
            return flag.FlagName ?? flag.Key ?? string.Empty;
        }

        /// <summary>
        /// Gets the localized description for a BuildFlagItem based on current language
        /// </summary>
        public static string GetLocalizedDescription(this BuildFlagItem flag)
        {
            if (flag == null)
                return string.Empty;

            var currentLang = LocalizationManager.CurrentLanguage;
            
            // Check if translations exist and if current language has a translation
            if (flag.Translations != null && 
                flag.Translations.TryGetValue(currentLang, out var translation) &&
                !string.IsNullOrEmpty(translation.Description))
            {
                return translation.Description;
            }

            // Fallback to default Description
            return flag.Description ?? string.Empty;
        }

        /// <summary>
        /// Gets the localized name for a Parameter based on current language
        /// </summary>
        public static string GetLocalizedName(this Parameter parameter)
        {
            if (parameter == null)
                return string.Empty;

            var currentLang = LocalizationManager.CurrentLanguage;
            
            // Check if translations exist and if current language has a translation
            if (parameter.Translations != null && 
                parameter.Translations.TryGetValue(currentLang, out var translation) &&
                !string.IsNullOrEmpty(translation.Name))
            {
                return translation.Name;
            }

            // Fallback to default Name
            return parameter.Name ?? parameter.Key ?? string.Empty;
        }

        /// <summary>
        /// Gets the localized description for a Parameter based on current language
        /// </summary>
        public static string GetLocalizedDescription(this Parameter parameter)
        {
            if (parameter == null)
                return string.Empty;

            var currentLang = LocalizationManager.CurrentLanguage;
            
            // Check if translations exist and if current language has a translation
            if (parameter.Translations != null && 
                parameter.Translations.TryGetValue(currentLang, out var translation) &&
                !string.IsNullOrEmpty(translation.Description))
            {
                return translation.Description;
            }

            // Fallback to empty string (parameters don't have default Description)
            return string.Empty;
        }

        /// <summary>
        /// Gets the localized name for an EnumValue based on current language
        /// </summary>
        public static string GetLocalizedName(this EnumValue enumValue, Parameter parentParameter)
        {
            if (enumValue == null)
                return string.Empty;

            var currentLang = LocalizationManager.CurrentLanguage;
            
            // Check if parent parameter has translations with EnumValues
            if (parentParameter?.Translations != null && 
                parentParameter.Translations.TryGetValue(currentLang, out var translation) &&
                translation.EnumValues != null)
            {
                // Find matching enum value by Value field
                var translatedEnum = translation.EnumValues
                    .FirstOrDefault(e => e.Value == enumValue.Value);
                    
                if (translatedEnum != null && !string.IsNullOrEmpty(translatedEnum.Name))
                {
                    return translatedEnum.Name;
                }
            }

            // Fallback to default Name
            return enumValue.Name ?? enumValue.Value ?? string.Empty;
        }

        /// <summary>
        /// Gets the localized description for an EnumValue based on current language
        /// </summary>
        public static string GetLocalizedDescription(this EnumValue enumValue, Parameter parentParameter)
        {
            if (enumValue == null)
                return string.Empty;

            var currentLang = LocalizationManager.CurrentLanguage;
            
            // Check if parent parameter has translations with EnumValues
            if (parentParameter?.Translations != null && 
                parentParameter.Translations.TryGetValue(currentLang, out var translation) &&
                translation.EnumValues != null)
            {
                // Find matching enum value by Value field
                var translatedEnum = translation.EnumValues
                    .FirstOrDefault(e => e.Value == enumValue.Value);
                    
                if (translatedEnum != null && !string.IsNullOrEmpty(translatedEnum.Description))
                {
                    return translatedEnum.Description;
                }
            }

            // Fallback to default Description
            return enumValue.Description ?? string.Empty;
        }
    }
}
