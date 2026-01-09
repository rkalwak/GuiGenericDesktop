using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Newtonsoft.Json;
using CompilationLib;
using Serilog;
using GuiGenericBuilderDesktop.Localization;
using GuiGenericBuilderDesktop.Services;

namespace GuiGenericBuilderDesktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<BuildFlagItem> AllBuildFlags { get; set; }
        GitHubRepoDownloader _gitHubRepoDownloader = new GitHubRepoDownloader();
        DeviceDetector _deviceDetector = new(new EsptoolWrapper());
        string _repositoryPath = string.Empty;
        string _port = string.Empty;
        string _chip = string.Empty;
        BuildConfigurationManager _configManager;
        private ComboBox boardSelector;
        private ComboBox comPortSelector;
        private ComboBox flashSizeSelector;
        private ComboBox languageSelector;
        private CheckBox deployCheckBox;
        private CheckBox backupCheckBox;
        private CheckBox eraseFlashCheckBox;
        private readonly ILogger _logger;
        private Button updateGGButton;
        private Button checkDeviceButton;
        private Button compileButton;
        private TextBlock statusText;
        private CancellationTokenSource _compilationCancellation;
        private BuilderConfig _builderConfig = new BuilderConfig();
        
        // Service layer
        private ValidationService _validationService;
        private DeviceManagementService _deviceManagementService;
        private VersionService _versionService;
        private UIBuilderService _uiBuilderService;

        public MainWindow()
        {
            InitializeComponent();
            _logger = Log.ForContext<MainWindow>();
            _logger.Information("MainWindow initializing");

            AllBuildFlags = new List<BuildFlagItem>();

            // Initialize configuration manager
            var configDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configurations");
            _configManager = new BuildConfigurationManager(configDir);

            InitializeBuildFlags();

            // Add the Parameters column dynamically so it's visible in the grid
            AddParametersColumnDynamically();

            FlagsDataGrid.ItemsSource = AllBuildFlags;
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GGLocal")))
            {
                _repositoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "repo", "gg");
            }
            else
            {
                _repositoryPath = @"c:\repozytoria\platformio\GUI-Generic";
            }

            // Initialize services
            _validationService = new ValidationService(_logger);
            _deviceManagementService = new DeviceManagementService(_deviceDetector, _logger);
            _versionService = new VersionService(_repositoryPath, _logger);
            _uiBuilderService = new UIBuilderService(_builderConfig, _logger);

            // Update version display and window title on startup
            var (suplaVersion, ggVersion) = _versionService.GetVersions();
            Title = _versionService.GenerateWindowTitle(suplaVersion, ggVersion);

            _logger.Information("MainWindow initialized successfully");
        }

        private void AddParametersColumnDynamically()
        {
            // Prevent adding twice
            if (FlagsDataGrid.Columns.Any(c => string.Equals(c.Header?.ToString(), "Parameters", StringComparison.OrdinalIgnoreCase)))
                return;

            var templateCol = new DataGridTemplateColumn { Header = "Parameters", Width = new DataGridLength(120) };

            // Create DataTemplate in code
            var buttonFactory = new FrameworkElementFactory(typeof(Button));
            buttonFactory.SetValue(Button.ContentProperty, "Params...");
            buttonFactory.SetValue(Button.PaddingProperty, new Thickness(6, 2, 6, 2));
            buttonFactory.SetValue(Button.HorizontalAlignmentProperty, HorizontalAlignment.Center);

            // Bind Tag to entire row (the BuildFlagItem)
            var tagBinding = new Binding(); // binds to DataContext (row item)
            buttonFactory.SetBinding(Button.TagProperty, tagBinding);
            // Register Click handler
            buttonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(EditParameters_Click));

            var dataTemplate = new DataTemplate { VisualTree = buttonFactory };
            templateCol.CellTemplate = dataTemplate;

            // Insert before Description column if possible, otherwise add to end
            int insertIndex = Math.Max(0, FlagsDataGrid.Columns.Count - 1);
            FlagsDataGrid.Columns.Insert(insertIndex, templateCol);
        }

        private void InitializeBuildFlags()
        {
            try
            {
                string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "builder.json");

                if (!File.Exists(jsonPath))
                {
                    MessageBox.Show(
                        LocalizationManager.GetFormat("BuilderJsonNotFound", jsonPath),
                        LocalizationManager.Get("FileNotFound"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                string jsonContent = File.ReadAllText(jsonPath);

                // Use Newtonsoft.Json for deserialization
                _builderConfig = JsonConvert.DeserializeObject<BuilderConfig>(jsonContent);

                if (_builderConfig?.Sections == null)
                {
                    MessageBox.Show(
                        LocalizationManager.Get("InvalidSections"),
                        LocalizationManager.Get("Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }


                // First pass: populate AllBuildFlags with metadata but keep IsEnabled as the deserialized default
                foreach (var sectionItem in _builderConfig.Sections.OrderBy(X => X.Value.Order))
                {
                    foreach (var flagItem in sectionItem.Value.Flags)
                    {
                        flagItem.Value.Section = sectionItem.Key;
                        flagItem.Value.Key = flagItem.Key;

                        // Initialize parameter values from DefaultValue if Value is not set
                        if (flagItem.Value.Parameters != null)
                        {
                            foreach (var param in flagItem.Value.Parameters)
                            {
                                if (string.IsNullOrEmpty(param.Value) && !string.IsNullOrEmpty(param.DefaultValue))
                                {
                                    param.Value = param.DefaultValue;
                                }
                            }
                        }

                        AllBuildFlags.Add(flagItem.Value);
                    }
                }

                BuildFlowDocument();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationManager.GetFormat("ErrorLoadingBuilderJson", ex.Message),
                    LocalizationManager.Get("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BuildFlowDocument()
        {
            var doc = new FlowDocument
            {
                PagePadding = new Thickness(12),
                ColumnGap = 24,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 12
            };
            
            // Add Device detection panel
            var devicePanel = new DockPanel { LastChildFill = false, Margin = new Thickness(12, 8, 12, 6) };

            // Board selector ComboBox
            var boardLabel = new TextBlock(new Run(LocalizationManager.Get("Board"))) { FontWeight = FontWeights.SemiBold, Margin = new Thickness(12, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };
            boardSelector = new ComboBox { Width = 220, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            boardSelector.Items.Add(new ComboBoxItem { Content = LocalizationManager.Get("None"), Tag = "None", IsSelected = true });
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP32 (default)", Tag = "GUI_Generic_ESP32" });
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP32-C3", Tag = "GUI_Generic_ESP32C3" });
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP32-C6", Tag = "GUI_Generic_ESP32C6" });
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP32-S3", Tag = "GUI_Generic_ESP32S3" });

            // Add SelectionChanged handler to validate platform compatibility when user manually selects a board
            boardSelector.SelectionChanged += (s, e) =>
            {
                if (boardSelector.SelectedItem is ComboBoxItem selectedItem)
                {
                    _chip = selectedItem.Content?.ToString()?.ToLower() ?? string.Empty;

                    if (!string.IsNullOrEmpty(_chip))
                    {
                        var disabledFlags = _validationService.DisableIncompatibleFlags(_chip, AllBuildFlags);
                        
                        // Notify user if any flags were disabled
                        if (disabledFlags.Any())
                        {
                            var flagsList = string.Join("\n", disabledFlags.Select(f => $"• {f}"));
                            var message = LocalizationManager.GetFormat("PlatformCompatibility", _chip, flagsList);

                            MessageBox.Show(
                                message,
                                LocalizationManager.Get("PlatformCompatibilityTitle"),
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                    }
                }
            };

            var loadConfigButton = new Button
            {
                Content = LocalizationManager.Get("ManageConfigs"),
                Width = 160,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(4)
            };
            loadConfigButton.Click += LoadConfig_Click;

            updateGGButton = new Button
            {
                Content = LocalizationManager.Get("UpdateGuiGeneric"),
                Width = 160,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(4)
            };
            updateGGButton.Click += UpdateGG_Click;

            checkDeviceButton = new Button { Content = LocalizationManager.Get("CheckDevice"), Width = 140, Height = 28, Margin = new Thickness(8, 0, 0, 0) };
            checkDeviceButton.Click += CheckConnectedDevice_Click;

            compileButton = new Button
            {
                Content = LocalizationManager.Get("Compile"),
                Width = 140,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(4)
            };
            compileButton.Click += CompileSelected_Click;

            // COM port selector (COM1..COM10)
            var portLabel = new TextBlock(new Run(LocalizationManager.Get("Port"))) { FontWeight = FontWeights.SemiBold, Margin = new Thickness(12, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };
            comPortSelector = new ComboBox { Width = 100, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            comPortSelector.Items.Add(new ComboBoxItem { Content = LocalizationManager.Get("None"), Tag = "None", IsSelected = true });
            for (int i = 1; i <= 100; i++)
            {
                var item = new ComboBoxItem { Content = $"COM{i}", Tag = $"COM{i}" };
                comPortSelector.Items.Add(item);
            }
            comPortSelector.SelectionChanged += (s, e) =>
            {
                if (comPortSelector.SelectedItem is ComboBoxItem ci)
                {
                    _port = (ci.Tag as string) ?? (ci.Content as string) ?? string.Empty;
                }
            };

            devicePanel.Children.Add(portLabel);
            devicePanel.Children.Add(comPortSelector);
            devicePanel.Children.Add(boardLabel);
            devicePanel.Children.Add(boardSelector);

            // Flash size selector
            var flashSizeLabel = new TextBlock(new Run(LocalizationManager.Get("Flash"))) { FontWeight = FontWeights.SemiBold, Margin = new Thickness(12, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };
            flashSizeSelector = new ComboBox { Width = 120, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = LocalizationManager.Get("None"), Tag = "None", IsSelected = true });
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = "4MB", Tag = "4MB" });
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = "8MB", Tag = "8MB" });
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = "16MB", Tag = "16MB" });
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = "32MB", Tag = "32MB" });
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = "64MB", Tag = "64MB" });
            devicePanel.Children.Add(flashSizeLabel);
            devicePanel.Children.Add(flashSizeSelector);

            // Language selector
            var languageLabel = new TextBlock(new Run(LocalizationManager.Get("Language"))) 
            { 
                FontWeight = FontWeights.SemiBold, 
                Margin = new Thickness(12, 0, 4, 0), 
                VerticalAlignment = VerticalAlignment.Center 
            };
            
            // Remember current language before recreating selector
            var currentLanguageCode = LocalizationManager.CurrentLanguage;
            
            languageSelector = new ComboBox 
            { 
                Width = 120, 
                Margin = new Thickness(8, 0, 0, 0), 
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = LocalizationManager.Get("LanguageTooltip")
            };
            
            var languages = LocalizationManager.GetAvailableLanguages();
            languageSelector.ItemsSource = languages;
            
            // Set the previously selected language
            var selectedLanguage = languages.FirstOrDefault(l => l.Code == currentLanguageCode);
            if (selectedLanguage != null)
            {
                languageSelector.SelectedItem = selectedLanguage;
            }
            else
            {
                languageSelector.SelectedIndex = 0; // Fallback to Polish
            }
            
            // Flag to prevent recursive calls during UI rebuild
            bool isChangingLanguage = false;
            
            languageSelector.SelectionChanged += async (s, e) =>
            {
                if (isChangingLanguage) return;
                
                if (languageSelector.SelectedItem is LanguageOption option)
                {
                    // Skip if this is the same language (happens during rebuild)
                    if (option.Code == currentLanguageCode) return;
                    
                    _logger.Information("Changing language to: {Language}", option.Code);
                    
                    isChangingLanguage = true;
                    try
                    {
                        LocalizationManager.SetLanguage(option.Code);
                        
                        // Rebuild UI with new language
                        BuildFlowDocument();
                        
                        // Show confirmation message
                        await Dispatcher.InvokeAsync(() =>
                        {
                            MessageBox.Show(
                                LocalizationManager.GetFormat("LanguageChanged", option.NativeName),
                                LocalizationManager.Get("Success"),
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error rebuilding UI after language change");
                        MessageBox.Show(
                            $"Error applying language change: {ex.Message}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                    finally
                    {
                        isChangingLanguage = false;
                    }
                }
            };
            
            devicePanel.Children.Add(languageLabel);
            devicePanel.Children.Add(languageSelector);

            // Deploy checkbox - positioned right before compile button
            deployCheckBox = new CheckBox
            {
                Content = LocalizationManager.Get("Deploy"),
                IsChecked = true,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 4, 0),
                FontWeight = FontWeights.SemiBold
            };

            // Backup checkbox - positioned right before deploy checkbox
            backupCheckBox = new CheckBox
            {
                Content = LocalizationManager.Get("Backup"),
                IsChecked = true,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 4, 0),
                FontWeight = FontWeights.SemiBold,
                ToolTip = LocalizationManager.Get("BackupTooltip")
            };

            // Erase Flash checkbox - positioned right before backup checkbox
            eraseFlashCheckBox = new CheckBox
            {
                Content = LocalizationManager.Get("EraseFlash"),
                IsChecked = false,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 4, 0),
                FontWeight = FontWeights.SemiBold,
                ToolTip = LocalizationManager.Get("EraseFlashTooltip")
            };

            DockPanel.SetDock(compileButton, Dock.Right);
            devicePanel.Children.Add(compileButton);
            DockPanel.SetDock(deployCheckBox, Dock.Right);
            devicePanel.Children.Add(deployCheckBox);
            DockPanel.SetDock(backupCheckBox, Dock.Right);
            devicePanel.Children.Add(backupCheckBox);
            DockPanel.SetDock(eraseFlashCheckBox, Dock.Right);
            devicePanel.Children.Add(eraseFlashCheckBox);

            DockPanel.SetDock(checkDeviceButton, Dock.Right);
            devicePanel.Children.Add(checkDeviceButton);
            DockPanel.SetDock(updateGGButton, Dock.Right);
            devicePanel.Children.Add(updateGGButton);

            DockPanel.SetDock(loadConfigButton, Dock.Right);
            devicePanel.Children.Add(loadConfigButton);

            doc.Blocks.Add(new BlockUIContainer(devicePanel));

            // Status text row - separate from controls for better visibility
            statusText = new TextBlock
            {
                Text = string.Empty,
                Margin = new Thickness(12, 4, 12, 4),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                FontWeight = FontWeights.Normal,
                FontSize = 13,
                Foreground = System.Windows.Media.Brushes.DarkBlue,
                Visibility = Visibility.Collapsed,
                TextWrapping = TextWrapping.Wrap
            };

            doc.Blocks.Add(new BlockUIContainer(statusText));

            var grouped = AllBuildFlags.GroupBy(f => f.Section).ToList();

            foreach (var group in grouped)
            {
                // Get the section info from config to access translations
                string sectionDisplayName = group.Key;
                if (_builderConfig?.Sections != null && _builderConfig.Sections.TryGetValue(group.Key, out var sectionInfo))
                {
                    // Try to get translated section name
                    var currentLang = LocalizationManager.CurrentLanguage;
                    if (sectionInfo.Translations != null && sectionInfo.Translations.TryGetValue(currentLang, out var translatedName) && !string.IsNullOrEmpty(translatedName))
                    {
                        sectionDisplayName = translatedName;
                    }
                }

                // Section header with a checkbox to toggle all flags in the section
                var headerPanel = new DockPanel
                {
                    LastChildFill = true,
                    Margin = new Thickness(0, 8, 0, 4),

                };
                var groupCheckBox = new CheckBox
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(2),
                    IsThreeState = false
                };
                // Initialize checked state based on group's items
                UpdateGroupCheckBoxState(groupCheckBox, group);

                // When checkbox toggled, set all items in the group accordingly
                groupCheckBox.Checked += (s, e) => SetGroupFlags(group, true, groupCheckBox);
                groupCheckBox.Unchecked += (s, e) => SetGroupFlags(group, false, groupCheckBox);

                var titleText = new TextBlock(new Run(sectionDisplayName + $" ({group.Count()})")) { FontWeight = FontWeights.Bold, FontSize = 14, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };

                headerPanel.Children.Add(groupCheckBox);
                headerPanel.Children.Add(titleText);

                // Subscribe to item property changes to update group checkbox state
                foreach (var item in group)
                {
                    item.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(BuildFlagItem.IsEnabled))
                        {
                            Dispatcher.Invoke(() => UpdateGroupCheckBoxState(groupCheckBox, group));
                        }
                    };
                }

                // Create bordered container for the section
                var border = new Border
                {
                    BorderBrush = System.Windows.Media.Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8),
                    Margin = new Thickness(0, 8, 0, 8),
                    Background = System.Windows.Media.Brushes.White
                };

                var panel = new StackPanel { Orientation = Orientation.Vertical };
                panel.Children.Add(headerPanel);

                // Grid to show items
                var grid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

                // header row - USE LOCALIZED STRINGS
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                AddText(grid, 0, 0, LocalizationManager.Get("Enabled"), FontWeights.SemiBold);
                AddText(grid, 0, 1, LocalizationManager.Get("Key"), FontWeights.SemiBold);
                AddText(grid, 0, 2, LocalizationManager.Get("Name"), FontWeights.SemiBold);
                AddText(grid, 0, 3, LocalizationManager.Get("Description"), FontWeights.SemiBold);
                AddText(grid, 0, 4, LocalizationManager.Get("Parameters"), FontWeights.SemiBold);

                int r = 1;
                foreach (var item in group.OrderBy(i => i.SectionOrder).ThenBy(x => x.Key))
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    var chk = new CheckBox { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2) };
                    chk.SetBinding(CheckBox.IsCheckedProperty, new Binding(nameof(BuildFlagItem.IsEnabled)) { Source = item, Mode = BindingMode.TwoWay });

                    chk.Checked += (s, e) =>
                    {
                        var errorMessage = DependencyResolver.ProcessFlagEnabled(item, AllBuildFlags);
                        // Only show error if the flag is NOT enabled (meaning ProcessFlagEnabled failed)
                        // If the flag IS enabled, it was successfully auto-enabled, so no error
                        if (errorMessage != null && !item.IsEnabled)
                        {
                            MessageBox.Show(
                                errorMessage,
                                LocalizationManager.Get("BlockingDependencies"),
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }

                        // Check platform compatibility when user enables a flag
                        if (item.IsEnabled && !string.IsNullOrEmpty(_chip))
                        {
                            if (item.DisabledOnPlatforms != null &&
                                item.DisabledOnPlatforms.Any(p => string.Equals(p, _chip, StringComparison.OrdinalIgnoreCase)))
                            {
                                // Flag is incompatible with current platform, disable it and show message
                                item.IsEnabled = false;
                                MessageBox.Show(
                                    LocalizationManager.GetFormat("PlatformIncompatibility", item.GetLocalizedName(), _chip, string.Join(", ", item.DisabledOnPlatforms)),
                                    LocalizationManager.Get("PlatformIncompatibilityTitle"),
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                            }
                        }
                    };
                    chk.Unchecked += (s, e) =>
                    {
                        DependencyResolver.ProcessFlagDisabled(item, AllBuildFlags);
                    };
                    Grid.SetRow(chk, r); Grid.SetColumn(chk, 0); grid.Children.Add(chk);

                    AddText(grid, r, 1, item.Key ?? string.Empty);
                    AddText(grid, r, 2, item.GetLocalizedName());
                    AddText(grid, r, 3, item.GetLocalizedDescription());

                    var btn = new Button { Content = LocalizationManager.Get("ParamsButton"), Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(2), Tag = item };
                    btn.Click += (s, e) =>
                    {
                        if ((s as Button)?.Tag is BuildFlagItem bf)
                        {
                            if (bf.Parameters == null || !bf.Parameters.Any()) return;

                            var editor = new ParametersEditorWindow(bf.Parameters, bf.GetLocalizedName());
                            editor.ShowDialog();
                        }
                    };
                    Grid.SetRow(btn, r);
                    Grid.SetColumn(btn, 4);
                    if (item.Parameters.Any())
                    {
                        grid.Children.Add(btn);
                    }

                    r++;
                }

                panel.Children.Add(grid);
                border.Child = panel;

                doc.Blocks.Add(new BlockUIContainer(border));

                // local helper
                void AddText(Grid g, int row, int col, string text, System.Windows.FontWeight? weight = null)
                {
                    var tb = new TextBlock(new Run(text)) { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 2, 4, 2) };
                    if (weight.HasValue) tb.FontWeight = weight.Value;
                    Grid.SetRow(tb, row); Grid.SetColumn(tb, col);
                    g.Children.Add(tb);
                }
            }




            docView.Document = doc;
        }

        private void EditSelectedParameters_Click(object sender, RoutedEventArgs e)
        {
            if (FlagsDataGrid.SelectedItem is BuildFlagItem item)
            {
                if (item.Parameters == null || !item.Parameters.Any())
                {
                    MessageBox.Show(
                        LocalizationManager.Get("NoParameters"),
                        LocalizationManager.Get("NoParametersTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var editor = new ParametersEditorWindow(item.Parameters, item.FlagName ?? item.Key);
                var res = editor.ShowDialog();
                if (res == true)
                {
                    // parameters edited in-place
                }
            }
            else
            {
                MessageBox.Show(
                    LocalizationManager.Get("NoSelection"),
                    LocalizationManager.Get("NoSelectionTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private async void UpdateGG_Click(object sender, RoutedEventArgs e)
        {
            // Disable button and show status
            updateGGButton.IsEnabled = false;
            statusText.Text = LocalizationManager.Get("DownloadingRepository");
            statusText.Visibility = Visibility.Visible;

            try
            {
                _repositoryPath = await _gitHubRepoDownloader.DownloadRepositoryAsync(
                    owner: "rkalwak",
                    repo: "GUI-Generic",
                    destinationRoot: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "repo"),
                    destinationSubdir: "gg",
                    branch: "master",
                    cancellationToken: CancellationToken.None);

                // Update version display after successful download
                var (suplaVersion, ggVersion) = _versionService.GetVersions();
                Title = _versionService.GenerateWindowTitle(suplaVersion, ggVersion);

                // Success status
                statusText.Text = LocalizationManager.Get("RepositoryUpdatedSuccess");
                statusText.Foreground = System.Windows.Media.Brushes.Green;

                // Hide status after 3 seconds
                await Task.Delay(3000);
                statusText.Visibility = Visibility.Collapsed;
                statusText.Foreground = System.Windows.Media.Brushes.DarkBlue;

                MessageBox.Show(
                    LocalizationManager.Get("RepositoryUpdatedMessage"),
                    LocalizationManager.Get("UpdateComplete"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // Error status
                statusText.Text = LocalizationManager.Get("RepositoryUpdateFailed");
                statusText.Foreground = System.Windows.Media.Brushes.Red;

                // Hide status after 3 seconds
                await Task.Delay(3000);
                statusText.Visibility = Visibility.Collapsed;
                statusText.Foreground = System.Windows.Media.Brushes.DarkBlue;

                MessageBox.Show(
                    LocalizationManager.GetFormat("ErrorUpdatingRepository", ex.Message),
                    LocalizationManager.Get("UpdateFailed"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                // Re-enable button
                updateGGButton.IsEnabled = true;
            }
        }

        private void SetGroupFlags(IGrouping<string, BuildFlagItem> group, bool value, CheckBox source)
        {
            foreach (var item in group)
            {
                item.IsEnabled = value;
            }

            // Update checkbox state explicitly after changes
            UpdateGroupCheckBoxState(source, group);
        }

        private void UpdateGroupCheckBoxState(CheckBox checkBox, IGrouping<string, BuildFlagItem> group)
        {
            int total = group.Count();
            int on = group.Count(i => i.IsEnabled);

            if (on == 0)
                checkBox.IsChecked = false;
            else if (on == total)
                checkBox.IsChecked = true;
            else
                checkBox.IsChecked = null; // Indeterminate
        }

        private async void CompileSelected_Click(object sender, RoutedEventArgs e)
        {
            // Check if we're stopping an ongoing compilation
            if (_compilationCancellation != null && !_compilationCancellation.IsCancellationRequested)
            {
                _logger.Information("User requested compilation cancellation");

                // Request cancellation
                _compilationCancellation.Cancel();

                // Update button text
                compileButton.Content = LocalizationManager.Get("Compile");

                // Update status
                statusText.Text = LocalizationManager.Get("StoppingCompilation");
                statusText.Foreground = System.Windows.Media.Brushes.Orange;

                return;
            }

            // Check if GUI-Generic repository exists and is not empty
            if (string.IsNullOrEmpty(_repositoryPath) || !Directory.Exists(_repositoryPath))
            {
                MessageBox.Show(
                    LocalizationManager.Get("RepositoryNotFound"),
                    LocalizationManager.Get("RepositoryNotFoundTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Check if the repository directory is empty
            if (!Directory.EnumerateFileSystemEntries(_repositoryPath).Any())
            {
                MessageBox.Show(
                    LocalizationManager.GetFormat("EmptyRepository", _repositoryPath),
                    LocalizationManager.Get("EmptyRepositoryTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Verify essential files exist in the repository
            var platformioIniPath = Path.Combine(_repositoryPath, "platformio.ini");
            if (!File.Exists(platformioIniPath))
            {
                MessageBox.Show(
                    LocalizationManager.GetFormat("IncompleteRepository", _repositoryPath),
                    LocalizationManager.Get("IncompleteRepositoryTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            List<BuildFlagItem> selectedFlags = AllBuildFlags.Where(f => f.IsEnabled).ToList();
            if (!selectedFlags.Any())
            {
                MessageBox.Show(
                    LocalizationManager.Get("NoFlagsSelected"),
                    LocalizationManager.Get("NoFlags"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Get selected platform
            var selectedPlatform = (boardSelector?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;

            // Validate platform is selected
            if (string.IsNullOrEmpty(selectedPlatform) || selectedPlatform.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    LocalizationManager.Get("PlatformRequired"),
                    LocalizationManager.Get("PlatformRequiredTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Validate platform compatibility
            var incompatibleFlags = _validationService.ValidatePlatformCompatibility(_chip, selectedFlags);
            if (incompatibleFlags.Any())
            {
                var flagsList = string.Join("\n", incompatibleFlags.Select(f => $"• {f}"));
                var message = LocalizationManager.GetFormat("PlatformCompatibilityError", selectedPlatform, flagsList);

                MessageBox.Show(
                    message,
                    LocalizationManager.Get("PlatformCompatibilityErrorTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Validate I2C devices have consistent SCL and SDA values
            var i2cValidationError = _validationService.ValidateI2CParameters(selectedFlags);
            if (!string.IsNullOrEmpty(i2cValidationError))
            {
                MessageBox.Show(
                    i2cValidationError,
                    LocalizationManager.Get("I2CConfigurationErrorTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Get and validate flash size selection
            var selectedFlashSize = (flashSizeSelector?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;

            // Get deploy and backup checkbox states
            bool shouldDeploy = deployCheckBox?.IsChecked ?? true;
            bool shouldBackup = backupCheckBox?.IsChecked ?? true;
            bool shouldEraseFlash = eraseFlashCheckBox?.IsChecked ?? false;

            // Validate COM port selection only if deploying
            if (shouldDeploy)
            {
                var selectedComPort = (comPortSelector?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(selectedComPort) || selectedComPort.Equals("None", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(
                        LocalizationManager.Get("COMPortRequired"),
                        LocalizationManager.Get("COMPortRequiredTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            // Create new cancellation token source for this compilation
            _compilationCancellation = new CancellationTokenSource();

            // Change button text to "Stop"
            compileButton.Content = LocalizationManager.Get("StopCompilation");

            // Track compilation time
            var compilationStopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Show status indicator with initial time
            statusText.Text = LocalizationManager.GetFormat("CompilingFirmware", 0.0);
            statusText.Foreground = System.Windows.Media.Brushes.Black;
            statusText.FontStyle = FontStyles.Oblique;
            statusText.Visibility = Visibility.Visible;

            // Start a timer to update elapsed time
            var timerCancellation = new CancellationTokenSource();
            var timerTask = Task.Run(async () =>
            {
                try
                {
                    while (!timerCancellation.Token.IsCancellationRequested)
                    {
                        await Task.Delay(100, timerCancellation.Token); // Update every 100ms for smooth display

                        var elapsed = compilationStopwatch.Elapsed.TotalSeconds;
                        Dispatcher.Invoke(() =>
                        {
                            statusText.Text = LocalizationManager.GetFormat("CompilingFirmware", elapsed);
                        });
                    }
                }
                catch (TaskCanceledException)
                {
                    // Timer cancelled, this is expected
                }
            }, timerCancellation.Token);

            try
            {
                var ggRequest = new CompileRequest
                {
                    BuildFlags = selectedFlags,
                    Platform = selectedPlatform,
                    ProjectPath = Path.Combine(_repositoryPath, "src"),
                    ProjectDirectory = _repositoryPath,
                    LibrariesPath = Path.Combine(_repositoryPath, "lib"),
                    PortCom = (comPortSelector?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty,
                    ShouldDeploy = shouldDeploy,
                    ShouldBackup = shouldBackup,
                    ShouldEraseFlash = shouldEraseFlash,
                    GlobalSettings = _builderConfig.GlobalSettings
                };
                var handler = new PlatformioCliHandler();
                ICompileHandler compiler = new PlatformioCliHandler();
                var result = await compiler.Handle(ggRequest, _compilationCancellation.Token);

                // Stop the timer and stopwatch
                timerCancellation.Cancel();
                compilationStopwatch.Stop();
                try
                {
                    await timerTask;
                }
                catch (TaskCanceledException)
                {
                    // Expected
                }

                var compilationTime = compilationStopwatch.Elapsed;

                // Check if compilation was cancelled
                if (_compilationCancellation.IsCancellationRequested)
                {
                    _logger.Information("Compilation cancelled by user");

                    statusText.Text = LocalizationManager.GetFormat("CompilationStopped", compilationTime.TotalSeconds);
                    statusText.Foreground = System.Windows.Media.Brushes.Black;
                    statusText.FontStyle = FontStyles.Oblique;

                    MessageBox.Show(
                        LocalizationManager.GetFormat("CompilationStoppedMessage", compilationTime.TotalSeconds),
                        LocalizationManager.Get("CompilationStoppedTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else if (result.IsSuccessful)
                {
                    // Success status with compilation time - KEEP IT VISIBLE
                    statusText.Text = LocalizationManager.GetFormat("CompilationSuccessful", compilationTime.TotalSeconds);
                    statusText.Foreground = System.Windows.Media.Brushes.Black;
                    statusText.FontStyle = FontStyles.Oblique;
                    // DO NOT hide the status - keep it visible

                    // Generate encoded configuration string
                    var encodedConfig = BuildConfigurationHasher.EncodeOptions(selectedFlags);

                    // Save configuration with hash
                    try
                    {
                        var platform = selectedPlatform;
                        var comPort = (comPortSelector?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;
                        var compiledFirmwarePath = Path.Combine(result.OutputDirectory, result.OutputFile);

                        _configManager.SaveConfiguration(
                            selectedFlags,
                            configName: null,
                            platform: platform,
                            comPort: comPort,
                            firmwareFilePath: compiledFirmwarePath,
                            buildOutputDirectory: result.OutputDirectory);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to save configuration: {ex.Message}");
                    }

                    // Show success results with encoded configuration string and backup path
                    // Use the copy in configurations directory instead of the build directory
                    var configurationsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configurations");
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var configBaseName = $"Config_{timestamp}";
                    var firmwareInConfigsPath = Path.Combine(configurationsDir, $"{configBaseName}.bin");

                    var resultsWindow = new CompilationResultsWindow(
                        encodedConfig,
                        true,
                        result.BackupFilePath,
                        firmwareInConfigsPath)
                    {
                        Owner = this
                    };
                    resultsWindow.ShowDialog();
                }

                else
                {
                    // Error status with compilation time - KEEP IT VISIBLE
                    statusText.Text = LocalizationManager.GetFormat("CompilationFailed", compilationTime.TotalSeconds);
                    statusText.Foreground = System.Windows.Media.Brushes.Red;
                    // DO NOT hide the status - keep it visible

                    // Show detailed logs in modal window
                    var resultsWindow = new CompilationResultsWindow(result.Logs)
                    {
                        Owner = this
                    };
                    resultsWindow.ShowDialog();
                }
            }
            catch (OperationCanceledException)
            {
                // Stop the timer and stopwatch
                timerCancellation.Cancel();
                compilationStopwatch.Stop();
                try
                {
                    await timerTask;
                }
                catch (TaskCanceledException)
                {
                    // Expected
                }

                _logger.Information("Compilation cancelled (OperationCanceledException caught)");

                statusText.Text = LocalizationManager.GetFormat("CompilationStopped", compilationStopwatch.Elapsed.TotalSeconds);
                statusText.Foreground = System.Windows.Media.Brushes.Orange;

                MessageBox.Show(
                    LocalizationManager.GetFormat("CompilationStoppedMessage", compilationStopwatch.Elapsed.TotalSeconds),
                    LocalizationManager.Get("CompilationStoppedTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // Stop the timer and stopwatch
                timerCancellation.Cancel();
                compilationStopwatch.Stop();
                try
                {
                    await timerTask;
                }
                catch (TaskCanceledException)
                {
                    // Expected
                }

                // Error status with time - KEEP IT VISIBLE
                statusText.Text = LocalizationManager.GetFormat("CompilationError", compilationStopwatch.Elapsed.TotalSeconds);
                statusText.Foreground = System.Windows.Media.Brushes.Red;
                // DO NOT hide the status - keep it visible

                MessageBox.Show(
                    LocalizationManager.GetFormat("CompilationErrorMessage", ex.Message),
                    LocalizationManager.Get("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                // Restore button text to "3. Compile"
                compileButton.Content = LocalizationManager.Get("Compile");

                // Clean up cancellation token source
                _compilationCancellation?.Dispose();
                _compilationCancellation = null;
            }

        }

        private async void CheckConnectedDevice_Click(object sender, RoutedEventArgs e)
        {
            // Disable button and show status
            checkDeviceButton.IsEnabled = false;
            statusText.Text = LocalizationManager.Get("DetectingDevice");
            statusText.Visibility = Visibility.Visible;
            statusText.Foreground = System.Windows.Media.Brushes.DarkBlue;

            try
            {
                var (port, deviceInfo) = await _deviceManagementService.DetectDeviceAsync();

                await Dispatcher.InvokeAsync(() =>
                {
                    if (!string.IsNullOrWhiteSpace(port))
                    {
                        // Update COM port selector
                        comPortSelector.SelectedItem = _deviceManagementService.FindComboBoxItemByTag(comPortSelector, port);
                        _logger.Debug("COM port selector updated to: {Port}", port);

                        // Update chip and board selector
                        _chip = deviceInfo?.ChipType ?? string.Empty;
                        _chip = _chip.ToLowerInvariant();
                        
                        if (!string.IsNullOrWhiteSpace(_chip) && boardSelector != null)
                        {
                            var platformTag = _deviceManagementService.GetPlatformTagFromChip(_chip);
                            
                            if (platformTag != null)
                            {
                                var match = _deviceManagementService.FindComboBoxItemByTag(boardSelector, platformTag);
                                if (match != null)
                                {
                                    boardSelector.SelectedItem = match;
                                    _logger.Information("Board selector updated to: {BoardTag}", platformTag);

                                    // Disable incompatible flags for this platform
                                    var disabledFlags = _validationService.DisableIncompatibleFlags(_chip, AllBuildFlags);
                                    
                                    // Notify user if any flags were disabled
                                    if (disabledFlags.Any())
                                    {
                                        var flagsList = string.Join("\n", disabledFlags.Select(f => $"• {f}"));
                                        var message = LocalizationManager.GetFormat("PlatformCompatibility", _chip, flagsList);

                                        MessageBox.Show(
                                            message,
                                            LocalizationManager.Get("PlatformCompatibilityTitle"),
                                            MessageBoxButton.OK,
                                            MessageBoxImage.Information);
                                    }
                                }
                            }

                            // Set flash size selector if available
                            var fs = deviceInfo?.FlashSize ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(fs) && flashSizeSelector != null)
                            {
                                var normalized = fs.Trim().ToUpperInvariant();
                                var fmatch = flashSizeSelector.Items.OfType<ComboBoxItem>()
                                    .FirstOrDefault(ci => normalized.Contains((ci.Tag as string) ?? (ci.Content as string)));
                                if (fmatch != null)
                                {
                                    flashSizeSelector.SelectedItem = fmatch;
                                    _logger.Information("Flash size selector updated to: {FlashSize}", fs);
                                }
                            }
                        }

                        // Success status
                        statusText.Text = LocalizationManager.GetFormat("DeviceDetected", _chip, port);
                        statusText.Foreground = System.Windows.Media.Brushes.Green;

                        // Hide status after 3 seconds
                        Task.Run(async () =>
                        {
                            await Task.Delay(3000);
                            Dispatcher.Invoke(() =>
                            {
                                statusText.Visibility = Visibility.Collapsed;
                                statusText.Foreground = System.Windows.Media.Brushes.DarkBlue;
                            });
                        });
                    }
                    else
                    {
                        _logger.Warning("Device detection completed but no port found");

                        // No device status
                        statusText.Text = LocalizationManager.Get("NoDeviceDetected");
                        statusText.Foreground = System.Windows.Media.Brushes.OrangeRed;

                        // Hide status after 3 seconds
                        Task.Run(async () =>
                        {
                            await Task.Delay(3000);
                            Dispatcher.Invoke(() =>
                            {
                                statusText.Visibility = Visibility.Collapsed;
                                statusText.Foreground = System.Windows.Media.Brushes.DarkBlue;
                            });
                        });

                        MessageBox.Show(
                            LocalizationManager.Get("NoDeviceDetectedMessage"),
                            LocalizationManager.Get("DeviceNotFound"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }

                    // Re-enable button
                    checkDeviceButton.IsEnabled = true;
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error during device detection");
                
                await Dispatcher.InvokeAsync(() =>
                {
                    // Error status
                    statusText.Text = LocalizationManager.Get("DeviceDetectionError");
                    statusText.Foreground = System.Windows.Media.Brushes.Red;

                    // Hide status after 3 seconds
                    Task.Run(async () =>
                    {
                        await Task.Delay(3000);
                        Dispatcher.Invoke(() =>
                        {
                            statusText.Visibility = Visibility.Collapsed;
                            statusText.Foreground = System.Windows.Media.Brushes.DarkBlue;
                        });
                    });

                    // Re-enable button
                    checkDeviceButton.IsEnabled = true;

                    MessageBox.Show(
                        LocalizationManager.GetFormat("DeviceDetectionErrorMessage", ex.Message),
                        LocalizationManager.Get("DetectionError"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
        }

        private void EditParameters_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is BuildFlagItem item)
            {
                if (item.Parameters == null || !item.Parameters.Any())
                {
                    MessageBox.Show(
                        LocalizationManager.Get("ThisFlagHasNoParameters"),
                        LocalizationManager.Get("NoParametersTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var editor = new ParametersEditorWindow(item.Parameters, item.FlagName ?? item.Key);
                var res = editor.ShowDialog();
                if (res == true)
                {
                    // Parameters modified in-place; nothing else required.
                }
            }
        }

        private void LoadConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var managerWindow = new ConfigurationManagerWindow(AllBuildFlags)
                {
                    Owner = this
                };

                if (managerWindow.ShowDialog() == true && managerWindow.SelectedConfiguration != null)
                {
                    LoadConfiguration(managerWindow.SelectedConfiguration);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationManager.GetFormat("ErrorOpeningConfigManager", ex.Message),
                    LocalizationManager.Get("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadConfiguration(SavedBuildConfiguration config)
        {
            // Check if this is a placeholder configuration (no flags)
            if (config.EnabledFlagKeys == null || !config.EnabledFlagKeys.Any())
            {
                MessageBox.Show(
                    LocalizationManager.GetFormat("ManualConfigurationRequired", config.ConfigurationName),
                    LocalizationManager.Get("ManualConfigurationRequiredTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Disable all flags first
            foreach (var flag in AllBuildFlags)
            {
                flag.IsEnabled = false;
            }

            // Enable flags from the configuration
            foreach (var flagKey in config.EnabledFlagKeys)
            {
                var flag = AllBuildFlags.FirstOrDefault(f =>
                    string.Equals(f.Key, flagKey, StringComparison.OrdinalIgnoreCase));

                if (flag != null)
                {
                    flag.IsEnabled = true;
                }
            }

            // Restore parameter values if available
            if (config.BuildFlagsParameters != null && config.BuildFlagsParameters.Any())
            {
                foreach (var flagParams in config.BuildFlagsParameters)
                {
                    var flag = AllBuildFlags.FirstOrDefault(f =>
                        string.Equals(f.Key, flagParams.Key, StringComparison.OrdinalIgnoreCase));

                    if (flag != null && flag.Parameters != null)
                    {
                        foreach (var paramValue in flagParams.Value)
                        {
                            var parameter = flag.Parameters.FirstOrDefault(p =>
                                string.Equals(p.Identifier, paramValue.Key, StringComparison.OrdinalIgnoreCase));

                            if (parameter != null)
                            {
                                parameter.Value = paramValue.Value;
                            }
                        }
                    }
                }
            }

            // Restore platform selection if available
            if (!string.IsNullOrEmpty(config.Platform) && boardSelector != null)
            {
                var platformItem = boardSelector.Items.OfType<ComboBoxItem>()
                    .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), config.Platform, StringComparison.OrdinalIgnoreCase));

                if (platformItem != null)
                {
                    boardSelector.SelectedItem = platformItem;
                }
            }

            // Restore COM port selection if available
            if (!string.IsNullOrEmpty(config.ComPort) && comPortSelector != null)
            {
                var comPortItem = comPortSelector.Items.OfType<ComboBoxItem>()
                    .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), config.ComPort, StringComparison.OrdinalIgnoreCase));

                if (comPortItem != null)
                {
                    comPortSelector.SelectedItem = comPortItem;
                }
            }

            MessageBox.Show(
                LocalizationManager.GetFormat("ConfigurationLoaded", config.ConfigurationName, config.Platform, config.ComPort, config.EnabledFlagKeys.Count),
                LocalizationManager.Get("ConfigurationLoadedTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}