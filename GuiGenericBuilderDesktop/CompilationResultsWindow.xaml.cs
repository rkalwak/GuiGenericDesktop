using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using GuiGenericBuilderDesktop.Localization;

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
        private string _firmwareFilePath;
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
        /// Constructor for showing compilation success with encoded configuration, backup path, and firmware path
        /// </summary>
        public CompilationResultsWindow(string encodedConfig, bool isSuccess, string backupFilePath, string firmwareFilePath)
        {
            InitializeComponent();
            _encodedConfig = encodedConfig;
            _isSuccess = isSuccess;
            _backupFilePath = backupFilePath;
            _firmwareFilePath = firmwareFilePath;
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
            // Set window title dynamically based on success
            Title = (string)FindResource("CompilationSuccessTitle");
            TitleText.Text = (string)FindResource("CompilationSuccessTitle");
            
            // Show encoded configuration section
            HashSection.Visibility = Visibility.Visible;
            HashTextBox.Text = _encodedConfig ?? LocalizationManager.Get("NoConfigurationAvailable");
            CopyHashButton.Visibility = Visibility.Visible;
            
            // Show backup section if backup file path is available
            if (!string.IsNullOrEmpty(_backupFilePath) && File.Exists(_backupFilePath))
            {
                BackupSection.Visibility = Visibility.Visible;
                BackupFileNameText.Text = Path.GetFileName(_backupFilePath);
                BackupPathText.Text = _backupFilePath;
            }
            
            // Show firmware section if firmware file path is available
            if (!string.IsNullOrEmpty(_firmwareFilePath) && File.Exists(_firmwareFilePath))
            {
                FirmwareSection.Visibility = Visibility.Visible;
                FirmwareFileNameText.Text = Path.GetFileName(_firmwareFilePath);
                FirmwarePathText.Text = _firmwareFilePath;
            }
            
            // Hide or minimize log section if no logs
            if (string.IsNullOrWhiteSpace(_logs))
            {
                LogSection.Visibility = Visibility.Collapsed;
            }
            else
            {
                LogSectionTitle.Text = (string)FindResource("BuildOutput");
                LogSectionTitle.Visibility = Visibility.Visible;
                LogTextBox.Text = _logs;
                CopyLogsButton.Visibility = Visibility.Visible;
                SaveButton.Visibility = Visibility.Visible;
            }
        }

        private void ConfigureForErrorLogs()
        {
            // Set window title dynamically based on failure
            Title = (string)FindResource("CompilationFailedTitle");
            TitleText.Text = (string)FindResource("CompilationFailedTitle");
            
            // Hide hash section
            HashSection.Visibility = Visibility.Collapsed;
            
            // Show log section
            LogSectionTitle.Text = (string)FindResource("ErrorLogs");
            LogSectionTitle.Visibility = Visibility.Visible;
            LogTextBox.Text = _logs ?? LocalizationManager.Get("NoLogsAvailable");
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
                    MessageBox.Show(
                        LocalizationManager.Get("ConfigCopied"),
                        LocalizationManager.Get("CopySuccess"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationManager.GetFormat("FailedToCopyConfig", ex.Message),
                    LocalizationManager.Get("CopyError"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CopyLogsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_logs))
                {
                    Clipboard.SetText(_logs);
                    var message = _isSuccess 
                        ? LocalizationManager.Get("OutputCopied")
                        : LocalizationManager.Get("LogsCopied");
                    MessageBox.Show(
                        message,
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = LocalizationManager.Get("LogFilesFilter"),
                    DefaultExt = ".log",
                    FileName = $"compilation_{(_isSuccess ? "output" : "errors")}_{DateTime.Now:yyyyMMdd_HHmmss}.log",
                    Title = _isSuccess 
                        ? LocalizationManager.Get("SaveCompilationOutput")
                        : LocalizationManager.Get("SaveCompilationLogs")
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var contentToSave = _logs ?? string.Empty;
                    
                    // Include encoded configuration in file if available
                    if (!string.IsNullOrWhiteSpace(_encodedConfig))
                    {
                        contentToSave = $"{LocalizationManager.Get("BuildConfigurationString")}:\n{_encodedConfig}\n\n{new string('=', 80)}\n\n{contentToSave}";
                    }
                    
                    File.WriteAllText(saveFileDialog.FileName, contentToSave);
                    MessageBox.Show(
                        LocalizationManager.GetFormat("LogsSaved", saveFileDialog.FileName),
                        LocalizationManager.Get("SaveSuccess"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationManager.GetFormat("FailedToSave", ex.Message),
                    LocalizationManager.Get("SaveError"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
                        LocalizationManager.GetFormat("BackupPathCopied", _backupFilePath),
                        LocalizationManager.Get("CopySuccess"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationManager.GetFormat("FailedToCopyBackupPath", ex.Message),
                    LocalizationManager.Get("CopyError"),
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
                            LocalizationManager.GetFormat("BackupDirectoryNotFound", directoryPath),
                            LocalizationManager.Get("DirectoryNotFoundTitle"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show(
                        LocalizationManager.Get("BackupFileNotFound"),
                        LocalizationManager.Get("FileNotFound"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationManager.GetFormat("OpenFolderError", ex.Message),
                    LocalizationManager.Get("OpenFolderErrorTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        private void CopyFirmwarePathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_firmwareFilePath))
                {
                    Clipboard.SetText(_firmwareFilePath);
                    MessageBox.Show(
                        LocalizationManager.GetFormat("FirmwarePathCopied", _firmwareFilePath),
                        LocalizationManager.Get("CopySuccess"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationManager.GetFormat("FailedToCopyFirmwarePath", ex.Message),
                    LocalizationManager.Get("CopyError"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OpenFirmwareFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_firmwareFilePath) && File.Exists(_firmwareFilePath))
                {
                    // Open folder in Windows Explorer and select the file
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{_firmwareFilePath}\"");
                }
                else
                {
                    MessageBox.Show(
                        LocalizationManager.Get("FirmwareFileNotFoundError"),
                        LocalizationManager.Get("FileNotFound"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationManager.GetFormat("OpenFolderError", ex.Message),
                    LocalizationManager.Get("OpenFolderErrorTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
