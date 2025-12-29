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
        private string _encodedConfig;
        private string _logs;
        private string _backupFilePath;
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
        /// Constructor for showing compilation success with encoded configuration string
        /// </summary>
        public CompilationResultsWindow(string encodedConfig, bool isSuccess)
        {
            InitializeComponent();
            _encodedConfig = encodedConfig;
            _isSuccess = isSuccess;
            ConfigureForSuccess();
        }

        /// <summary>
        /// Constructor for showing compilation success with encoded configuration string and backup path
        /// </summary>
        public CompilationResultsWindow(string encodedConfig, bool isSuccess, string backupFilePath)
        {
            InitializeComponent();
            _encodedConfig = encodedConfig;
            _isSuccess = isSuccess;
            _backupFilePath = backupFilePath;
            ConfigureForSuccess();
        }

        /// <summary>
        /// Constructor for showing both encoded configuration and logs (comprehensive view)
        /// </summary>
        public CompilationResultsWindow(string encodedConfig, string logs, bool isSuccess)
        {
            InitializeComponent();
            _encodedConfig = encodedConfig;
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
            
            // Show encoded configuration section
            HashSection.Visibility = Visibility.Visible;
            HashTextBox.Text = _encodedConfig ?? "No configuration available.";
            CopyHashButton.Visibility = Visibility.Visible;
            
            // Show backup section if backup file path is available
            if (!string.IsNullOrEmpty(_backupFilePath) && File.Exists(_backupFilePath))
            {
                BackupSection.Visibility = Visibility.Visible;
                BackupFileNameText.Text = Path.GetFileName(_backupFilePath);
                BackupPathText.Text = _backupFilePath;
            }
            
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
                if (!string.IsNullOrWhiteSpace(_encodedConfig))
                {
                    Clipboard.SetText(_encodedConfig);
                    MessageBox.Show("Configuration string copied to clipboard successfully!", "Copy Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy configuration string to clipboard: {ex.Message}", "Copy Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    
                    // Include encoded configuration in file if available
                    if (!string.IsNullOrWhiteSpace(_encodedConfig))
                    {
                        contentToSave = $"Build Configuration String:\n{_encodedConfig}\n\n{new string('=', 80)}\n\n{contentToSave}";
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

        private void CopyBackupPathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_backupFilePath))
                {
                    Clipboard.SetText(_backupFilePath);
                    MessageBox.Show(
                        $"Backup file path copied to clipboard!\n\n{_backupFilePath}", 
                        "Copy Success", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to copy backup path to clipboard: {ex.Message}", 
                    "Copy Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        private void OpenBackupFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_backupFilePath) && File.Exists(_backupFilePath))
                {
                    var directoryPath = Path.GetDirectoryName(_backupFilePath);
                    if (Directory.Exists(directoryPath))
                    {
                        // Open folder in Windows Explorer and select the file
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{_backupFilePath}\"");
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Backup directory not found:\n{directoryPath}", 
                            "Directory Not Found", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show(
                        "Backup file not found.", 
                        "File Not Found", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to open backup folder: {ex.Message}", 
                    "Open Folder Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }
    }
}
