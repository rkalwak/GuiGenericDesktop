using System.Windows;
using Octokit;
using Serilog;
using GuiGenericBuilderDesktop.Localization;
using GuiGenericBuilderDesktop.Services;

namespace GuiGenericBuilderDesktop
{
    public partial class UpdateWindow : Window
    {
        private readonly AutoUpdateService _autoUpdateService;
        private readonly Release _release;
        private readonly ILogger _logger;

        public UpdateWindow(AutoUpdateService autoUpdateService, Release release, ILogger logger)
        {
            InitializeComponent();
            _autoUpdateService = autoUpdateService;
            _release = release;
            _logger = logger;

            LoadUpdateInformation();
        }

        private void LoadUpdateInformation()
        {
            Title = LocalizationManager.Get("UpdateWindowTitle");
            HeaderText.Text = LocalizationManager.Get("UpdateAvailableHeader");
            CurrentVersionLabel.Text = LocalizationManager.Get("CurrentVersionLabel");
            NewVersionLabel.Text = LocalizationManager.Get("NewVersionLabel");
            ReleaseNotesGroup.Header = LocalizationManager.Get("ReleaseNotesHeader");
            ProgressText.Text = LocalizationManager.Get("DownloadingUpdate");
            InstallButton.Content = LocalizationManager.Get("InstallUpdate");
            RemindLaterButton.Content = LocalizationManager.Get("RemindMeLater");

            CurrentVersionText.Text = _autoUpdateService.GetCurrentVersion().ToString();
            NewVersionText.Text = _release.TagName.TrimStart('v', 'V');
            ReleaseNotesText.Text = string.IsNullOrWhiteSpace(_release.Body) 
                ? LocalizationManager.Get("NoReleaseNotes") 
                : _release.Body;
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                InstallButton.IsEnabled = false;
                RemindLaterButton.IsEnabled = false;
                ProgressPanel.Visibility = Visibility.Visible;

                _logger.Information("User initiated update installation");

                var progress = new Progress<int>(percentage =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        ProgressBar.Value = percentage;
                        ProgressText.Text = LocalizationManager.GetFormat("DownloadingUpdateProgress", percentage);
                    });
                });

                var success = await _autoUpdateService.DownloadAndInstallUpdateAsync(_release, progress);

                if (success)
                {
                    _logger.Information("Update downloaded successfully. Applying update...");
                    
                    MessageBox.Show(
                        LocalizationManager.Get("UpdateDownloadSuccess"),
                        LocalizationManager.Get("UpdateReady"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    _autoUpdateService.ApplyUpdateAndRestart();
                }
                else
                {
                    _logger.Warning("Update installation failed");
                    
                    MessageBox.Show(
                        LocalizationManager.Get("UpdateDownloadFailed"),
                        LocalizationManager.Get("UpdateFailed"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    InstallButton.IsEnabled = true;
                    RemindLaterButton.IsEnabled = true;
                    ProgressPanel.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error during update installation");
                
                MessageBox.Show(
                    LocalizationManager.GetFormat("UpdateInstallError", ex.Message),
                    LocalizationManager.Get("UpdateErrorTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                InstallButton.IsEnabled = true;
                RemindLaterButton.IsEnabled = true;
                ProgressPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void RemindLaterButton_Click(object sender, RoutedEventArgs e)
        {
            _logger.Information("User chose to skip update");
            DialogResult = false;
            Close();
        }
    }
}
