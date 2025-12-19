using System.IO;
using System.Windows;
using System.Windows.Controls;
using CompilationLib;

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
                    $"Are you sure you want to delete configuration '{config.ConfigurationName}'?\n\n" +
                    $"{identifierText}",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        var deleted = !string.IsNullOrEmpty(config.Hash) && _configManager.DeleteConfiguration(config.Hash);
#pragma warning restore CS0618 // Type or member is obsolete
                        
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
                                "Configuration deleted successfully.",
                                "Delete Success",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show(
                                "Failed to delete configuration file.",
                                "Delete Failed",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Failed to delete configuration: {ex.Message}",
                            "Delete Error",
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
            
            if (config.EnabledFlagKeys != null && config.EnabledFlagKeys.Any())
            {
                FlagsCountText.Text = $"Enabled Flags ({config.EnabledFlagKeys.Count})";
                FlagsListBox.ItemsSource = config.EnabledFlagKeys.OrderBy(f => f);
            }
            else
            {
                FlagsCountText.Text = "Enabled Flags (0)";
                FlagsListBox.ItemsSource = null;
            }
        }
        
        private void HideConfigurationDetails()
        {
            ConfigDetailsPanel.Visibility = Visibility.Collapsed;
            EmptyFlagsText.Visibility = Visibility.Visible;
            FlagsListBox.ItemsSource = null;
            FlagsCountText.Text = "Enabled Flags (0)";
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
                            "Encoded configuration copied to clipboard!\n\n" +
                            "You can now share this string or paste it in the 'Load from Encoded String' tab.",
                            "Copy Success",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            "This configuration does not have an encoded value.\n\n" +
                            "It may have been created with an older version.",
                            "No Encoded Value",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to copy to clipboard: {ex.Message}",
                    "Copy Error",
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
                            "Content pasted successfully!\n\n" +
                            "Click 'Decode and Preview' to view the configuration.",
                            "Paste Success",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            "Clipboard is empty.",
                            "No Content",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show(
                        "Clipboard does not contain text.",
                        "Invalid Content",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to paste from clipboard: {ex.Message}",
                    "Paste Error",
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
                        "Please enter an encoded configuration string.",
                        "No Input",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var decodedFlags = BuildConfigurationHasher.DecodeOptions(encoded);
                
                if (decodedFlags == null || !decodedFlags.Any())
                {
                    MessageBox.Show(
                        "Failed to decode the configuration.\n\n" +
                        "Please check that the encoded string is valid and not corrupted.",
                        "Decoding Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    
                    // Disable action buttons
                    LoadDecodedButton.IsEnabled = false;
                    SaveDecodedButton.IsEnabled = false;
                    return;
                }

                DecodedFlagsListBox.ItemsSource = decodedFlags.OrderBy(f => f);
                DecodedCountTextBlock.Text = $"{decodedFlags.Length} flags decoded";
                
                DecodedResultsPanel.Visibility = Visibility.Visible;
                
                // Enable action buttons
                LoadDecodedButton.IsEnabled = true;
                SaveDecodedButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Decoding error: {ex.Message}",
                    "Decoding Error",
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
                    $"Error loading configuration: {ex.Message}",
                    "Load Error",
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
                        "Please decode a configuration first.",
                        "No Configuration",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var decodedFlags = BuildConfigurationHasher.DecodeOptions(encoded);
                if (decodedFlags == null || !decodedFlags.Any())
                {
                    MessageBox.Show(
                        "Failed to decode the configuration.",
                        "Decoding Failed",
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
                            "Configuration name cannot be empty.",
                            "Invalid Name",
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
                        $"Configuration '{configName}' saved successfully!\n\n" +
                        $"It will now appear in the 'Saved Configurations' tab.",
                        "Save Success",
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
                    $"Error saving configuration: {ex.Message}",
                    "Save Error",
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
                        "No flags are currently enabled.\n\n" +
                        "Please enable some flags before saving the configuration.",
                        "No Flags Selected",
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
                            "Configuration name cannot be empty.",
                            "Invalid Name",
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
                    SavedFlagCountText.Text = $"{enabledFlags.Count} flags";
                    SavedPlatformText.Text = string.IsNullOrEmpty(platform) ? "Not specified" : platform;
                    SavedComPortText.Text = string.IsNullOrEmpty(comPort) ? "Not specified" : comPort;
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
                    $"Error saving configuration: {ex.Message}",
                    "Save Error",
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
                        "Encoded configuration copied to clipboard!",
                        "Copied",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to copy to clipboard: {ex.Message}",
                    "Copy Error",
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
