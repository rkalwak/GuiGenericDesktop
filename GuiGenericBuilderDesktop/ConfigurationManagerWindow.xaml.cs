using System.IO;
using System.Windows;
using System.Windows.Controls;
using CompilationLib;
using GuiGenericBuilderDesktop.Localization;

namespace GuiGenericBuilderDesktop
{
    /// <summary>
    /// Unified Configuration Manager Window - combines loading, encoding, and decoding
    /// </summary>
    public partial class ConfigurationManagerWindow : Window
    {
        private readonly List<SavedBuildConfiguration> _configurations;
        private readonly BuildConfigurationManager _configManager;
        private readonly List<BuildFlagItem> _allFlags;

        public SavedBuildConfiguration SelectedConfiguration { get; private set; }

        public ConfigurationManagerWindow(List<BuildFlagItem> allFlags)
        {
            InitializeComponent();
            _allFlags = allFlags ?? new List<BuildFlagItem>();
            
            // Initialize configuration manager
            var configDir = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, 
                "configurations");
            _configManager = new BuildConfigurationManager(configDir);
            
            // Load saved configurations
            _configurations = _configManager.GetAllConfigurations();
            ConfigurationsListBox.ItemsSource = _configurations;
            
            // Show empty state if no configurations
            if (!_configurations.Any())
            {
                EmptyStateText.Visibility = Visibility.Visible;
            }
            
            // Localize the window
            LocalizeWindow();
        }

        private void LocalizeWindow()
        {
            // Window title
            Title = LocalizationManager.Get("ConfigurationManager");
            
            // Find the TabControl from the visual tree
            var grid = Content as Grid;
            if (grid != null)
            {
                // Find TabControl among grid children
                TabControl tabControl = null;
                foreach (var child in grid.Children)
                {
                    if (child is TabControl tc)
                    {
                        tabControl = tc;
                        break;
                    }
                }
                
                if (tabControl != null && tabControl.Items.Count >= 4)
                {
                    // Tab 0: Saved Configurations
                    if (tabControl.Items[0] is TabItem savedTab)
                    {
                        savedTab.Header = LocalizationManager.Get("SavedConfigurations");
                        LocalizeSavedConfigurationsTab(savedTab);
                    }
                    
                    // Tab 1: Load from Encoded String
                    if (tabControl.Items[1] is TabItem loadTab)
                    {
                        loadTab.Header = LocalizationManager.Get("LoadFromEncoded");
                        LocalizeLoadFromEncodedTab(loadTab);
                    }
                    
                    // Tab 2: Save Current Configuration
                    if (tabControl.Items[2] is TabItem saveTab)
                    {
                        saveTab.Header = LocalizationManager.Get("SaveCurrentConfig");
                        LocalizeSaveCurrentConfigTab(saveTab);
                    }
                    
                    // Tab 3: Help
                    if (tabControl.Items[3] is TabItem helpTab)
                    {
                        helpTab.Header = LocalizationManager.Get("Help");
                    }
                }
            }
            
            // Update button content
            if (LoadButton != null)
            {
                LoadButton.Content = LocalizationManager.Get("Load");
            }
            
            if (DeleteButton != null)
            {
                DeleteButton.Content = LocalizationManager.Get("Delete");
            }
            
            // Update static text in XAML that we can access by name
            if (EmptyStateText != null)
            {
                EmptyStateText.Text = LocalizationManager.Get("NoConfigurationsYet");
            }
            
            if (EmptyFlagsText != null)
            {
                EmptyFlagsText.Text = LocalizationManager.Get("SelectConfiguration");
            }
            
            // Find and update buttons in Load from Encoded String tab
            if (FindName("DecodeButton") is Button decodeBtn)
            {
                decodeBtn.Content = LocalizationManager.Get("DecodeAndPreview");
            }
            
            if (FindName("LoadDecodedButton") is Button loadDecodedBtn)
            {
                loadDecodedBtn.Content = LocalizationManager.Get("LoadDecoded");
            }
            
            if (FindName("SaveDecodedButton") is Button saveDecodedBtn)
            {
                saveDecodedBtn.Content = LocalizationManager.Get("SaveDecoded");
            }
            
            // Find and update button in Save Current Configuration tab
            if (FindName("SaveCurrentButton") is Button saveCurrentBtn)
            {
                saveCurrentBtn.Content = LocalizationManager.Get("SaveCurrentConfigButton");
            }
            
            // Find Close button (in bottom border)
            LocalizeCloseButton();
        }

