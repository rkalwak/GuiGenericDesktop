using System.Windows;
using System.Windows.Controls;
using Serilog;
using CompilationLib;
using CompilationLib.GithubInteractions;

namespace GuiGenericBuilderDesktop
{
    public partial class Z2SVersionPickerWindow : Window
    {
        private readonly Z2SUpdateService _updateService;
        private readonly string _deviceVersion;
        private readonly int _versionHistoryCount;
        private readonly ILogger _logger;

        private IReadOnlyList<GitHubRelease> _releases;
        private GitHubRelease _latestRelease;

        /// <summary>
        /// The release selected by the user. Null if the window was cancelled.
        /// </summary>
        public GitHubRelease SelectedRelease { get; private set; }

        /// <summary>
        /// True when the selected release is older than the latest available release.
        /// </summary>
        public bool IsDowngrade { get; private set; }

        public Z2SVersionPickerWindow(Z2SUpdateService updateService, string deviceVersion, ILogger logger, int versionHistoryCount = 10)
        {
            InitializeComponent();
            _updateService = updateService;
            _deviceVersion = deviceVersion;
            _logger = logger;
            _versionHistoryCount = versionHistoryCount > 0 ? versionHistoryCount : 10;

            if (!string.IsNullOrWhiteSpace(deviceVersion))
                CurrentVersionText.Text = $"Aktualna wersja na urządzeniu: {deviceVersion}";

            LoadingText.Text = "Ładowanie zapisanej listy wersji firmware…";
            Loaded += async (_, __) => await LoadReleasesAsync();
        }

        private async Task LoadReleasesAsync()
        {
            try
            {
                LoadingText.Visibility = Visibility.Visible;
                LoadingText.Text = "Pobieranie listy wersji z GitHub…";
                FlashButton.IsEnabled = false;
                LoadReleasesButton.IsEnabled = false;
                RefreshReleasesButton.IsEnabled = false;

                _releases = await _updateService.GetReleasesAsync(_versionHistoryCount);

                if (_releases.Count == 0)
                {
                    LoadingText.Text = "Brak dostępnych wersji w repozytorium GitHub.";
                    return;
                }

                _latestRelease = _releases[0];

                ReleaseListBox.ItemsSource = _releases;

                // Mark the latest release item after the list renders
                ReleaseListBox.Loaded += MarkLatestItem;

                LoadingText.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to fetch Z2S releases for picker");
                LoadingText.Text = $"Błąd pobierania listy wersji: {ex.Message}";
            }
            finally
            {
                LoadReleasesButton.IsEnabled = true;
                RefreshReleasesButton.IsEnabled = true;
            }
        }

        private async void LoadReleasesButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadReleasesAsync();
        }

        private async void RefreshReleasesButton_Click(object sender, RoutedEventArgs e)
        {
            _updateService.InvalidateReleasesCache();
            await LoadReleasesAsync();
        }

        private void MarkLatestItem(object sender, RoutedEventArgs e)
        {
            ReleaseListBox.Loaded -= MarkLatestItem;
            if (_releases == null || _releases.Count == 0) return;

            var container = ReleaseListBox.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
            if (container == null) return;

            var badge = FindChild<Border>(container, "LatestBadge");
            if (badge != null)
                badge.Visibility = Visibility.Visible;
        }

        private void ReleaseListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = ReleaseListBox.SelectedItem as GitHubRelease;
            if (selected == null)
            {
                FlashButton.IsEnabled = false;
                DowngradeWarningBorder.Visibility = Visibility.Collapsed;
                return;
            }

            FlashButton.IsEnabled = true;

            bool isDowngrade = _latestRelease != null &&
                               selected.TagName != _latestRelease.TagName &&
                               selected.CreatedAt < _latestRelease.CreatedAt;

            IsDowngrade = isDowngrade;
            DowngradeWarningBorder.Visibility = isDowngrade ? Visibility.Visible : Visibility.Collapsed;
        }

        private void FlashButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = ReleaseListBox.SelectedItem as GitHubRelease;
            if (selected == null) return;

            if (IsDowngrade)
            {
                var warn = MessageBox.Show(
                    $"Wybrana wersja ({selected.TagName}) jest starsza niż najnowsza ({_latestRelease?.TagName}).\n\n" +
                    "Przejście na starszą wersję może powodować problemy z działaniem urządzenia.\n\n" +
                    "Czy na pewno chcesz kontynuować?",
                    "Ostrzeżenie – starsza wersja",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (warn != MessageBoxResult.Yes)
                    return;
            }

            SelectedRelease = selected;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typed && (child as FrameworkElement)?.Name == childName)
                    return typed;
                var result = FindChild<T>(child, childName);
                if (result != null) return result;
            }
            return null;
        }
    }
}
