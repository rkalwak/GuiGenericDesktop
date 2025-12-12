using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace GuiGenericBuilderDesktop
{
    /// <summary>
    /// Unified window for displaying compilation results - both success (with hash) and failure (with logs)
    /// </summary>
    public partial class CompilationResultsWindow : Window
    {
        private string? _hash;
        private string? _logs;
        private bool _isSuccess;

        /// <summary>
        /// Constructor for showing compilation failure with error logs
        /// </summary>
        public CompilationResultsWindow(string logs)
        {
            InitializeComponent();
            _logs = logs;
            _isSuccess = false;
            ConfigureForErrorLogs();
        }

        /// <summary>
        /// Constructor for showing compilation success with hash
        /// </summary>
        public CompilationResultsWindow(string hash, bool isSuccess)
        {
            InitializeComponent();
            _hash = hash;
            _isSuccess = isSuccess;
            ConfigureForSuccess();
        }

        /// <summary>
        /// Constructor for showing both hash and logs (comprehensive view)
        /// </summary>
        public CompilationResultsWindow(string hash, string logs, bool isSuccess)
        {
            InitializeComponent();
            _hash = hash;
            _logs = logs;
            _isSuccess = isSuccess;
            
            if (isSuccess)
                ConfigureForSuccess();
            else
                ConfigureForErrorLogs();
        }

        private void ConfigureForSuccess()
        {
            TitleText.Text = "Compilation Successful";
            
            // Show hash section
            HashSection.Visibility = Visibility.Visible;
            HashTextBox.Text = _hash ?? "No hash available.";
            CopyHashButton.Visibility = Visibility.Visible;
            
            // Hide or minimize log section if no logs
            if (string.IsNullOrWhiteSpace(_logs))
            {
                LogSection.Visibility = Visibility.Collapsed;
            }
            else
            {
                LogSectionTitle.Text = "Build Output:";
                LogSectionTitle.Visibility = Visibility.Visible;
                LogTextBox.Text = _logs;
                CopyLogsButton.Content = "Copy Output";
                CopyLogsButton.Visibility = Visibility.Visible;
                SaveButton.Visibility = Visibility.Visible;
            }
        }

        private void ConfigureForErrorLogs()
        {
            TitleText.Text = "Compilation Failed";
            
            // Hide hash section
            HashSection.Visibility = Visibility.Collapsed;
            
            // Show log section
            LogSectionTitle.Text = "Error Logs:";
            LogSectionTitle.Visibility = Visibility.Visible;
            LogTextBox.Text = _logs ?? "No logs available.";
            CopyLogsButton.Visibility = Visibility.Visible;
            SaveButton.Visibility = Visibility.Visible;
        }

        private void CopyHashButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_hash))
                {
                    Clipboard.SetText(_hash);
                    MessageBox.Show("Hash copied to clipboard successfully!", "Copy Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy hash to clipboard: {ex.Message}", "Copy Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyLogsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_logs))
                {
                    Clipboard.SetText(_logs);
                    MessageBox.Show($"{(_isSuccess ? "Output" : "Logs")} copied to clipboard successfully!", "Copy Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy to clipboard: {ex.Message}", "Copy Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Log files (*.log)|*.log|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    DefaultExt = ".log",
                    FileName = $"compilation_{(_isSuccess ? "output" : "errors")}_{DateTime.Now:yyyyMMdd_HHmmss}.log",
                    Title = $"Save Compilation {(_isSuccess ? "Output" : "Logs")}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var contentToSave = _logs ?? string.Empty;
                    
                    // Include hash in file if available
                    if (!string.IsNullOrWhiteSpace(_hash))
                    {
                        contentToSave = $"Build Configuration Hash:\n{_hash}\n\n{new string('=', 80)}\n\n{contentToSave}";
                    }
                    
                    File.WriteAllText(saveFileDialog.FileName, contentToSave);
                    MessageBox.Show($"Content saved successfully to:\n{saveFileDialog.FileName}", "Save Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
