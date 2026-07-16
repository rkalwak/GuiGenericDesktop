using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using GuiGenericBuilderDesktop.Localization;
using System.Text;
using System.Diagnostics;
using System.Linq.Expressions;

namespace GuiGenericBuilderDesktop
{
    public partial class CompilationResultsWindow : Window
    {
        private string _encodedConfig;
        private StringBuilder _logsBuilder = new StringBuilder();
        private string _backupFilePath;
        private string _firmwareFilePath;
        private bool _isSuccess;
        private bool _isLiveMode = false;

        // Timer fields
        private Stopwatch _compilationStopwatch;
        private CancellationTokenSource _timerCancellation;
        private Task _timerTask;

        public CompilationResultsWindow(string logs)
        {
            InitializeComponent();
            _logsBuilder.Append(logs);
            _isSuccess = false;
            ConfigureForErrorLogs();
        }

        public CompilationResultsWindow()
        {
            InitializeComponent();
            _isLiveMode = true;
            ConfigureForLiveMode();
        }

        public CompilationResultsWindow(string encodedConfig, bool isSuccess)
        {
            InitializeComponent();
            _encodedConfig = encodedConfig;
            _isSuccess = isSuccess;
            ConfigureForSuccess();
        }

        public CompilationResultsWindow(string encodedConfig, bool isSuccess, string backupFilePath)
        {
            InitializeComponent();
            _encodedConfig = encodedConfig;
            _isSuccess = isSuccess;
            _backupFilePath = backupFilePath;
            ConfigureForSuccess();
        }

        public CompilationResultsWindow(string encodedConfig, bool isSuccess, string backupFilePath, string firmwareFilePath)
        {
            InitializeComponent();
            _encodedConfig = encodedConfig;
            _isSuccess = isSuccess;
            _backupFilePath = backupFilePath;
            _firmwareFilePath = firmwareFilePath;
            ConfigureForSuccess();
        }

        public CompilationResultsWindow(string encodedConfig, string logs, bool isSuccess)
        {
            InitializeComponent();
            _encodedConfig = encodedConfig;
            _logsBuilder.Append(logs);
            _isSuccess = isSuccess;

            if (isSuccess)
                ConfigureForSuccess();
            else
                ConfigureForErrorLogs();
        }

        private void ConfigureForLiveMode()
        {
            Title = LocalizationManager.Get("CompilationInProgress");
            TitleText.Text = LocalizationManager.Get("CompilationInProgress");

            HashSection.Visibility = Visibility.Collapsed;
            BackupSection.Visibility = Visibility.Collapsed;
            FirmwareSection.Visibility = Visibility.Collapsed;
            StatusSection.Visibility = Visibility.Collapsed;

            LogSectionTitle.Text = LocalizationManager.Get("CompilationOutput");
            LogSectionTitle.Visibility = Visibility.Visible;
            LogSection.Visibility = Visibility.Visible;
            LogTextBox.Text = "";

            CopyHashButton.Visibility = Visibility.Collapsed;
            CopyLogsButton.Visibility = Visibility.Collapsed;
            SaveButton.Visibility = Visibility.Collapsed;

            // Show and start the timer
            TimerText.Visibility = Visibility.Visible;
            StartTimer();
        }

        public void SetLiveModeTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return;

            Dispatcher.Invoke(() =>
            {
                Title = title;
                TitleText.Text = title;
            });
        }

        public void StartTimer()
        {
            _compilationStopwatch = Stopwatch.StartNew();
            _timerCancellation = new CancellationTokenSource();

            _timerTask = Task.Run(async () =>
            {
                try
                {
                    while (!_timerCancellation.Token.IsCancellationRequested)
                    {
                        await Task.Delay(100, _timerCancellation.Token);

                        var elapsed = _compilationStopwatch.Elapsed.TotalSeconds;
                        Dispatcher.Invoke(() =>
                        {
                            TimerText.Text = $"? {elapsed:F1}s";
                        });
                    }
                }
                catch (TaskCanceledException)
                {
                    // Timer cancelled, this is expected
                }
            }, _timerCancellation.Token);
        }

