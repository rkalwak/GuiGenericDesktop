using CompilationLib;
using GuiGenericBuilderDesktop.Localization;
using Serilog;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace GuiGenericBuilderDesktop.Services
{
    /// <summary>
    /// Handles UI building operations, particularly FlowDocument generation
    /// </summary>
    public class UIBuilderService
    {
        private readonly ILogger _logger;
        private readonly BuilderConfig _builderConfig;

        public UIBuilderService(BuilderConfig builderConfig, ILogger logger)
        {
            _builderConfig = builderConfig;
            _logger = logger;
        }

        /// <summary>
        /// Creates a row header section with localized column names
        /// </summary>
        public void AddHeaderRow(Grid grid)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            AddText(grid, 0, 0, LocalizationManager.Get("Enabled"), FontWeights.SemiBold);
            AddText(grid, 0, 1, LocalizationManager.Get("Key"), FontWeights.SemiBold);
            AddText(grid, 0, 2, LocalizationManager.Get("Name"), FontWeights.SemiBold);
            AddText(grid, 0, 3, LocalizationManager.Get("Description"), FontWeights.SemiBold);
            AddText(grid, 0, 4, LocalizationManager.Get("Parameters"), FontWeights.SemiBold);
        }

        /// <summary>
        /// Adds a text block to a grid cell
        /// </summary>
        public void AddText(Grid grid, int row, int col, string text, FontWeight? weight = null)
        {
            var tb = new TextBlock(new Run(text)) 
            { 
                VerticalAlignment = VerticalAlignment.Center, 
                Margin = new Thickness(4, 2, 4, 2) 
            };
            
            if (weight.HasValue) 
                tb.FontWeight = weight.Value;
            
            Grid.SetRow(tb, row); 
            Grid.SetColumn(tb, col);
            grid.Children.Add(tb);
        }

        /// <summary>
        /// Gets the localized section display name
        /// </summary>
        public string GetLocalizedSectionName(string sectionKey)
        {
            string sectionDisplayName = sectionKey;
            
            if (_builderConfig?.Sections != null && _builderConfig.Sections.TryGetValue(sectionKey, out var sectionInfo))
            {
                // Try to get translated section name
                var currentLang = LocalizationManager.CurrentLanguage;
                if (sectionInfo.Translations != null && 
                    sectionInfo.Translations.TryGetValue(currentLang, out var translatedName) && 
                    !string.IsNullOrEmpty(translatedName))
                {
                    sectionDisplayName = translatedName;
                }
            }

            return sectionDisplayName;
        }

        /// <summary>
        /// Creates a bordered container for a section
        /// </summary>
        public Border CreateSectionBorder()
        {
            return new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 8, 0, 8),
                Background = Brushes.White
            };
        }

        /// <summary>
        /// Creates a grid for displaying flags
        /// </summary>
        public Grid CreateFlagsGrid()
        {
            var grid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            return grid;
        }
    }
}
