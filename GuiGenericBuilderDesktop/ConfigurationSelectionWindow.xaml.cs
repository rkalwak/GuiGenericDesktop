using System.Collections.Generic;
using System.Linq;
using System.Windows;
using CompilationLib;

namespace GuiGenericBuilderDesktop
{
    /// <summary>
    /// Interaction logic for ConfigurationSelectionWindow.xaml
    /// </summary>
    public partial class ConfigurationSelectionWindow : Window
    {
        private readonly List<SavedBuildConfiguration> _configurations;
        private readonly BuildConfigurationManager _configManager;

        public SavedBuildConfiguration SelectedConfiguration { get; private set; }

        public ConfigurationSelectionWindow(List<SavedBuildConfiguration> configurations)
        {
            InitializeComponent();
            _configurations = configurations;
            
            // Initialize configuration manager for loading by hash
            var configDir = System.IO.Path.Combine(
                System.AppDomain.CurrentDomain.BaseDirectory, 
                "configurations");
            _configManager = new BuildConfigurationManager(configDir);
            
            ConfigurationsListBox.ItemsSource = _configurations;
        }

        private void ConfigurationsListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var isSelected = ConfigurationsListBox.SelectedItem != null;
            LoadButton.IsEnabled = isSelected;
            DeleteButton.IsEnabled = isSelected;
            
            // Update details panel
            if (ConfigurationsListBox.SelectedItem is SavedBuildConfiguration config)
            {
                ShowConfigurationDetails(config);
            }
            else
            {
                HideConfigurationDetails();
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

        private void ConfigurationsListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Load configuration on double-click if an item is selected
            if (ConfigurationsListBox.SelectedItem is SavedBuildConfiguration config)
            {
                SelectedConfiguration = config;
                DialogResult = true;
                Close();
            }
        }
        
        private void LoadByHashButton_Click(object sender, RoutedEventArgs e)
        {
            var hash = HashInputTextBox.Text?.Trim();
            
            if (string.IsNullOrEmpty(hash))
            {
                MessageBox.Show("Please enter a hash value.", "Invalid Hash", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Normalize hash to lowercase for consistency
            hash = hash.ToLowerInvariant();
            
            // Validate hash format (64 hexadecimal characters)
            if (hash.Length != 64 || !System.Text.RegularExpressions.Regex.IsMatch(hash, "^[0-9a-f]{64}$"))
            {
                MessageBox.Show(
                    "Invalid hash format. Hash must be 64 hexadecimal characters.",
                    "Invalid Hash",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Try to load configuration by hash
            var config = _configManager.LoadConfiguration(hash);
            
            if (config == null)
            {
                var result = MessageBox.Show(
                    $"Configuration with hash:\n{hash}\n\nwas not found in the local configurations folder.\n\n" +
                    "Would you like to create a new configuration with this hash?\n" +
                    "(You'll need to manually select the flags that match this hash)",
                    "Configuration Not Found",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    // Create a placeholder configuration that will need manual flag selection
                    SelectedConfiguration = new SavedBuildConfiguration
                    {
                        Hash = hash,
                        ConfigurationName = $"Manual_{DateTime.Now:yyyyMMdd_HHmmss}",
                        SavedDate = DateTime.Now,
                        Platform = string.Empty,
                        BuildFlagsParameters = new Dictionary<string, Dictionary<string, string>>() // Empty - user will need to select flags
                    };
                    
                    MessageBox.Show(
                        "A placeholder configuration has been created.\n\n" +
                        "Please manually select the build flags that match this hash.\n" +
                        "After compilation, verify that the generated hash matches.",
                        "Manual Configuration",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    
                    DialogResult = true;
                    Close();
                }
                return;
            }

            SelectedConfiguration = config;
            DialogResult = true;
            Close();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConfigurationsListBox.SelectedItem is SavedBuildConfiguration config)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete configuration '{config.ConfigurationName}'?\n\n" +
                    $"Hash: {config.Hash}",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (_configManager.DeleteConfiguration(config.Hash))
                        {
                            _configurations.Remove(config);
                            ConfigurationsListBox.ItemsSource = null;
                            ConfigurationsListBox.ItemsSource = _configurations;
                            
                            MessageBox.Show(
                                "Configuration deleted successfully.",
                                "Delete Success",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void PasteHashButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    var clipboardText = Clipboard.GetText()?.Trim();
                    
                    if (!string.IsNullOrEmpty(clipboardText))
                    {
                        // Normalize to lowercase
                        clipboardText = clipboardText.ToLowerInvariant();
                        
                        // Validate it looks like a hash
                        if (clipboardText.Length == 64 && 
                            System.Text.RegularExpressions.Regex.IsMatch(clipboardText, "^[0-9a-f]{64}$"))
                        {
                            HashInputTextBox.Text = clipboardText;
                            MessageBox.Show(
                                "Hash pasted successfully!",
                                "Paste Success",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                        else
                        {
                            // Still paste it but warn user
                            HashInputTextBox.Text = clipboardText;
                            MessageBox.Show(
                                "Clipboard content pasted, but it may not be a valid SHA256 hash.\n\n" +
                                "Expected format: 64 hexadecimal characters (0-9, a-f)",
                                "Invalid Hash Format",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
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

        private void HashInputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Trigger load when Enter is pressed
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                LoadByHashButton_Click(sender, e);
                e.Handled = true;
            }
            // Paste when Ctrl+V is pressed
            else if (e.Key == System.Windows.Input.Key.V && 
                     (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                PasteHashButton_Click(sender, e);
                e.Handled = true;
            }
        }
        
        private void ShowConfigurationDetails(SavedBuildConfiguration config)
        {
            ConfigDetailsPanel.Visibility = Visibility.Visible;
            EmptyStateText.Visibility = Visibility.Collapsed;
            
            // Populate details
            DetailNameText.Text = config.ConfigurationName ?? "N/A";
            DetailPlatformText.Text = config.Platform ?? "N/A";
            DetailComPortText.Text = config.ComPort ?? "N/A";
            DetailSavedDateText.Text = config.SavedDate.ToString("yyyy-MM-dd HH:mm:ss");
            DetailHashText.Text = config.Hash ?? "N/A";
            
            // Populate flags list
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
            EmptyStateText.Visibility = Visibility.Visible;
            FlagsListBox.ItemsSource = null;
            FlagsCountText.Text = "Enabled Flags (0)";
        }
    }
}