        public void StopTimer()
        {
            if (_timerCancellation != null && !_timerCancellation.IsCancellationRequested)
            {
                _timerCancellation.Cancel();
            }

            if (_compilationStopwatch != null)
            {
                _compilationStopwatch.Stop();
            }

            if (_timerTask != null)
            {
                try
                {
                    _timerTask.Wait(1000); // Wait max 1 second for cleanup
                }
                catch (AggregateException)
                {
                    // Expected if task was cancelled
                }
            }
        }

        public double GetElapsedSeconds()
        {
            return _compilationStopwatch?.Elapsed.TotalSeconds ?? 0;
        }

        public void AppendLog(string line)
        {
            if (!string.IsNullOrEmpty(line))
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        _logsBuilder.AppendLine(line);
                        LogTextBox.AppendText(line + Environment.NewLine);
                        LogScrollViewer.ScrollToEnd();
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"? Error while appending log: {ex.Message}");
                }
            }
        }


        public void FinalizeCompilation(bool success, string encodedConfig = null, string backupPath = null, string firmwarePath = null, string customTitle = null)
        {
            Dispatcher.Invoke(() =>
            {
                // Stop the timer first
                StopTimer();

                _isSuccess = success;
                _encodedConfig = encodedConfig;
                _backupFilePath = backupPath;
                _firmwareFilePath = firmwarePath;
                _isLiveMode = false;

                // Update timer text to show final time
                var finalTime = GetElapsedSeconds();
                TimerText.Text = $"? {finalTime:F1}s";
                TimerText.Foreground = new SolidColorBrush(_isSuccess ? Color.FromRgb(46, 125, 50) : Color.FromRgb(198, 40, 40));

                ShowFinalStatus();

                if (_isSuccess)
                {
                    Title = customTitle ?? LocalizationManager.Get("CompilationSuccessTitle");
                    TitleText.Text = customTitle ?? LocalizationManager.Get("CompilationSuccessTitle");

                    if (!string.IsNullOrEmpty(_encodedConfig))
                    {
                        HashSection.Visibility = Visibility.Visible;
                        HashTextBox.Text = _encodedConfig;
                        CopyHashButton.Visibility = Visibility.Visible;
                    }

                    if (!string.IsNullOrEmpty(_backupFilePath) && File.Exists(_backupFilePath))
                    {
                        BackupSection.Visibility = Visibility.Visible;
                        BackupFileNameText.Text = Path.GetFileName(_backupFilePath);
                        BackupPathText.Text = _backupFilePath;
                    }

                    if (!string.IsNullOrEmpty(_firmwareFilePath) && File.Exists(_firmwareFilePath))
                    {
                        FirmwareSection.Visibility = Visibility.Visible;
                        FirmwareFileNameText.Text = Path.GetFileName(_firmwareFilePath);
                        FirmwarePathText.Text = _firmwareFilePath;
                    }

                    LogSectionTitle.Text = LocalizationManager.Get("BuildOutput");
                }
                else
                {
                    Title = customTitle ?? LocalizationManager.Get("CompilationFailedTitle");
                    TitleText.Text = customTitle ?? LocalizationManager.Get("CompilationFailedTitle");
                    LogSectionTitle.Text = LocalizationManager.Get("ErrorLogs");
                }

                CopyLogsButton.Visibility = Visibility.Visible;
                SaveButton.Visibility = Visibility.Visible;
            });
        }

        private void ShowFinalStatus()
        {
            StatusSection.Visibility = Visibility.Visible;

            if (_isSuccess)
            {
                StatusSection.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                StatusSection.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                StatusIcon.Text = "?";
                StatusIcon.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                StatusTitleText.Text = LocalizationManager.Get("CompilationSuccessMessage");
                StatusTitleText.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                StatusDescriptionText.Text = LocalizationManager.Get("CompilationSuccessDescription");
                StatusDescriptionText.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
            }
            else
            {
                StatusSection.BorderBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                StatusSection.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                StatusIcon.Text = "?";
                StatusIcon.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                StatusTitleText.Text = LocalizationManager.Get("CompilationFailedMessage");
                StatusTitleText.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                StatusDescriptionText.Text = LocalizationManager.Get("CompilationFailedDescription");
                StatusDescriptionText.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
            }
        }

        private void ConfigureForSuccess()
        {
            Title = (string)FindResource("CompilationSuccessTitle");
            TitleText.Text = (string)FindResource("CompilationSuccessTitle");

            ShowFinalStatus();

            HashSection.Visibility = Visibility.Visible;
            HashTextBox.Text = _encodedConfig ?? LocalizationManager.Get("NoConfigurationAvailable");
            CopyHashButton.Visibility = Visibility.Visible;

            if (!string.IsNullOrEmpty(_backupFilePath) && File.Exists(_backupFilePath))
            {
                BackupSection.Visibility = Visibility.Visible;
                BackupFileNameText.Text = Path.GetFileName(_backupFilePath);
                BackupPathText.Text = _backupFilePath;
            }

            if (!string.IsNullOrEmpty(_firmwareFilePath) && File.Exists(_firmwareFilePath))
            {
                FirmwareSection.Visibility = Visibility.Visible;
                FirmwareFileNameText.Text = Path.GetFileName(_firmwareFilePath);
                FirmwarePathText.Text = _firmwareFilePath;
            }

            LogSectionTitle.Text = (string)FindResource("BuildOutput");
            LogSectionTitle.Visibility = Visibility.Visible;
            LogSection.Visibility = Visibility.Visible;
            LogTextBox.Text = _logsBuilder.ToString();

            CopyLogsButton.Visibility = Visibility.Visible;
            SaveButton.Visibility = Visibility.Visible;
        }

        private void ConfigureForErrorLogs()
        {
            Title = (string)FindResource("CompilationFailedTitle");
            TitleText.Text = (string)FindResource("CompilationFailedTitle");

            ShowFinalStatus();

            HashSection.Visibility = Visibility.Collapsed;
            BackupSection.Visibility = Visibility.Collapsed;
            FirmwareSection.Visibility = Visibility.Collapsed;

            LogSectionTitle.Text = (string)FindResource("ErrorLogs");
            LogSectionTitle.Visibility = Visibility.Visible;
            LogSection.Visibility = Visibility.Visible;

            var logs = _logsBuilder.ToString();
            LogTextBox.Text = string.IsNullOrWhiteSpace(logs) ? LocalizationManager.Get("NoLogsAvailable") : logs;

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
                var logs = _logsBuilder.ToString();
                if (!string.IsNullOrWhiteSpace(logs))
                {
                    Clipboard.SetText(logs);
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
                    Title = _isSuccess ? LocalizationManager.Get("SaveCompilationOutput") : LocalizationManager.Get("SaveCompilationLogs")
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var contentToSave = _logsBuilder.ToString();

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
                    var directory = Path.GetDirectoryName(_backupFilePath);
                    if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                    {
                        System.Diagnostics.Process.Start("explorer.exe", directory);
                    }
                    else
                    {
                        MessageBox.Show(
                            LocalizationManager.GetFormat("BackupDirectoryNotFound", directory),
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
                    var directory = Path.GetDirectoryName(_firmwareFilePath);
                    if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                    {
                        System.Diagnostics.Process.Start("explorer.exe", directory);
                    }
                    else
                    {
                        MessageBox.Show(
                            LocalizationManager.GetFormat("DirectoryNotFound", directory),
                            LocalizationManager.Get("DirectoryNotFoundTitle"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
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
