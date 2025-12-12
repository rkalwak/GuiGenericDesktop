using System.Windows;
using System.Windows.Input;

namespace GuiGenericBuilderDesktop
{
    /// <summary>
    /// Interaction logic for ConfigurationNameInputWindow.xaml
    /// </summary>
    public partial class ConfigurationNameInputWindow : Window
    {
        public string ConfigurationName { get; private set; } = string.Empty;

        public ConfigurationNameInputWindow()
        {
            InitializeComponent();
            
            // Focus the text box when window loads
            Loaded += (s, e) => ConfigNameTextBox.Focus();
            
            // Generate default name with timestamp
            ConfigNameTextBox.Text = $"Config_{DateTime.Now:yyyyMMdd_HHmmss}";
            ConfigNameTextBox.SelectAll();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var name = ConfigNameTextBox.Text?.Trim();
            
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(
                    "Please enter a configuration name.",
                    "Name Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                ConfigNameTextBox.Focus();
                return;
            }

            // Validate name doesn't contain invalid characters
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            if (name.Any(c => invalidChars.Contains(c)))
            {
                MessageBox.Show(
                    "Configuration name contains invalid characters.\n\n" +
                    "Please avoid using: \\ / : * ? \" < > |",
                    "Invalid Characters",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                ConfigNameTextBox.Focus();
                return;
            }

            ConfigurationName = name;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ConfigNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Save on Enter key
            if (e.Key == Key.Enter)
            {
                SaveButton_Click(sender, e);
                e.Handled = true;
            }
        }
    }
}
