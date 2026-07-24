using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Newtonsoft.Json;
using CompilationLib;
using Serilog;
using Microsoft.Extensions.Configuration;
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
        IEsptoolWrapper _esptoolWrapper = null;
        DeviceDetector _deviceDetector;
        string _repositoryPath = string.Empty;
        string _portCom = string.Empty;
        string _board = string.Empty;

        private string _platform;
        string _flashSize = string.Empty;
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
        private AutoUpdateService _autoUpdateService;
        private GGUpdateService _ggUpdateService;
        private Z2SUpdateService _z2sUpdateService;

        // Z2S tab state
        private string _z2sChip = string.Empty;
        private string _z2sFlashSize = string.Empty;
        private string _z2sDeviceVersion = null;
        private int _z2sVersionHistoryCount = 10;
        private CancellationTokenSource _z2sCancellation;

        private async Task<CompilationResultsWindow> ShowZ2SOperationLogWindowAsync(string title)
        {
            CompilationResultsWindow window = null;

            await Dispatcher.InvokeAsync(() =>
            {
                window = new CompilationResultsWindow
                {
                    Owner = this
                };
                window.Show();
                window.SetLiveModeTitle(title);
            });

            return window;
        }

        private void AttachZ2SLogStreaming(CompilationResultsWindow window, out EventHandler<string> outputHandler, out EventHandler<string> errorHandler)
        {
            outputHandler = null;
            errorHandler = null;

            if (_esptoolWrapper is EsptoolWrapper esptool)
            {
                outputHandler = (_, line) => window?.AppendLog(line);
                errorHandler = (_, line) => window?.AppendLog(line);
                esptool.OutputLine += outputHandler;
                esptool.ErrorLine += errorHandler;
            }
        }

        private void DetachZ2SLogStreaming(EventHandler<string> outputHandler, EventHandler<string> errorHandler)
        {
            if (_esptoolWrapper is EsptoolWrapper esptool)
            {
                if (outputHandler != null)
                    esptool.OutputLine -= outputHandler;

                if (errorHandler != null)
                    esptool.ErrorLine -= errorHandler;
            }
        }

        public MainWindow()
        {
            _esptoolWrapper = new EsptoolWrapper();
            _deviceDetector = new DeviceDetector(_esptoolWrapper);
            InitializeComponent();
            _logger = Log.ForContext<MainWindow>();
            _logger.Information("MainWindow initializing");

            AllBuildFlags = new List<BuildFlagItem>();

            // Initialize configuration manager
            var configDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configurations");
            _configManager = new BuildConfigurationManager(configDir, _esptoolWrapper);

            // Load application configuration from appsettings.json and environment variables
            var appConfig = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .Build()
                .Get<AppConfig>() ?? new AppConfig();

            if (string.IsNullOrWhiteSpace(appConfig.GGLocal))
            {
                _repositoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "repo", "gg");
            }
            else
            {
                _repositoryPath = appConfig.GGLocal;
            }

            // Initialize services
            _validationService = new ValidationService(_logger);
            _deviceManagementService = new DeviceManagementService(_deviceDetector, _logger);
            _versionService = new VersionService(_repositoryPath, _logger);
            _uiBuilderService = new UIBuilderService(_builderConfig, _logger);
            _autoUpdateService = new AutoUpdateService("rkalwak", "GuiGenericDesktop", appConfig, _logger);
            _ggUpdateService = new GGUpdateService(_repositoryPath, appConfig, _logger);
            _z2sUpdateService = new Z2SUpdateService(_esptoolWrapper, appConfig, _logger);
            _z2sVersionHistoryCount = appConfig.Z2SVersionHistoryCount > 0 ? appConfig.Z2SVersionHistoryCount : 10;

            // Populate Z2S COM port selector
            z2sComPortSelector.Items.Add(new ComboBoxItem { Content = "None", Tag = "None", IsSelected = true });
            for (int i = 1; i <= 100; i++)
            {
                z2sComPortSelector.Items.Add(new ComboBoxItem { Content = $"COM{i}", Tag = $"COM{i}" });
            }

            // Populate Z2S flash size selector
            z2sFlashSizeSelector.Items.Add(new ComboBoxItem { Content = "Auto", Tag = string.Empty, IsSelected = true });
            foreach (var fs in new[] { "2MB", "4MB", "8MB", "16MB", "32MB" })
                z2sFlashSizeSelector.Items.Add(new ComboBoxItem { Content = fs, Tag = fs });
            z2sFlashSizeSelector.SelectionChanged += (s, e) =>
            {
                if (z2sFlashSizeSelector.SelectedItem is ComboBoxItem ci)
                    _z2sFlashSize = (ci.Tag as string) ?? string.Empty;
            };

            InitializeBuildFlags();
            // Add the Parameters column dynamically so it's visible in the grid
            _uiBuilderService.AddParametersColumnDynamically(FlagsDataGrid, EditParameters_Click);

            FlagsDataGrid.ItemsSource = AllBuildFlags;


            // Update version display and window title on startup
            var (suplaVersion, ggVersion) = _versionService.GetVersions();
            Title = _versionService.GenerateWindowTitle(suplaVersion, ggVersion);

            // Validate PlatformIO installation on startup
            _validationService.ShowPlatformIOWarningIfNeeded();

            // Add Window Loaded event handler for automatic update check
            Loaded += MainWindow_Loaded;

            _logger.Information("MainWindow initialized successfully");
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Automatically check for application updates on startup (non-blocking)
            try
            {
                _logger.Information("Performing automatic update check on startup");

                var (updateAvailable, latestRelease) = await _autoUpdateService.CheckForUpdatesAsync();

                if (updateAvailable && latestRelease != null)
                {
                    _logger.Information("Update available on startup: {Version}", latestRelease.TagName);

                    var result = MessageBox.Show(
                        LocalizationManager.GetFormat("NewVersionAvailable", latestRelease.TagName),
                        LocalizationManager.Get("UpdateAvailableTitle"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        var updateWindow = new UpdateWindow(_autoUpdateService, latestRelease, _logger)
                        {
                            Owner = this
                        };
                        updateWindow.ShowDialog();
                    }
                }
                else
                {
                    _logger.Information("Application is up to date (startup check)");
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to check for updates on startup (non-critical)");
                // Don't show error to user on startup - it's just a background check
            }

            // Check for GUI-Generic builder updates
            try
            {
                _logger.Information("Performing GUI-Generic builder update check on startup");

                var (ggUpdateAvailable, remoteVersion, currentVersion) = await _ggUpdateService.CheckForGGUpdatesAsync();

                if (ggUpdateAvailable && remoteVersion != null)
                {
                    _logger.Information("GUI-Generic builder update available: {RemoteVersion} (current: {CurrentVersion})", 
                        remoteVersion, currentVersion);

                    var result = MessageBox.Show(
                        LocalizationManager.GetFormat("GGUpdateAvailable", currentVersion, remoteVersion),
                        LocalizationManager.Get("GGUpdateAvailableTitle"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Trigger the update button click
                        UpdateGG_Click(updateGGButton, new RoutedEventArgs());
                    }
                }
                else
                {
                    _logger.Information("GUI-Generic builder is up to date (startup check)");
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to check for GUI-Generic builder updates on startup (non-critical)");
                // Don't show error to user on startup - it's just a background check
            }
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
            // Clear existing panels
            devicePanel.Children.Clear();
            buttonsPanel.Children.Clear();
            flagsContainer.Children.Clear();

            // ===== DEVICE PANEL (Row 0) - Port/Board/Flash/Language =====

            var portLabel = new TextBlock(new Run(LocalizationManager.Get("Port"))) 
            { 
                FontWeight = FontWeights.SemiBold, 
                Margin = new Thickness(4, 0, 4, 0), 
                VerticalAlignment = VerticalAlignment.Center 
            };
            comPortSelector = new ComboBox { Width = 80, Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            comPortSelector.Items.Add(new ComboBoxItem { Content = LocalizationManager.Get("None"), Tag = "None", IsSelected = true });
            for (int i = 1; i <= 100; i++)
            {
                comPortSelector.Items.Add(new ComboBoxItem { Content = $"COM{i}", Tag = $"COM{i}" });
            }
            comPortSelector.SelectionChanged += (s, e) =>
            {
                if (comPortSelector.SelectedItem is ComboBoxItem ci)
                {
                    _portCom = (ci.Tag as string) ?? (ci.Content as string) ?? string.Empty;
                }
            };

            var boardLabel = new TextBlock(new Run(LocalizationManager.Get("Board"))) 
            { 
                FontWeight = FontWeights.SemiBold, 
                Margin = new Thickness(4, 0, 4, 0), 
                VerticalAlignment = VerticalAlignment.Center 
            };
            boardSelector = new ComboBox { Width = 80, Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            boardSelector.Items.Add(new ComboBoxItem { Content = LocalizationManager.Get("None"), Tag = "None", IsSelected = true });
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP32", Tag = "GUI_Generic_ESP32" });
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP32-C3", Tag = "GUI_Generic_ESP32C3" });
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP32-C6", Tag = "GUI_Generic_ESP32C6" });
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP32-S3", Tag = "GUI_Generic_ESP32S3" });
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP32-S2", Tag = "GUI_Generic_ESP32S2" });

            boardSelector.SelectionChanged += (s, e) =>
            {
                if (boardSelector.SelectedItem is ComboBoxItem selectedItem)
                {
                    _board = selectedItem.Content?.ToString()?.ToLower() ?? string.Empty;
                    _platform = selectedItem.Tag?.ToString() ?? string.Empty;

                    if (!string.IsNullOrEmpty(_board))
                    {
                        var disabledFlags = _validationService.DisableIncompatibleFlags(_board, AllBuildFlags);

                        if (disabledFlags.Any())
                        {
                            var flagsList = string.Join("\n", disabledFlags.Select(f => $"• {f}"));
                            var message = LocalizationManager.GetFormat("PlatformCompatibility", _board, flagsList);

                            MessageBox.Show(
                                message,
                                LocalizationManager.Get("PlatformCompatibilityTitle"),
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                    }
                }
            };

            var flashSizeLabel = new TextBlock(new Run(LocalizationManager.Get("Flash"))) 
            { 
                FontWeight = FontWeights.SemiBold, 
                Margin = new Thickness(4, 0, 4, 0), 
                VerticalAlignment = VerticalAlignment.Center 
            };
            flashSizeSelector = new ComboBox { Width = 80, Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = LocalizationManager.Get("None"), Tag = "None", IsSelected = true });
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = "4MB", Tag = "4MB" });
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = "8MB", Tag = "8MB" });
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = "16MB", Tag = "16MB" });
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = "32MB", Tag = "32MB" });
            flashSizeSelector.SelectionChanged += (s, e) =>
            {
                if (flashSizeSelector.SelectedItem is ComboBoxItem ci)
                {
                    _flashSize = (ci.Tag as string) ?? (ci.Content as string) ?? string.Empty;
                }
            };

            var languageLabel = new TextBlock(new Run(LocalizationManager.Get("Language")))
            {
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(12, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var currentLanguageCode = LocalizationManager.CurrentLanguage;

            languageSelector = new ComboBox
            {
                Width = 100,
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = LocalizationManager.Get("LanguageTooltip")
            };

            var languages = LocalizationManager.GetAvailableLanguages();
            languageSelector.ItemsSource = languages;

            var selectedLanguage = languages.FirstOrDefault(l => l.Code == currentLanguageCode);
            if (selectedLanguage != null)
            {
                languageSelector.SelectedItem = selectedLanguage;
            }
            else
            {
                languageSelector.SelectedIndex = 0;
            }

            bool isChangingLanguage = false;

            languageSelector.SelectionChanged += async (s, e) =>
            {
                if (isChangingLanguage) return;

                if (languageSelector.SelectedItem is LanguageOption option)
                {
                    if (option.Code == currentLanguageCode) return;

                    _logger.Information("Changing language to: {Language}", option.Code);

                    isChangingLanguage = true;
                    try
                    {
                        LocalizationManager.SetLanguage(option.Code);
                        BuildFlowDocument();

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

            // Help button - dock to right (added first so it appears rightmost)
            var helpButton = new Button
            {
                Content = LocalizationManager.Get("ViewHelp"),
                Width = 100,
                Height = 28,
                Margin = new Thickness(4, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = LocalizationManager.Get("ViewHelpTooltip")
            };
            helpButton.Click += ViewHelp_Click;

            // Changelog button - dock to right (added second so it appears to the left of help button)
            var changelogButton = new Button
            {
                Content = LocalizationManager.Get("ViewChangelog"),
                Width = 120,
                Height = 28,
                Margin = new Thickness(4, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = LocalizationManager.Get("ViewChangelogTooltip")
            };
            changelogButton.Click += ViewChangelog_Click;

            // Check for Updates button - dock to right (added third so it appears to the left of changelog button)
            var checkUpdatesButton = new Button
            {
                Content = LocalizationManager.Get("CheckForUpdates"),
                Width = 150,
                Height = 28,
                Margin = new Thickness(4, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = LocalizationManager.Get("CheckForUpdatesTooltip")
            };
            checkUpdatesButton.Click += CheckForUpdates_Click;

            devicePanel.Children.Add(portLabel);
            devicePanel.Children.Add(comPortSelector);
            devicePanel.Children.Add(boardLabel);
            devicePanel.Children.Add(boardSelector);
            devicePanel.Children.Add(flashSizeLabel);
            devicePanel.Children.Add(flashSizeSelector);
            devicePanel.Children.Add(languageLabel);
            devicePanel.Children.Add(languageSelector);
            
            // Dock buttons to the right
            DockPanel.SetDock(helpButton, Dock.Right);
            devicePanel.Children.Add(helpButton);
            DockPanel.SetDock(changelogButton, Dock.Right);
            devicePanel.Children.Add(changelogButton);
            DockPanel.SetDock(checkUpdatesButton, Dock.Right);
            devicePanel.Children.Add(checkUpdatesButton);

            // ===== BUTTONS PANEL (Row 1) - Action Buttons and Checkboxes =====

            checkDeviceButton = new Button 
            { 
                Content = LocalizationManager.Get("CheckDevice"), 
                Width = 140, 
                Height = 28, 
                Margin = new Thickness(4, 0, 0, 0) 
            };
            checkDeviceButton.Click += CheckConnectedDevice_Click;

            updateGGButton = new Button
            {
                Content = LocalizationManager.Get("UpdateGuiGeneric"),
                Width = 160,
                Height = 28,
                Margin = new Thickness(4, 0, 0, 0)
            };
            updateGGButton.Click += UpdateGG_Click;

            var loadConfigButton = new Button
            {
                Content = LocalizationManager.Get("ManageConfigs"),
                Width = 160,
                Height = 28,
                Margin = new Thickness(4, 0, 0, 0)
            };
            loadConfigButton.Click += LoadConfig_Click;

            eraseFlashCheckBox = new CheckBox
            {
                Content = LocalizationManager.Get("EraseFlash"),
                IsChecked = false,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 4, 0),
                FontWeight = FontWeights.SemiBold,
                ToolTip = LocalizationManager.Get("EraseFlashTooltip")
            };

            backupCheckBox = new CheckBox
            {
                Content = LocalizationManager.Get("Backup"),
                IsChecked = true,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 4, 0),
                FontWeight = FontWeights.SemiBold,
                ToolTip = LocalizationManager.Get("BackupTooltip")
            };

            deployCheckBox = new CheckBox
            {
                Content = LocalizationManager.Get("Deploy"),
                IsChecked = true,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 4, 0),
                FontWeight = FontWeights.SemiBold
            };

            compileButton = new Button
            {
                Content = LocalizationManager.Get("Compile"),
                Width = 140,
                Height = 28,
                Margin = new Thickness(4, 0, 0, 0)
            };
            compileButton.Click += CompileSelected_Click;

            DockPanel.SetDock(compileButton, Dock.Right);
            buttonsPanel.Children.Add(compileButton);
            DockPanel.SetDock(deployCheckBox, Dock.Right);
            buttonsPanel.Children.Add(deployCheckBox);
            DockPanel.SetDock(backupCheckBox, Dock.Right);
            buttonsPanel.Children.Add(backupCheckBox);
            DockPanel.SetDock(eraseFlashCheckBox, Dock.Right);
            buttonsPanel.Children.Add(eraseFlashCheckBox);

            buttonsPanel.Children.Add(checkDeviceButton);
            DockPanel.SetDock(checkDeviceButton, Dock.Right);

            buttonsPanel.Children.Add(updateGGButton);
            DockPanel.SetDock(updateGGButton, Dock.Right);

            buttonsPanel.Children.Add(loadConfigButton);
            DockPanel.SetDock(loadConfigButton, Dock.Right);

            // Status text
            statusText = new TextBlock
            {
                Text = string.Empty,
                Margin = new Thickness(0, 4, 0, 12),
                HorizontalAlignment = HorizontalAlignment.Left,
                FontWeight = FontWeights.Normal,
                FontSize = 13,
                Foreground = System.Windows.Media.Brushes.DarkBlue,
                Visibility = Visibility.Collapsed,
                TextWrapping = TextWrapping.Wrap
            };

            flagsContainer.Children.Add(statusText);

            var grouped = AllBuildFlags.GroupBy(f => f.Section).ToList();

            foreach (var group in grouped)
            {
                string sectionDisplayName = group.Key;
                if (_builderConfig?.Sections != null && _builderConfig.Sections.TryGetValue(group.Key, out var sectionInfo))
                {
                    var currentLang = LocalizationManager.CurrentLanguage;
                    if (sectionInfo.Translations != null && sectionInfo.Translations.TryGetValue(currentLang, out var translatedName) && !string.IsNullOrEmpty(translatedName))
                    {
                        sectionDisplayName = translatedName;
                    }
                }

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
                UpdateGroupCheckBoxState(groupCheckBox, group);

                groupCheckBox.Checked += (s, e) => SetGroupFlags(group, true, groupCheckBox);
                groupCheckBox.Unchecked += (s, e) => SetGroupFlags(group, false, groupCheckBox);

                var titleText = new TextBlock(new Run(sectionDisplayName + $" ({group.Count()})")) 
                { 
                    FontWeight = FontWeights.Bold, 
                    FontSize = 14, 
                    VerticalAlignment = VerticalAlignment.Center, 
                    Margin = new Thickness(8, 0, 0, 0) 
                };

                headerPanel.Children.Add(groupCheckBox);
                headerPanel.Children.Add(titleText);

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

                var grid = _uiBuilderService.CreateFlagsGrid();

                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                _uiBuilderService.AddText(grid, 0, 0, LocalizationManager.Get("Enabled"), FontWeights.SemiBold);
                _uiBuilderService.AddText(grid, 0, 1, LocalizationManager.Get("Key"), FontWeights.SemiBold);
                _uiBuilderService.AddText(grid, 0, 2, LocalizationManager.Get("Name"), FontWeights.SemiBold);
                _uiBuilderService.AddText(grid, 0, 3, LocalizationManager.Get("Description"), FontWeights.SemiBold);
                _uiBuilderService.AddText(grid, 0, 4, LocalizationManager.Get("Parameters"), FontWeights.SemiBold);

                int r = 1;
                foreach (var item in group.OrderBy(i => i.SectionOrder).ThenBy(x => x.Key))
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    var chk = new CheckBox { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2) };
                    chk.SetBinding(CheckBox.IsCheckedProperty, new Binding(nameof(BuildFlagItem.IsEnabled)) { Source = item, Mode = BindingMode.TwoWay });

                    chk.Checked += (s, e) =>
                    {
                        var errorMessage = DependencyResolver.ProcessFlagEnabled(item, AllBuildFlags);
                        if (errorMessage != null && !item.IsEnabled)
                        {
                            MessageBox.Show(
                                errorMessage,
                                LocalizationManager.Get("BlockingDependencies"),
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }

                        if (item.IsEnabled && !string.IsNullOrEmpty(_board))
                        {
                            if (item.DisabledOnPlatforms != null &&
                                item.DisabledOnPlatforms.Any(p => string.Equals(p, _board, StringComparison.OrdinalIgnoreCase)))
                            {
                                item.IsEnabled = false;
                                MessageBox.Show(
                                    LocalizationManager.GetFormat("PlatformIncompatibility", item.GetLocalizedName(), _board, string.Join(", ", item.DisabledOnPlatforms)),
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

                    _uiBuilderService.AddText(grid, r, 1, item.Key ?? string.Empty);
                    _uiBuilderService.AddText(grid, r, 2, item.GetLocalizedName());
                    _uiBuilderService.AddText(grid, r, 3, item.GetLocalizedDescription(), enableTextWrapping: true);

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

                flagsContainer.Children.Add(border);
            }
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

        private async void Z2SDetectPort_Click(object sender, RoutedEventArgs e)
        {
            z2sDetectPortButton.IsEnabled = false;
            z2sStatusText.Text = "Wykrywanie urządzenia…";

            try
            {
                var (port, deviceInfo) = await _deviceManagementService.DetectDeviceAsync();

                if (!string.IsNullOrWhiteSpace(port))
                {
                    var match = _deviceManagementService.FindComboBoxItemByTag(z2sComPortSelector, port);
                    if (match != null)
                        z2sComPortSelector.SelectedItem = match;

                    _z2sChip = deviceInfo?.ChipType?.ToLowerInvariant() ?? string.Empty;
                    z2sChipLabel.Text = string.IsNullOrEmpty(_z2sChip) ? string.Empty : $"Chip: {_z2sChip.ToUpperInvariant()}";

                    // Auto-select detected flash size in the dropdown
                    var detectedFlash = deviceInfo?.FlashSize ?? string.Empty;
                    _z2sFlashSize = detectedFlash;
                    var flashMatch = z2sFlashSizeSelector.Items
                        .OfType<ComboBoxItem>()
                        .FirstOrDefault(ci => string.Equals(ci.Tag as string, detectedFlash, StringComparison.OrdinalIgnoreCase));
                    z2sFlashSizeSelector.SelectedItem = flashMatch ?? z2sFlashSizeSelector.Items[0];

                    z2sStatusText.Text = $"Wykryto port: {port}" +
                        (string.IsNullOrEmpty(_z2sChip) ? string.Empty : $", chip: {_z2sChip.ToUpperInvariant()}") +
                        (string.IsNullOrEmpty(detectedFlash) ? string.Empty : $", flash: {detectedFlash}") +
                        ".\nNaciśnij przycisk aby sprawdzić wersję.";
                }
                else
                {
                    _z2sChip = string.Empty;
                    _z2sFlashSize = string.Empty;
                    z2sChipLabel.Text = string.Empty;
                    z2sFlashSizeSelector.SelectedIndex = 0;
                    z2sStatusText.Text = "Nie wykryto urządzenia. Sprawdź połączenie USB i spróbuj ponownie.";
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Z2SDetectPort_Click failed");
                z2sStatusText.Text = $"Błąd wykrywania: {ex.Message}";
            }
            finally
            {
                z2sDetectPortButton.IsEnabled = true;
            }
        }

        private async void CheckZ2SVersion_Click(object sender, RoutedEventArgs e)
        {
            var z2sPort = (z2sComPortSelector.SelectedItem as ComboBoxItem)?.Tag as string;
            if (string.IsNullOrEmpty(z2sPort) || z2sPort == "None")
            {
                z2sStatusText.Text = "Błąd: Wybierz port COM przed sprawdzeniem.";
                return;
            }

            z2sCheckVersionButton.IsEnabled = false;
            z2sStatusText.Text = $"Łączenie z urządzeniem na {z2sPort}…";

            try
            {
                var result = await _z2sUpdateService.CheckVersionAsync(z2sPort);

                _z2sDeviceVersion = result.DeviceVersion;

                if (result.DeviceVersion == null && result.Error != null)
                {
                    z2sStatusText.Text = $"Nie udało się odczytać wersji z urządzenia.\n{result.Error}";
                }
                else if (result.DeviceVersion == null)
                {
                    z2sStatusText.Text = "Nie znaleziono pliku version.dat w SPIFFS.\nSprawdź czy urządzenie jest podłączone i ma firmware Z2S.";
                }
                else if (result.IsUpdateAvailable)
                {
                    z2sStatusText.Text = $"✔ Dostępna aktualizacja!\n\nWersja na urządzeniu:  {result.DeviceVersion}\nNajnowsza wersja:       {result.LatestVersion ?? "nieznana"}\n\nhttps://github.com/lsroka76/Z2S_Library";
                }
                else
                {
                    z2sStatusText.Text = $"✔ Firmware jest aktualny.\n\nWersja na urządzeniu:  {result.DeviceVersion}\nNajnowsza wersja:       {result.LatestVersion ?? "nieznana"}";
                }
            }
            catch (OperationCanceledException)
            {
                z2sStatusText.Text = "Operacja anulowana.";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "CheckZ2SVersion_Click failed");
                z2sStatusText.Text = $"Błąd: {ex.Message}";
            }
            finally
            {
                z2sCheckVersionButton.IsEnabled = true;
            }
        }

        private void Z2SSetButtonsEnabled(bool enabled)
        {
            z2sCheckVersionButton.IsEnabled = enabled;
            z2sBackupButton.IsEnabled = enabled;
            z2sRestoreButton.IsEnabled = enabled;
            z2sUpgradeButton.IsEnabled = enabled;
            z2sDetectPortButton.IsEnabled = enabled;
        }

        private void Z2SBrowseBackupDir_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Wybierz folder docelowy dla backupu Z2S",
                InitialDirectory = z2sBackupDirTextBox.Text
            };

            if (dialog.ShowDialog() == true)
                z2sBackupDirTextBox.Text = dialog.FolderName;
        }

        private async void Z2SBackup_Click(object sender, RoutedEventArgs e)
        {
            var z2sPort = (z2sComPortSelector.SelectedItem as ComboBoxItem)?.Tag as string;
            if (string.IsNullOrEmpty(z2sPort) || z2sPort == "None")
            {
                z2sStatusText.Text = "Błąd: Wybierz port COM przed wykonaniem backupu.";
                return;
            }

            if (string.IsNullOrEmpty(_z2sChip))
            {
                z2sStatusText.Text = "Błąd: Najpierw wykryj urządzenie przyciskiem 'Wykryj port'.";
                return;
            }

            var backupDir = z2sBackupDirTextBox?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(backupDir))
            {
                z2sStatusText.Text = "Błąd: Wybierz katalog docelowy backupu przed wykonaniem operacji.";
                return;
            }

            CompilationResultsWindow backupLogWindow = null;
            EventHandler<string> backupOutputHandler = null;
            EventHandler<string> backupErrorHandler = null;
            bool backupLogWindowFinalized = false;

            Z2SSetButtonsEnabled(false);
            _z2sCancellation = new CancellationTokenSource();

            try
            {
                backupLogWindow = await ShowZ2SOperationLogWindowAsync("Z2S backup w toku");
                AttachZ2SLogStreaming(backupLogWindow, out backupOutputHandler, out backupErrorHandler);
                backupLogWindow.AppendLog($"Backup started on port {z2sPort}.");

                z2sOperationProgressBar.Visibility = Visibility.Visible;
                z2sStatusText.Text = $"Tworzenie backupu na porcie {z2sPort}…\n(może potrwać kilka minut)";

                var result = await _z2sUpdateService.BackupAsync(
                    z2sPort,
                    _z2sChip,
                    backupDir,
                    _z2sFlashSize,
                    _z2sCancellation.Token,
                    msg => Dispatcher.Invoke(() =>
                    {
                        z2sStatusText.Text = msg;
                        backupLogWindow?.AppendLog(msg);
                    }));

                if (result.Success)
                {
                    _logger.Information("Z2S backup succeeded: {File}", result.BackupFilePath);
                    z2sStatusText.Text = $"✔ Backup zakończony pomyślnie.\n\nPlik: {result.BackupFilePath}";
                    z2sStatusBorder.Background = System.Windows.Media.Brushes.LightGreen;
                    backupLogWindow?.AppendLog($"Backup saved to {result.BackupFilePath}");
                    backupLogWindow?.FinalizeCompilation(true, customTitle: "Z2S backup zakończony");
                    backupLogWindowFinalized = true;
                    await Task.Delay(4000);
                    z2sStatusBorder.Background = System.Windows.Media.Brushes.Transparent;
                }
                else
                {
                    z2sStatusText.Text = $"✘ Backup nieudany.\n{result.Error}";
                    z2sStatusBorder.Background = System.Windows.Media.Brushes.MistyRose;
                    backupLogWindow?.AppendLog($"Backup failed: {result.Error}");
                    backupLogWindow?.FinalizeCompilation(false, customTitle: "Z2S backup nieudany");
                    backupLogWindowFinalized = true;
                    await Task.Delay(4000);
                    z2sStatusBorder.Background = System.Windows.Media.Brushes.Transparent;
                }
            }
            catch (OperationCanceledException)
            {
                z2sStatusText.Text = "Backup anulowany.";
                backupLogWindow?.AppendLog("Backup cancelled.");
                backupLogWindow?.FinalizeCompilation(false, customTitle: "Z2S backup anulowany");
                backupLogWindowFinalized = true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Z2SBackup_Click failed");
                z2sStatusText.Text = $"Błąd backupu: {ex.Message}";
                backupLogWindow?.AppendLog($"Backup error: {ex.Message}");
                backupLogWindow?.FinalizeCompilation(false, customTitle: "Z2S backup błąd");
                backupLogWindowFinalized = true;
            }
            finally
            {
                if (backupLogWindow != null)
                {
                    DetachZ2SLogStreaming(backupOutputHandler, backupErrorHandler);
                    if (!backupLogWindowFinalized)
                        backupLogWindow.FinalizeCompilation(false, customTitle: "Z2S backup zakończony");
                }

                _z2sCancellation?.Dispose();
                _z2sCancellation = null;
                z2sOperationProgressBar.Visibility = Visibility.Collapsed;
                Z2SSetButtonsEnabled(true);
            }
        }

        private async void Z2SUpgrade_Click(object sender, RoutedEventArgs e)
        {
            var z2sPort = (z2sComPortSelector.SelectedItem as ComboBoxItem)?.Tag as string;
            if (string.IsNullOrEmpty(z2sPort) || z2sPort == "None")
            {
                z2sStatusText.Text = "Błąd: Wybierz port COM przed aktualizacją.";
                return;
            }

            if (string.IsNullOrEmpty(_z2sChip))
            {
                z2sStatusText.Text = "Błąd: Najpierw wykryj urządzenie przyciskiem 'Wykryj port'.";
                return;
            }

            bool withLogs = z2sWithLogsCheckBox.IsChecked ?? true;
            bool fullVersion = z2sFullVersionCheckBox.IsChecked ?? false;
            bool clearDevice = z2sClearDeviceCheckBox.IsChecked ?? false;
            bool backupBeforeFlash = z2sBackupBeforeFlashCheckBox.IsChecked ?? true;
            var backupDir = z2sBackupDirTextBox?.Text?.Trim();

            if (backupBeforeFlash && string.IsNullOrWhiteSpace(backupDir))
            {
                z2sStatusText.Text = "Błąd: włączony backup przed aktualizacją wymaga ustawienia katalogu backupu.";
                return;
            }

            // Show version picker — let the user choose from the last N releases
            var picker = new Z2SVersionPickerWindow(_z2sUpdateService, _z2sDeviceVersion, _logger, _z2sVersionHistoryCount)
            {
                Owner = this
            };

            if (picker.ShowDialog() != true || picker.SelectedRelease == null)
                return;

            var selectedRelease = picker.SelectedRelease;
            string firmwareFileName = Z2SUpdateService.GetFirmwareFileName(withLogs, fullVersion, _z2sFlashSize);

            var confirmMessage = $"Czy na pewno wgrać firmware Z2S {selectedRelease.TagName} na urządzeniu podłączonym do {z2sPort}?\n\nPlik: {firmwareFileName}";

            if (!picker.IsDowngrade && _z2sDeviceVersion != null && selectedRelease.TagName != _z2sDeviceVersion)
                confirmMessage += "\n\n\u26a0 Uwaga: wgrywanie nowszej wersji mo\u017ce spowodowa\u0107 problemy z uruchomieniem urz\u0105dzenia. W razie problem\u00f3w u\u017cyj opcji 'Przywr\u00f3\u0107 z backupu'.";

            if (clearDevice)
                confirmMessage += "\n\n⚠ Cała pamięć flash zostanie wyczyszczona przed wgraniem firmware!";

            if (backupBeforeFlash)
                confirmMessage += "\n\n✔ Backup zostanie wykonany automatycznie przed wgraniem firmware.";

            var confirm = MessageBox.Show(
                confirmMessage,
                "Potwierdzenie aktualizacji",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            CompilationResultsWindow upgradeLogWindow = null;
            EventHandler<string> upgradeOutputHandler = null;
            EventHandler<string> upgradeErrorHandler = null;
            bool upgradeLogWindowFinalized = false;

            Z2SSetButtonsEnabled(false);
            _z2sCancellation = new CancellationTokenSource();

            try
            {
                upgradeLogWindow = await ShowZ2SOperationLogWindowAsync("Z2S aktualizacja w toku");
                AttachZ2SLogStreaming(upgradeLogWindow, out upgradeOutputHandler, out upgradeErrorHandler);
                upgradeLogWindow.AppendLog($"Flash started on port {z2sPort}.");

                // Auto-backup before flashing if requested
                if (backupBeforeFlash)
                {
                    z2sOperationProgressBar.Visibility = Visibility.Visible;
                    z2sStatusText.Text = $"Tworzenie backupu przed aktualizacją na porcie {z2sPort}…\n(może potrwać kilka minut)";
                    upgradeLogWindow.AppendLog("Starting pre-flash backup...");
                    var backupResult = await _z2sUpdateService.BackupAsync(
                        z2sPort,
                        _z2sChip,
                        backupDir,
                        _z2sFlashSize,
                        _z2sCancellation.Token,
                        msg => Dispatcher.Invoke(() =>
                        {
                            z2sStatusText.Text = msg;
                            upgradeLogWindow?.AppendLog(msg);
                        }));

                    if (!backupResult.Success)
                    {
                        upgradeLogWindow.AppendLog($"Pre-flash backup failed: {backupResult.Error}");
                        var continueAnyway = MessageBox.Show(
                            $"Backup nie powiódł się: {backupResult.Error}\n\nCzy chcesz kontynuować aktualizację mimo niepowodzenia backupu?",
                            "Błąd backupu",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (continueAnyway != MessageBoxResult.Yes)
                        {
                            z2sStatusText.Text = "Aktualizacja anulowana — backup nie powiódł się.";
                            upgradeLogWindow.FinalizeCompilation(false, customTitle: "Z2S aktualizacja anulowana");
                            upgradeLogWindowFinalized = true;
                            return;
                        }
                    }
                    else
                    {
                        z2sStatusText.Text = $"✔ Backup zakończony: {backupResult.BackupFilePath}\nRozpoczynanie aktualizacji…";
                        upgradeLogWindow.AppendLog($"Pre-flash backup saved to {backupResult.BackupFilePath}");
                    }
                }

                z2sStatusText.Text = "Przygotowywanie aktualizacji…";
                upgradeLogWindow.AppendLog("Preparing firmware download...");

                var result = await _z2sUpdateService.DownloadAndFlashAsync(
                    z2sPort,
                    _z2sChip,
                    withLogs,
                    fullVersion,
                    clearDevice,
                    _z2sFlashSize,
                    selectedRelease,
                    msg => Dispatcher.Invoke(() =>
                    {
                        z2sStatusText.Text = msg;
                        upgradeLogWindow?.AppendLog(msg);
                    }),
                    _z2sCancellation.Token);

                if (result.Success)
                {
                    _logger.Information("Z2S upgrade succeeded, version {Version}", result.FlashedVersion);
                    z2sStatusText.Text = $"✔ Aktualizacja zakończona pomyślnie!\n\nWgrana wersja: {result.FlashedVersion}";
                    z2sStatusBorder.Background = System.Windows.Media.Brushes.LightGreen;
                    upgradeLogWindow?.AppendLog($"Upgrade completed successfully: {result.FlashedVersion}");
                    upgradeLogWindow?.FinalizeCompilation(true, customTitle: "Z2S aktualizacja zakończona");
                    upgradeLogWindowFinalized = true;
                    await Task.Delay(4000);
                    z2sStatusBorder.Background = System.Windows.Media.Brushes.Transparent;
                }
                else
                {
                    z2sStatusText.Text = $"✘ Aktualizacja nieudana.\n{result.Error}";
                    z2sStatusBorder.Background = System.Windows.Media.Brushes.MistyRose;
                    upgradeLogWindow?.AppendLog($"Upgrade failed: {result.Error}");
                    upgradeLogWindow?.FinalizeCompilation(false, customTitle: "Z2S aktualizacja nieudana");
                    upgradeLogWindowFinalized = true;
                    await Task.Delay(4000);
                    z2sStatusBorder.Background = System.Windows.Media.Brushes.Transparent;
                }
            }
            catch (OperationCanceledException)
            {
                z2sStatusText.Text = "Aktualizacja anulowana.";
                upgradeLogWindow?.AppendLog("Upgrade cancelled.");
                upgradeLogWindow?.FinalizeCompilation(false, customTitle: "Z2S aktualizacja anulowana");
                upgradeLogWindowFinalized = true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Z2SUpgrade_Click failed");
                z2sStatusText.Text = $"Błąd aktualizacji: {ex.Message}";
                upgradeLogWindow?.AppendLog($"Upgrade error: {ex.Message}");
                upgradeLogWindow?.FinalizeCompilation(false, customTitle: "Z2S aktualizacja błąd");
                upgradeLogWindowFinalized = true;
            }
            finally
            {
                if (upgradeLogWindow != null)
                {
                    DetachZ2SLogStreaming(upgradeOutputHandler, upgradeErrorHandler);
                    if (!upgradeLogWindowFinalized)
                        upgradeLogWindow.FinalizeCompilation(false, customTitle: "Z2S aktualizacja zakończona");
                }

                _z2sCancellation?.Dispose();
                _z2sCancellation = null;
                z2sOperationProgressBar.Visibility = Visibility.Collapsed;
                Z2SSetButtonsEnabled(true);
            }
        }

        private async void Z2SRestore_Click(object sender, RoutedEventArgs e)
        {
            var z2sPort = (z2sComPortSelector.SelectedItem as ComboBoxItem)?.Tag as string;
            if (string.IsNullOrEmpty(z2sPort) || z2sPort == "None")
            {
                z2sStatusText.Text = "Błąd: Wybierz port COM przed przywracaniem backupu.";
                return;
            }

            if (string.IsNullOrEmpty(_z2sChip))
            {
                z2sStatusText.Text = "Błąd: Najpierw wykryj urządzenie przyciskiem 'Wykryj port'.";
                return;
            }

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Wybierz plik backupu Z2S do przywrócenia",
                Filter = "Pliki binarne (*.bin)|*.bin|Wszystkie pliki (*.*)|*.*",
                InitialDirectory = string.IsNullOrWhiteSpace(z2sBackupDirTextBox?.Text)
                    ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups", "z2s")
                    : z2sBackupDirTextBox.Text
            };

            if (dialog.ShowDialog() != true)
                return;

            var backupFile = dialog.FileName;

            var confirm = MessageBox.Show(
                $"Czy na pewno chcesz przywrócić backup na urządzeniu podłączonym do {z2sPort}?\n\nPlik: {backupFile}\n\n⚠ Obecna zawartość flash zostanie nadpisana!",
                "Potwierdzenie przywracania backupu",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            CompilationResultsWindow restoreLogWindow = null;
            EventHandler<string> restoreOutputHandler = null;
            EventHandler<string> restoreErrorHandler = null;
            bool restoreLogWindowFinalized = false;

            Z2SSetButtonsEnabled(false);
            _z2sCancellation = new CancellationTokenSource();

            try
            {
                restoreLogWindow = await ShowZ2SOperationLogWindowAsync("Z2S przywracanie w toku");
                AttachZ2SLogStreaming(restoreLogWindow, out restoreOutputHandler, out restoreErrorHandler);
                restoreLogWindow.AppendLog($"Restore started from {backupFile}.");

                z2sStatusText.Text = $"Przywracanie backupu z pliku:\n{backupFile}\n(może potrwać kilka minut)";

                var result = await _z2sUpdateService.RestoreFromBackupAsync(
                    z2sPort,
                    _z2sChip,
                    backupFile,
                    msg => Dispatcher.Invoke(() =>
                    {
                        z2sStatusText.Text = msg;
                        restoreLogWindow?.AppendLog(msg);
                    }),
                    _z2sCancellation.Token);

                if (result.Success)
                {
                    _logger.Information("Z2S restore succeeded from {File}", backupFile);
                    z2sStatusText.Text = $"✔ Przywracanie zakończone pomyślnie!\n\nPlik: {backupFile}";
                    z2sStatusBorder.Background = System.Windows.Media.Brushes.LightGreen;
                    restoreLogWindow?.AppendLog($"Restore completed successfully from {backupFile}");
                    restoreLogWindow?.FinalizeCompilation(true, customTitle: "Z2S przywracanie zakończone");
                    restoreLogWindowFinalized = true;
                    await Task.Delay(4000);
                    z2sStatusBorder.Background = System.Windows.Media.Brushes.Transparent;
                }
                else
                {
                    z2sStatusText.Text = $"✘ Przywracanie nieudane.\n{result.Error}";
                    z2sStatusBorder.Background = System.Windows.Media.Brushes.MistyRose;
                    restoreLogWindow?.AppendLog($"Restore failed: {result.Error}");
                    restoreLogWindow?.FinalizeCompilation(false, customTitle: "Z2S przywracanie nieudane");
                    restoreLogWindowFinalized = true;
                    await Task.Delay(4000);
                    z2sStatusBorder.Background = System.Windows.Media.Brushes.Transparent;
                }
            }
            catch (OperationCanceledException)
            {
                z2sStatusText.Text = "Przywracanie anulowane.";
                restoreLogWindow?.AppendLog("Restore cancelled.");
                restoreLogWindow?.FinalizeCompilation(false, customTitle: "Z2S przywracanie anulowane");
                restoreLogWindowFinalized = true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Z2SRestore_Click failed");
                z2sStatusText.Text = $"Błąd przywracania: {ex.Message}";
                restoreLogWindow?.AppendLog($"Restore error: {ex.Message}");
                restoreLogWindow?.FinalizeCompilation(false, customTitle: "Z2S przywracanie błąd");
                restoreLogWindowFinalized = true;
            }
            finally
            {
                if (restoreLogWindow != null)
                {
                    DetachZ2SLogStreaming(restoreOutputHandler, restoreErrorHandler);
                    if (!restoreLogWindowFinalized)
                        restoreLogWindow.FinalizeCompilation(false, customTitle: "Z2S przywracanie zakończone");
                }

                _z2sCancellation?.Dispose();
                _z2sCancellation = null;
                z2sOperationProgressBar.Visibility = Visibility.Collapsed;
                Z2SSetButtonsEnabled(true);
            }
        }

        private async void UpdateGG_Click(object sender, RoutedEventArgs e)
        {
            // Disable all buttons and show status
            updateGGButton.IsEnabled = false;
            compileButton.IsEnabled = false;
            checkDeviceButton.IsEnabled = false;
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
                // Re-enable all buttons
                updateGGButton.IsEnabled = true;
                compileButton.IsEnabled = true;
                checkDeviceButton.IsEnabled = true;
            }
        }

        private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Information("User initiated update check");

                statusText.Text = LocalizationManager.Get("CheckingForUpdates");
                statusText.Visibility = Visibility.Visible;
                statusText.Foreground = System.Windows.Media.Brushes.DarkBlue;

                var (updateAvailable, latestRelease) = await _autoUpdateService.CheckForUpdatesAsync();

                statusText.Visibility = Visibility.Collapsed;

                if (updateAvailable && latestRelease != null)
                {
                    _logger.Information("Update available: {Version}", latestRelease.TagName);

                    var updateWindow = new UpdateWindow(_autoUpdateService, latestRelease, _logger)
                    {
                        Owner = this
                    };
                    updateWindow.ShowDialog();
                }
                else
                {
                    _logger.Information("Application is up to date");
                    MessageBox.Show(
                        LocalizationManager.GetFormat("AppUpToDate", _autoUpdateService.GetCurrentVersion()),
                        LocalizationManager.Get("UpToDateTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("rate limit"))
            {
                _logger.Warning(ex, "GitHub API rate limit exceeded");
                statusText.Visibility = Visibility.Collapsed;
                MessageBox.Show(
                    LocalizationManager.Get("RateLimitExceeded"),
                    LocalizationManager.Get("RateLimitTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error checking for updates");
                statusText.Visibility = Visibility.Collapsed;
                MessageBox.Show(
                    LocalizationManager.GetFormat("UpdateCheckFailed", ex.Message),
                    LocalizationManager.Get("UpdateCheckFailedTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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

            // Validate platform is selected
            if (string.IsNullOrEmpty(_board) || _board.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    LocalizationManager.Get("PlatformRequired"),
                    LocalizationManager.Get("PlatformRequiredTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Validate platform compatibility
            var incompatibleFlags = _validationService.ValidatePlatformCompatibility(_board, selectedFlags);
            if (incompatibleFlags.Any())
            {
                var flagsList = string.Join("\n", incompatibleFlags.Select(f => $"• {f}"));
                var message = LocalizationManager.GetFormat("PlatformCompatibilityError", _board, flagsList);

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
            var selectedFlashSize = _flashSize;
            if (!string.IsNullOrEmpty(selectedFlashSize) && !selectedFlashSize.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                // Validate flash size is compatible with platform
                if (!PartitionManager.ValidateFlashSize(_board, selectedFlashSize))
                {
                    var supportedSizes = PartitionManager.GetSupportedFlashSizes(_board);
                    var sizesList = string.Join(", ", supportedSizes);

                    MessageBox.Show(
                        $"Flash size {selectedFlashSize} is not supported for {_board}.\n\n" +
                        $"Supported flash sizes: {sizesList}",
                        "Incompatible Flash Size",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            // Get deploy and backup checkbox states
            bool shouldDeploy = deployCheckBox?.IsChecked ?? true;
            bool shouldBackup = backupCheckBox?.IsChecked ?? true;
            bool shouldEraseFlash = eraseFlashCheckBox?.IsChecked ?? false;

            // Validate COM port selection only if deploying
            if (shouldDeploy)
            {
                if (string.IsNullOrEmpty(_portCom) || _portCom.Equals("None", StringComparison.OrdinalIgnoreCase))
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
            
            // Disable other buttons during compilation
            checkDeviceButton.IsEnabled = false;
            updateGGButton.IsEnabled = false;

            // Show status indicator
            statusText.Text = LocalizationManager.GetFormat("CompilingFirmware", 0.0);
            statusText.Foreground = System.Windows.Media.Brushes.Black;
            statusText.FontStyle = FontStyles.Oblique;
            statusText.Visibility = Visibility.Visible;

            // Create the results window outside try block so it's accessible in catch blocks
            CompilationResultsWindow resultsWindow = null;

            try
            {
                // Generate timestamp once for both backup and configuration files to ensure consistency
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                
                // Create the results window for live log streaming
                await Dispatcher.InvokeAsync(() =>
                {
                    resultsWindow = new CompilationResultsWindow
                    {
                        Owner = this
                    };
                    resultsWindow.Show(); // Show as non-modal so compilation can proceed
                });
                
                var ggRequest = new CompileRequest
                {
                    BuildFlags = selectedFlags,
                    EnvironmentName = _platform,
                    Board = _board,
                    ProjectPath = Path.Combine(_repositoryPath, "src"),
                    ProjectDirectory = _repositoryPath,
                    LibrariesPath = Path.Combine(_repositoryPath, "lib"),
                    PortCom = _portCom,
                    FlashSize = _flashSize,
                    ShouldDeploy = shouldDeploy,
                    ShouldBackup = shouldBackup,
                    ShouldEraseFlash = shouldEraseFlash,
                    GlobalSettings = _builderConfig.GlobalSettings,
                    ConfigTimestamp = timestamp
                };
                
                var handler = new PlatformioCliHandler();
                
                // Subscribe to output events for live log streaming
                handler.OutputLine += (s, line) => resultsWindow?.AppendLog(line);
                handler.ErrorLine += (s, line) => resultsWindow?.AppendLog(line);
                
                var result = await handler.Handle(ggRequest, _compilationCancellation.Token);

                // Get elapsed time from results window
                var compilationTime = resultsWindow?.GetElapsedSeconds() ?? 0;

                // Check if compilation was cancelled
                if (_compilationCancellation.IsCancellationRequested)
                {
                    _logger.Information("Compilation cancelled by user");

                    statusText.Text = LocalizationManager.GetFormat("CompilationStopped", compilationTime);
                    statusText.Foreground = System.Windows.Media.Brushes.Black;
                    statusText.FontStyle = FontStyles.Oblique;
                    
                    // Finalize window with cancelled status (treat as failure)
                    resultsWindow?.FinalizeCompilation(false);

                    MessageBox.Show(
                        LocalizationManager.GetFormat("CompilationStoppedMessage", compilationTime),
                        LocalizationManager.Get("CompilationStoppedTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else if (result.IsSuccessful)
                {
                    // Success status with compilation time - KEEP IT VISIBLE
                    statusText.Text = LocalizationManager.GetFormat("CompilationSuccessful", compilationTime);
                    statusText.Foreground = System.Windows.Media.Brushes.Black;
                    statusText.FontStyle = FontStyles.Oblique;

                    var encodedConfig = BuildConfigurationHasher.EncodeOptions(selectedFlags);
                    // Use the same timestamp that was set in ggRequest for consistency
                    var configBaseName = $"Config_{ggRequest.ConfigTimestamp}";
                    try
                    {
                        var compiledFirmwarePath = Path.Combine(result.OutputDirectory, result.OutputFile);

                        await _configManager.SaveConfigurationAsync(selectedFlags, configName: configBaseName, board: _board, platform: _platform, comPort: _portCom, firmwareFilePath: compiledFirmwarePath, buildOutputDirectory: result.OutputDirectory, flashSize: _flashSize, repositoryPath: _repositoryPath);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to save configuration: {ex.Message}");
                    }

                    // Use the copy in configurations directory instead of the build directory
                    var configurationsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configurations");
                    var firmwareInConfigsPath = Path.Combine(configurationsDir, $"{configBaseName}.bin");

                    // Finalize the results window with success details
                    resultsWindow?.FinalizeCompilation(
                        true,
                        encodedConfig,
                        result.BackupFilePath,
                        firmwareInConfigsPath);
                }

                else
                {
                    // Error status with compilation time - KEEP IT VISIBLE
                    statusText.Text = LocalizationManager.GetFormat("CompilationFailed", compilationTime);
                    statusText.Foreground = System.Windows.Media.Brushes.Red;
                    // DO NOT hide the status - keep it visible

                    // Finalize window with failure status
                    resultsWindow?.FinalizeCompilation(false);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Information("Compilation cancelled (OperationCanceledException caught)");

                var compilationTime = resultsWindow?.GetElapsedSeconds() ?? 0;
                statusText.Text = LocalizationManager.GetFormat("CompilationStopped", compilationTime);
                statusText.Foreground = System.Windows.Media.Brushes.Orange;

                // Finalize window with cancelled status
                resultsWindow?.FinalizeCompilation(false);

                MessageBox.Show(
                    LocalizationManager.GetFormat("CompilationStoppedMessage", compilationTime),
                    LocalizationManager.Get("CompilationStoppedTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // Error status with time - KEEP IT VISIBLE
                var compilationTime = resultsWindow?.GetElapsedSeconds() ?? 0;
                statusText.Text = LocalizationManager.GetFormat("CompilationError", compilationTime);
                statusText.Foreground = System.Windows.Media.Brushes.Red;
                // DO NOT hide the status - keep it visible

                // Finalize window with error status
                resultsWindow?.FinalizeCompilation(false);

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
                
                // Re-enable other buttons
                checkDeviceButton.IsEnabled = true;
                updateGGButton.IsEnabled = true;

                // Clean up cancellation token source
                _compilationCancellation?.Dispose();
                _compilationCancellation = null;
            }

        }

        private async void CheckConnectedDevice_Click(object sender, RoutedEventArgs e)
        {
            // Disable all buttons and show status
            checkDeviceButton.IsEnabled = false;
            compileButton.IsEnabled = false;
            updateGGButton.IsEnabled = false;
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
                        _board = deviceInfo?.ChipType ?? string.Empty;
                        _board = _board.ToLowerInvariant();

                        if (!string.IsNullOrWhiteSpace(_board) && boardSelector != null)
                        {
                            var platformTag = _deviceManagementService.GetPlatformTagFromChip(_board);

                            if (platformTag != null)
                            {
                                var match = _deviceManagementService.FindComboBoxItemByTag(boardSelector, platformTag);
                                if (match != null)
                                {
                                    _platform = platformTag;
                                    boardSelector.SelectedItem = match;
                                    _logger.Information("Board selector updated to: {BoardTag}", platformTag);

                                    // Disable incompatible flags for this platform
                                    var disabledFlags = _validationService.DisableIncompatibleFlags(_board, AllBuildFlags);

                                    // Notify user if any flags were disabled
                                    if (disabledFlags.Any())
                                    {
                                        var flagsList = string.Join("\n", disabledFlags.Select(f => $"• {f}"));
                                        var message = LocalizationManager.GetFormat("PlatformCompatibility", _board, flagsList);

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
                                var fmatch = flashSizeSelector.Items.OfType<ComboBoxItem>()
                                    .FirstOrDefault(ci => string.Equals(ci.Tag as string, fs.Trim(), StringComparison.OrdinalIgnoreCase));
                                if (fmatch != null)
                                {
                                    flashSizeSelector.SelectedItem = fmatch;
                                    _logger.Information("Flash size selector updated to: {FlashSize}", fs);
                                }
                            }
                        }

                        // Success status
                        statusText.Text = LocalizationManager.GetFormat("DeviceDetected", _board, port);
                        statusText.Foreground = System.Windows.Media.Brushes.Green;
                    }
                    else
                    {
                        _logger.Warning("Device detection completed but no port found");

                        // No device status
                        statusText.Text = LocalizationManager.Get("NoDeviceDetected");
                        statusText.Foreground = System.Windows.Media.Brushes.OrangeRed;

                        MessageBox.Show(
                            LocalizationManager.Get("NoDeviceDetectedMessage"),
                            LocalizationManager.Get("DeviceNotFound"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }

                    // Re-enable all buttons
                    checkDeviceButton.IsEnabled = true;
                    compileButton.IsEnabled = true;
                    updateGGButton.IsEnabled = true;
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
                    
                    // Re-enable all buttons
                    checkDeviceButton.IsEnabled = true;
                    compileButton.IsEnabled = true;
                    updateGGButton.IsEnabled = true;

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
                var managerWindow = new ConfigurationManagerWindow(AllBuildFlags, _platform, _portCom, _flashSize, _board, _esptoolWrapper)
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

        private void ViewChangelog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var changelogWindow = new ChangelogWindow
                {
                    Owner = this
                };
                changelogWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error opening changelog window");
                MessageBox.Show(
                    $"Error opening changelog: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ViewHelp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var helpWindow = new HelpWindow
                {
                    Owner = this
                };
                helpWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error opening help window");
                MessageBox.Show(
                    $"Error opening help: {ex.Message}",
                    "Error",
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

            // Restore flash size selection if available
            if (!string.IsNullOrEmpty(config.FlashSize) && flashSizeSelector != null)
            {
                var flashSizeItem = flashSizeSelector.Items.OfType<ComboBoxItem>()
                    .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), config.FlashSize, StringComparison.OrdinalIgnoreCase));

                if (flashSizeItem != null)
                {
                    flashSizeSelector.SelectedItem = flashSizeItem;
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