        private void LocalizeSavedConfigurationsTab(TabItem tab)
        {
            // Navigate to the content and find TextBlocks with hardcoded English text
            if (tab.Content is ScrollViewer sv && sv.Content is Grid contentGrid)
            {
                LocalizeTextBlocksRecursively(contentGrid);
            }
            else if (tab.Content is Grid contentGrid2)
            {
                LocalizeTextBlocksRecursively(contentGrid2);
            }
        }

        private void LocalizeLoadFromEncodedTab(TabItem tab)
        {
            if (tab.Content is ScrollViewer sv && sv.Content is StackPanel sp)
            {
                LocalizeTextBlocksRecursively(sp);
            }
        }

        private void LocalizeSaveCurrentConfigTab(TabItem tab)
        {
            if (tab.Content is ScrollViewer sv && sv.Content is StackPanel sp)
            {
                LocalizeTextBlocksRecursively(sp);
            }
        }

        private void LocalizeTextBlocksRecursively(DependencyObject parent)
        {
            int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                
                if (child is TextBlock textBlock && !string.IsNullOrEmpty(textBlock.Text))
                {
                    // Translate common hardcoded English texts
                    var text = textBlock.Text.Trim();
                    
                    // Map English text to localization keys
                    var translationMap = new Dictionary<string, string>
                    {
                        ["Saved Configurations"] = "SavedConfigurations",
                        ["Configuration Details"] = "ConfigurationDetails",
                        ["Name:"] = "ConfigName",
                        ["Platform:"] = "Platform",
                        ["COM Port:"] = "COMPort",
                        ["Saved:"] = "SavedDate",
                        ["Encoded:"] = "EncodedConfig",
                        ["Copy"] = "CopyEncoded",
                        ["Paste"] = "PasteFromClipboard",
                        ["Load Configuration from Encoded String"] = "LoadFromEncodedTitle",
                        ["Paste an encoded configuration string to decode and load it."] = "LoadFromEncodedDescription",
                        ["Encoded Configuration String:"] = "EncodedConfigString",
                        ["Decoded Configuration"] = "DecodedConfiguration",
                        ["Flags:"] = "FlagsLabel",
                        ["Save Current Configuration"] = "SaveCurrentConfig",
                        ["Save the currently selected build flags to your configuration library."] = "SaveCurrentConfigDescription",
                        ["Configuration Saved"] = "ConfigurationSavedTitle",
                        ["Flags:"] = "FlagsLabel",
                        ["Encoded String (for sharing):"] = "EncodedStringForSharing"
                    };
                    
                    foreach (var mapping in translationMap)
                    {
                        if (text.Contains(mapping.Key))
                        {
                            textBlock.Text = textBlock.Text.Replace(mapping.Key, LocalizationManager.Get(mapping.Value));
                            break;
                        }
                    }
                }
                
                if (child is Button button && !string.IsNullOrEmpty(button.Content?.ToString()))
                {
                    var buttonText = button.Content.ToString().Trim();
                    var buttonMap = new Dictionary<string, string>
                    {
                        ["Copy"] = "CopyEncoded",
                        ["Paste"] = "PasteFromClipboard",
                        ["Load Selected"] = "Load",
                        ["Delete"] = "Delete"
                    };
                    
                    foreach (var mapping in buttonMap)
                    {
                        if (buttonText == mapping.Key)
                        {
                            button.Content = LocalizationManager.Get(mapping.Value);
                            break;
                        }
                    }
                }
                
                // Recursively process children
                LocalizeTextBlocksRecursively(child);
            }
        }

        private void LocalizeCloseButton()
        {
            // Find the close button in the visual tree
            var grid = Content as Grid;
            if (grid != null)
            {
                // Find Border among grid children
                foreach (var child in grid.Children)
                {
                    if (child is Border border && border.Child is Button closeBtn)
                    {
                        closeBtn.Content = LocalizationManager.Get("Close");
                        break;
                    }
                }
            }
        }

        #region Saved Configurations Tab

        private void ConfigurationsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var isSelected = ConfigurationsListBox.SelectedItem != null;
            LoadButton.IsEnabled = isSelected;
            DeleteButton.IsEnabled = isSelected;
            
            if (ConfigurationsListBox.SelectedItem is SavedBuildConfiguration config)
            {
                ShowConfigurationDetails(config);
            }
            else
            {
                HideConfigurationDetails();
            }
        }

        private void ConfigurationsListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ConfigurationsListBox.SelectedItem is SavedBuildConfiguration config)
            {
                SelectedConfiguration = config;
                DialogResult = true;
                Close();
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConfigurationsListBox.SelectedItem is SavedBuildConfiguration config)
            {
                SelectedConfiguration = config;
                DialogResult = true;
                Close();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConfigurationsListBox.SelectedItem is SavedBuildConfiguration config)
            {
                var identifierText = !string.IsNullOrEmpty(config.EncodedConfig) 
                    ? $"Encoded: {config.EncodedConfig.Substring(0, Math.Min(40, config.EncodedConfig.Length))}..." 
                    : $"Configuration: {config.ConfigurationName}";
                
                var result = MessageBox.Show(
                    LocalizationManager.GetFormat("ConfirmDelete", config.ConfigurationName, identifierText),
                    LocalizationManager.Get("ConfirmDeleteTitle"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var deleted = !string.IsNullOrEmpty(config.FileName) && _configManager.DeleteConfiguration(config.FileName);
                        
                        if (deleted)
                        {
                            _configurations.Remove(config);
                            ConfigurationsListBox.ItemsSource = null;
                            ConfigurationsListBox.ItemsSource = _configurations;
                            
                            // Show empty state if no more configurations
                            if (!_configurations.Any())
                            {
                                EmptyStateText.Visibility = Visibility.Visible;
                            }
                            
                            MessageBox.Show(
                                LocalizationManager.Get("ConfigDeleted"),
                                LocalizationManager.Get("DeleteSuccess"),
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show(
                                LocalizationManager.Get("DeleteFailed"),
                                LocalizationManager.Get("DeleteFailedTitle"),
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            LocalizationManager.GetFormat("FailedToDelete", ex.Message),
                            LocalizationManager.Get("DeleteError"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ShowConfigurationDetails(SavedBuildConfiguration config)
        {
            ConfigDetailsPanel.Visibility = Visibility.Visible;
            EmptyFlagsText.Visibility = Visibility.Collapsed;
            
            DetailNameText.Text = config.ConfigurationName ?? "N/A";
            DetailPlatformText.Text = config.Platform ?? "N/A";
            DetailComPortText.Text = config.ComPort ?? "N/A";
            DetailSavedDateText.Text = config.SavedDate.ToString("yyyy-MM-dd HH:mm:ss");
            DetailEncodedText.Text = config.EncodedConfig ?? "N/A";
            
            // Show or hide firmware folder button based on whether firmware file exists
            if (!string.IsNullOrEmpty(config.FirmwareFileName))
            {
                var firmwarePath = Path.Combine(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configurations"),
                    config.FirmwareFileName);
                
                if (File.Exists(firmwarePath))
                {
                    OpenFirmwareFolderButton.Visibility = Visibility.Visible;
                }
                else
                {
                    OpenFirmwareFolderButton.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                OpenFirmwareFolderButton.Visibility = Visibility.Collapsed;
            }
            
            if (config.EnabledFlagKeys != null && config.EnabledFlagKeys.Any())
            {
                FlagsCountText.Text = LocalizationManager.GetFormat("EnabledFlags", config.EnabledFlagKeys.Count);
                FlagsListBox.ItemsSource = config.EnabledFlagKeys.OrderBy(f => f);
            }
            else
            {
                FlagsCountText.Text = LocalizationManager.GetFormat("EnabledFlags", 0);
                FlagsListBox.ItemsSource = null;
            }
        }
        
        private void HideConfigurationDetails()
        {
            ConfigDetailsPanel.Visibility = Visibility.Collapsed;
            EmptyFlagsText.Visibility = Visibility.Visible;
            FlagsListBox.ItemsSource = null;
            FlagsCountText.Text = LocalizationManager.GetFormat("EnabledFlags", 0);
        }

        private void CopyEncodedButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ConfigurationsListBox.SelectedItem is SavedBuildConfiguration config)
                {
                    if (!string.IsNullOrWhiteSpace(config.EncodedConfig))
                    {
                        Clipboard.SetText(config.EncodedConfig);
                        MessageBox.Show(
                            LocalizationManager.Get("EncodedCopied"),
                            LocalizationManager.Get("CopySuccess"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            LocalizationManager.Get("NoEncodedValue"),
                            LocalizationManager.Get("NoEncodedValueTitle"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationManager.GetFormat("FailedToCopy", ex.Message),
                    LocalizationManager.Get("CopyError"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OpenFirmwareFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ConfigurationsListBox.SelectedItem is SavedBuildConfiguration config)
                {
                    if (!string.IsNullOrEmpty(config.FirmwareFileName))
                    {
                        var configurationsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configurations");
                        var firmwarePath = Path.Combine(configurationsDir, config.FirmwareFileName);
                        
                        if (File.Exists(firmwarePath))
                        {
                            // Open Windows Explorer and select the firmware file
                            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{firmwarePath}\"");
                        }
                        else
                        {
                            MessageBox.Show(
                                LocalizationManager.GetFormat("FirmwareFileNotFound", config.FirmwareFileName),
                                LocalizationManager.Get("FileNotFound"),
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationManager.GetFormat("OpenFolderError", ex.Message),
                    LocalizationManager.Get("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region Load from Encoded String Tab

        private void PasteEncodedButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    var clipboardText = Clipboard.GetText()?.Trim();
                    
                    if (!string.IsNullOrEmpty(clipboardText))
                    {
                        EncodedInputTextBox.Text = clipboardText;
                        MessageBox.Show(
                            LocalizationManager.Get("ContentPasted"),
                            LocalizationManager.Get("PasteSuccess"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            LocalizationManager.Get("ClipboardEmpty"),
                            LocalizationManager.Get("NoContent"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show(
                        LocalizationManager.Get("ClipboardInvalid"),
                        LocalizationManager.Get("InvalidContent"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationManager.GetFormat("FailedToPaste", ex.Message),
                    LocalizationManager.Get("PasteError"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DecodeConfiguration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var encoded = EncodedInputTextBox.Text?.Trim();
                
                if (string.IsNullOrWhiteSpace(encoded))
                {
                    MessageBox.Show(
                        LocalizationManager.Get("NoInput"),
                        LocalizationManager.Get("NoInputTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var decodedFlags = BuildConfigurationHasher.DecodeOptions(encoded);
                
                if (decodedFlags == null || !decodedFlags.Any())
                {
                    MessageBox.Show(
                        LocalizationManager.Get("DecodingFailed"),
                        LocalizationManager.Get("DecodingFailedTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    
                    // Disable action buttons
                    LoadDecodedButton.IsEnabled = false;
                    SaveDecodedButton.IsEnabled = false;
                    return;
                }

                DecodedFlagsListBox.ItemsSource = decodedFlags.OrderBy(f => f);
                DecodedCountTextBlock.Text = LocalizationManager.GetFormat("FlagsDecoded", decodedFlags.Length);
                
                DecodedResultsPanel.Visibility = Visibility.Visible;
                
                // Enable action buttons
                LoadDecodedButton.IsEnabled = true;
                SaveDecodedButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationManager.GetFormat("DecodingError", ex.Message),
                    LocalizationManager.Get("DecodingErrorTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                // Disable action buttons on error
                LoadDecodedButton.IsEnabled = false;
                SaveDecodedButton.IsEnabled = false;
            }
        }

        private void ApplyDecodedConfiguration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var encoded = EncodedInputTextBox.Text?.Trim();
                if (string.IsNullOrWhiteSpace(encoded))
                    return;

                var decodedFlags = BuildConfigurationHasher.DecodeOptions(encoded);
                if (decodedFlags == null || !decodedFlags.Any())
                    return;

                // Create configuration from decoded flags
                var flagsParameters = new Dictionary<string, Dictionary<string, string>>();
                foreach (var flagKey in decodedFlags)
                {
                    flagsParameters[flagKey] = new Dictionary<string, string>();
                }
                
                SelectedConfiguration = new SavedBuildConfiguration
                {
                    EncodedConfig = encoded,
                    ConfigurationName = $"Decoded_{DateTime.Now:yyyyMMdd_HHmmss}",
                    SavedDate = DateTime.Now,
                    Platform = string.Empty,
                    ComPort = string.Empty,
                    BuildFlagsParameters = flagsParameters
                };

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationManager.GetFormat("ErrorLoadingConfig", ex.Message),
                    LocalizationManager.Get("LoadError"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SaveDecodedConfiguration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var encoded = EncodedInputTextBox.Text?.Trim();
                if (string.IsNullOrWhiteSpace(encoded))
                {
                    MessageBox.Show(
                        LocalizationManager.Get("DecodeConfigFirst"),
                        LocalizationManager.Get("NoConfiguration"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var decodedFlags = BuildConfigurationHasher.DecodeOptions(encoded);
                if (decodedFlags == null || !decodedFlags.Any())
                {
                    MessageBox.Show(
                        LocalizationManager.Get("DecodingFailed"),
                        LocalizationManager.Get("DecodingFailedTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Prompt for name
                var inputDialog = new ConfigurationNameInputWindow()
                {
                    Owner = this
                };

                if (inputDialog.ShowDialog() == true)
                {
                    var configName = inputDialog.ConfigurationName;
                    
                    if (string.IsNullOrWhiteSpace(configName))
                    {
                        MessageBox.Show(
                            LocalizationManager.Get("InvalidName"),
                            LocalizationManager.Get("InvalidNameTitle"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    // Create BuildFlagItem objects from decoded keys
                    var enabledFlags = new List<BuildFlagItem>();
                    foreach (var flagKey in decodedFlags)
                    {
                        enabledFlags.Add(new BuildFlagItem { Key = flagKey });
                    }

                    // Save configuration (hash will be auto-generated internally)
                    _configManager.SaveConfiguration(enabledFlags, configName);

                    MessageBox.Show(
                        LocalizationManager.GetFormat("ConfigurationSaved", configName),
                        LocalizationManager.Get("SaveSuccessTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Refresh the saved configurations list
                    _configurations.Clear();
                    foreach (var config in _configManager.GetAllConfigurations())
                    {
                        _configurations.Add(config);
                    }
                    ConfigurationsListBox.ItemsSource = null;
                    ConfigurationsListBox.ItemsSource = _configurations;
                    
                    // Hide empty state
                    EmptyStateText.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationManager.GetFormat("ErrorSavingConfig", ex.Message),
                    LocalizationManager.Get("SaveErrorTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region Save Current Configuration Tab

        private void SaveCurrentConfiguration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var enabledFlags = _allFlags.Where(f => f.IsEnabled).ToList();
                
                if (!enabledFlags.Any())
                {
                    MessageBox.Show(
                        LocalizationManager.Get("NoFlagsEnabled"),
                        LocalizationManager.Get("NoFlagsSelectedTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                // Prompt for configuration name
                var inputDialog = new ConfigurationNameInputWindow()
                {
                    Owner = this
                };

                if (inputDialog.ShowDialog() == true)
                {
                    var configName = inputDialog.ConfigurationName;
                    
                    if (string.IsNullOrWhiteSpace(configName))
                    {
                        MessageBox.Show(
                            LocalizationManager.Get("InvalidName"),
                            LocalizationManager.Get("InvalidNameTitle"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    // Get platform and COM port (empty if not set in parent window)
                    var platform = string.Empty;
                    var comPort = string.Empty;

                    // Save the configuration
                    _configManager.SaveConfiguration(
                        enabledFlags,
                        configName,
                        platform,
                        comPort);

                    // Generate encoded string for display
                    var encoded = BuildConfigurationHasher.EncodeOptions(enabledFlags);

                    // Update UI with success message
                    SavedNameText.Text = configName;
                    SavedFlagCountText.Text = LocalizationManager.GetFormat("FlagsCount", enabledFlags.Count);
                    SavedPlatformText.Text = string.IsNullOrEmpty(platform) ? LocalizationManager.Get("NotSpecified") : platform;
                    SavedComPortText.Text = string.IsNullOrEmpty(comPort) ? LocalizationManager.Get("NotSpecified") : comPort;
                    SavedEncodedTextBox.Text = encoded;

                    SaveResultsPanel.Visibility = Visibility.Visible;

                    // Refresh the saved configurations list
                    _configurations.Clear();
                    foreach (var config in _configManager.GetAllConfigurations())
                    {
                        _configurations.Add(config);
                    }
                    ConfigurationsListBox.ItemsSource = null;
                    ConfigurationsListBox.ItemsSource = _configurations;
                    
                    // Hide empty state
                    EmptyStateText.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationManager.GetFormat("ErrorSavingConfig", ex.Message),
                    LocalizationManager.Get("SaveErrorTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CopySavedEncoded_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var text = SavedEncodedTextBox.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    Clipboard.SetText(text);
                    MessageBox.Show(
                        LocalizationManager.Get("EncodedConfigCopied"),
                        LocalizationManager.Get("CopySuccess"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationManager.GetFormat("FailedToCopy", ex.Message),
                    LocalizationManager.Get("CopyError"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
