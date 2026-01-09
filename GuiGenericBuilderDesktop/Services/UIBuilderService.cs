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
        public void AddText(Grid grid, int row, int col, string text, FontWeight? weight = null, bool enableTextWrapping = false)
        {
            var tb = new TextBlock(new Run(text)) 
            { 
                VerticalAlignment = VerticalAlignment.Center, 
                Margin = new Thickness(4, 2, 4, 2),
            };
            
            if (weight.HasValue) 
                tb.FontWeight = weight.Value;
            
            if (enableTextWrapping)
            {
                tb.TextWrapping = TextWrapping.Wrap;
                tb.VerticalAlignment = VerticalAlignment.Top;
            }
            
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
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70), });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            return grid;
        }

        /// <summary>
        /// Adds a Parameters column to a DataGrid dynamically
        /// </summary>
        /// <param name="dataGrid">The DataGrid to add the column to</param>
        /// <param name="editParametersHandler">Click event handler for the parameters button</param>
        public void AddParametersColumnDynamically(DataGrid dataGrid, RoutedEventHandler editParametersHandler)
        {
            if (dataGrid == null)
                throw new ArgumentNullException(nameof(dataGrid));

            if (editParametersHandler == null)
                throw new ArgumentNullException(nameof(editParametersHandler));

            // Prevent adding twice
            if (dataGrid.Columns.Any(c => string.Equals(c.Header?.ToString(), "Parameters", StringComparison.OrdinalIgnoreCase)))
            {
                _logger.Debug("Parameters column already exists in DataGrid, skipping");
                return;
            }

            var templateCol = new DataGridTemplateColumn 
            { 
                Header = "Parameters", 
                Width = new DataGridLength(120) 
            };

            // Create DataTemplate in code
            var buttonFactory = new FrameworkElementFactory(typeof(Button));
            buttonFactory.SetValue(Button.ContentProperty, "Params...");
            buttonFactory.SetValue(Button.PaddingProperty, new Thickness(6, 2, 6, 2));
            buttonFactory.SetValue(Button.HorizontalAlignmentProperty, HorizontalAlignment.Center);

            // Bind Tag to entire row (the BuildFlagItem)
            var tagBinding = new Binding(); // binds to DataContext (row item)
            buttonFactory.SetBinding(Button.TagProperty, tagBinding);
            
            // Register Click handler
            buttonFactory.AddHandler(Button.ClickEvent, editParametersHandler);

            var dataTemplate = new DataTemplate { VisualTree = buttonFactory };
            templateCol.CellTemplate = dataTemplate;

            // Insert before Description column if possible, otherwise add to end
            int insertIndex = Math.Max(0, dataGrid.Columns.Count - 1);
            dataGrid.Columns.Insert(insertIndex, templateCol);

            _logger.Debug("Parameters column added to DataGrid at index {Index}", insertIndex);
        }
    }
}
