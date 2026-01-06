using System;
using System.Windows;

namespace GuiGenericBuilderDesktop.Localization
{
    /// <summary>
    /// Manages dynamic resource dictionary loading for localization
    /// </summary>
    public static class ResourceDictionaryManager
    {
        private static readonly Uri DefaultResourceUri = new Uri("/GuiGenericBuilderDesktop;component/Resources/Strings.xaml", UriKind.Relative);
        private static readonly Uri PolishResourceUri = new Uri("/GuiGenericBuilderDesktop;component/Resources/Strings.pl.xaml", UriKind.Relative);
        
        /// <summary>
        /// Loads the appropriate resource dictionary based on the current language
        /// </summary>
        public static void LoadLanguageResources(string languageCode)
        {
            var app = Application.Current;
            if (app == null) return;
            
            // Remove existing language resource dictionaries
            var dictionariesToRemove = new System.Collections.Generic.List<ResourceDictionary>();
            foreach (var dict in app.Resources.MergedDictionaries)
            {
                if (dict.Source == DefaultResourceUri || dict.Source == PolishResourceUri)
                {
                    dictionariesToRemove.Add(dict);
                }
            }
            
            foreach (var dict in dictionariesToRemove)
            {
                app.Resources.MergedDictionaries.Remove(dict);
            }
            
            // Add the appropriate resource dictionary
            var resourceUri = languageCode.ToLower() == "pl" ? PolishResourceUri : DefaultResourceUri;
            var newDictionary = new ResourceDictionary { Source = resourceUri };
            app.Resources.MergedDictionaries.Add(newDictionary);
        }
        
        /// <summary>
        /// Initializes the resource dictionaries on application startup
        /// </summary>
        public static void Initialize()
        {
            var currentLanguage = LocalizationManager.CurrentLanguage;
            LoadLanguageResources(currentLanguage);
        }
    }
}
