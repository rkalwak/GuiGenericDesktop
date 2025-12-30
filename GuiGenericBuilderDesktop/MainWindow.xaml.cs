using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Newtonsoft.Json;
using CompilationLib;
using Serilog;

namespace GuiGenericBuilderDesktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<BuildFlagItem> AllBuildFlags { get; set; }
        GitHubRepoDownloader _gitHubRepoDownloader = new GitHubRepoDownloader();
        DeviceDetector _deviceDetector = new(new EsptoolWrapper());
        string _repositoryPath = string.Empty;
        string _port = string.Empty;
        BuildConfigurationManager _configManager;
        private ComboBox boardSelector;
        private ComboBox comPortSelector;
        private ComboBox flashSizeSelector;
        private ProgressBar compileProgressBar;
        private TextBlock compileTimerText;
        private CheckBox deployCheckBox;
        private CheckBox backupCheckBox;
        private CheckBox eraseFlashCheckBox;
        private CancellationTokenSource _compileTimerCts;
        private readonly ILogger _logger;
        private Button updateGGButton;
        private Button checkDeviceButton;
        private Button compileButton;
        private TextBlock statusText;

        public MainWindow()
        {
            InitializeComponent();
            _logger = Log.ForContext<MainWindow>();
            _logger.Information("MainWindow initializing");

            AllBuildFlags = new List<BuildFlagItem>();

            // Initialize configuration manager
            var configDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configurations");
            _configManager = new BuildConfigurationManager(configDir);

            InitializeBuildFlags();

            // Add the Parameters column dynamically so it's visible in the grid
            AddParametersColumnDynamically();

            FlagsDataGrid.ItemsSource = AllBuildFlags;
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GGLocal"))) 
            {
                _repositoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "repo", "gg");
            }
            else
            {
                _repositoryPath = @"c:\repozytoria\platformio\GUI-Generic";
            }


            _logger.Information("MainWindow initialized successfully");
        }

        private void AddParametersColumnDynamically()
        {
            // Prevent adding twice
            if (FlagsDataGrid.Columns.Any(c => string.Equals(c.Header?.ToString(), "Parameters", StringComparison.OrdinalIgnoreCase)))
                return;

            var templateCol = new DataGridTemplateColumn { Header = "Parameters", Width = new DataGridLength(120) };

            // Create DataTemplate in code
            var buttonFactory = new FrameworkElementFactory(typeof(Button));
            buttonFactory.SetValue(Button.ContentProperty, "Params...");
            buttonFactory.SetValue(Button.PaddingProperty, new Thickness(6, 2, 6, 2));
            buttonFactory.SetValue(Button.HorizontalAlignmentProperty, HorizontalAlignment.Center);

            // Bind Tag to entire row (the BuildFlagItem)
            var tagBinding = new Binding(); // binds to DataContext (row item)
            buttonFactory.SetBinding(Button.TagProperty, tagBinding);
            // Register Click handler
            buttonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(EditParameters_Click));

            var dataTemplate = new DataTemplate { VisualTree = buttonFactory };
            templateCol.CellTemplate = dataTemplate;

            // Insert before Description column if possible, otherwise add to end
            int insertIndex = Math.Max(0, FlagsDataGrid.Columns.Count - 1);
            FlagsDataGrid.Columns.Insert(insertIndex, templateCol);
        }

        private void InitializeBuildFlags()
        {
            try
            {
                string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "builder.json");

                if (!File.Exists(jsonPath))
                {
                    MessageBox.Show($"builder.json not found at: {jsonPath}", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string jsonContent = File.ReadAllText(jsonPath);

                // Use Newtonsoft.Json for deserialization
                var deserialized = JsonConvert.DeserializeObject<BuilderConfig>(jsonContent);

                if (deserialized?.Sections == null)
                {
                    MessageBox.Show($"builder.json does not contain valid Sections", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }


                // First pass: populate AllBuildFlags with metadata but keep IsEnabled as the deserialized default
                foreach (var sectionItem in deserialized.Sections.OrderBy(X => X.Value.Order))
                {
                    foreach (var flagItem in sectionItem.Value.Flags)
                    {
                        flagItem.Value.Section = sectionItem.Key;
                        flagItem.Value.Key = flagItem.Key;

                        // Initialize parameter values from DefaultValue if Value is not set
                        if (flagItem.Value.Parameters != null)
                        {
                            foreach (var param in flagItem.Value.Parameters)
                            {
                                if (string.IsNullOrEmpty(param.Value) && !string.IsNullOrEmpty(param.DefaultValue))
                                {
                                    param.Value = param.DefaultValue;
                                }
                            }
                        }

                        // SectionOrder has an internal setter in BuildFlagItem; cannot assign from this assembly.
                        // Preserve existing SectionOrder value from deserialization instead of assigning here.
                        AllBuildFlags.Add(flagItem.Value);
                    }
                }

                BuildFlowDocument();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading builder.json: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BuildFlowDocument()
        {
            var doc = new FlowDocument
            {
                PagePadding = new Thickness(12),
                ColumnGap = 24,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 12
            };
            // Add Device detection panel
            // Device detection panel
            var devicePanel = new DockPanel { LastChildFill = false, Margin = new Thickness(12, 8, 12, 6) };

            // Board selector ComboBox
            var boardLabel = new TextBlock(new Run("Board:")) { FontWeight = FontWeights.SemiBold, Margin = new Thickness(12, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };
            boardSelector = new ComboBox { Width = 220, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            boardSelector.Items.Add(new ComboBoxItem { Content = "None", Tag = "None", IsSelected = true });
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP32 (default)", Tag = "GUI_Generic_ESP32" });
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP32-C3", Tag = "GUI_Generic_ESP32C3" });
            //boardSelector.Items.Add(new ComboBoxItem { Content = "ESP8266", Tag = "GUI_Generic_ESP8266" });
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP32-C6", Tag = "GUI_Generic_ESP32C6" });
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP32-S3", Tag = "GUI_Generic_ESP32S3" });
            var loadConfigButton = new Button
            {
                Content = "Manage Configs...",
                Width = 130,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(4)
            };
            loadConfigButton.Click += LoadConfig_Click;

            updateGGButton = new Button
            {
                Content = "1. Update Gui-Generic",
                Width = 140,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(4)
            };
            updateGGButton.Click += UpdateGG_Click;


            // Load Configuration button



            checkDeviceButton = new Button { Content = "2. Check Device", Width = 120, Height = 28, Margin = new Thickness(8, 0, 0, 0) };
            checkDeviceButton.Click += CheckConnectedDevice_Click;

            // Status text for operations
            statusText = new TextBlock
            {
                Text = string.Empty,
                Margin = new Thickness(12, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Normal,
                Foreground = System.Windows.Media.Brushes.DarkBlue,
                Visibility = Visibility.Collapsed
            };

            // Progress bar and timer for compile operation (counts elapsed time)
            compileProgressBar = new ProgressBar
            {
                Width = 200,
                Height = 20,
                Margin = new Thickness(8, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Minimum = 0,
                Maximum = 180, // 3 minutes maximum for display purposes
                Value = 0,
                IsIndeterminate = false
            };

            compileTimerText = new TextBlock
            {
                Text = string.Empty,
                Margin = new Thickness(4, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.SemiBold,
            };

            compileButton = new Button
            {
                Content = "3. Compile",
                Width = 140,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(4)
            };
            compileButton.Click += CompileSelected_Click;
            // right-align the button inside the DockPanel

            // COM port selector (COM1..COM10)
            var portLabel = new TextBlock(new Run("Port:")) { FontWeight = FontWeights.SemiBold, Margin = new Thickness(12, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };
            comPortSelector = new ComboBox { Width = 100, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            comPortSelector.Items.Add(new ComboBoxItem { Content = $"None", Tag = $"None", IsSelected = true });
            for (int i = 1; i <= 100; i++)
            {
                var item = new ComboBoxItem { Content = $"COM{i}", Tag = $"COM{i}" };
                comPortSelector.Items.Add(item);
            }
            comPortSelector.SelectionChanged += (s, e) =>
            {
                if (comPortSelector.SelectedItem is ComboBoxItem ci)
                {
                    _port = (ci.Tag as string) ?? (ci.Content as string) ?? string.Empty;
                }
            };

            devicePanel.Children.Add(portLabel);
            devicePanel.Children.Add(comPortSelector);
            devicePanel.Children.Add(boardLabel);
            devicePanel.Children.Add(boardSelector);

            // Flash size selector
            var flashSizeLabel = new TextBlock(new Run("Flash:")) { FontWeight = FontWeights.SemiBold, Margin = new Thickness(12, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };
            flashSizeSelector = new ComboBox { Width = 120, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = "None", Tag = "None", IsSelected = true });
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = "4MB", Tag = "4MB" });
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = "8MB", Tag = "8MB" });
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = "16MB", Tag = "16MB" });
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = "32MB", Tag = "32MB" });
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = "64MB", Tag = "64MB" });
            devicePanel.Children.Add(flashSizeLabel);
            devicePanel.Children.Add(flashSizeSelector);

            // Status text (hidden initially)
            devicePanel.Children.Add(statusText);

            // Progress bar and countdown (these will be hidden initially)
            devicePanel.Children.Add(compileTimerText);
            devicePanel.Children.Add(compileProgressBar);

            // Deploy checkbox - positioned right before compile button
            deployCheckBox = new CheckBox
            {
                Content = "Deploy",
                IsChecked = true,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 4, 0),
                FontWeight = FontWeights.SemiBold
            };

            // Backup checkbox - positioned right before deploy checkbox
            backupCheckBox = new CheckBox
            {
                Content = "Backup",
                IsChecked = true,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 4, 0),
                FontWeight = FontWeights.SemiBold,
                ToolTip = "Create backup before deploying firmware"
            };

            // Erase Flash checkbox - positioned right before backup checkbox
            eraseFlashCheckBox = new CheckBox
            {
                Content = "Erase Flash",
                IsChecked = false,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 4, 0),
                FontWeight = FontWeights.SemiBold,
                ToolTip = "Erase flash memory before deploying firmware (recommended for clean installation)"
            };


            DockPanel.SetDock(compileButton, Dock.Right);
            devicePanel.Children.Add(compileButton);
            DockPanel.SetDock(deployCheckBox, Dock.Right);
            devicePanel.Children.Add(deployCheckBox);
            DockPanel.SetDock(backupCheckBox, Dock.Right);
            devicePanel.Children.Add(backupCheckBox);
            DockPanel.SetDock(eraseFlashCheckBox, Dock.Right);
            devicePanel.Children.Add(eraseFlashCheckBox);


            DockPanel.SetDock(checkDeviceButton, Dock.Right);
            devicePanel.Children.Add(checkDeviceButton);
            DockPanel.SetDock(updateGGButton, Dock.Right);
            devicePanel.Children.Add(updateGGButton);

            DockPanel.SetDock(loadConfigButton, Dock.Right);
            devicePanel.Children.Add(loadConfigButton);


            // right-align the button inside the DockPanel


            doc.Blocks.Add(new BlockUIContainer(devicePanel));

            var grouped = AllBuildFlags.GroupBy(f => f.Section).ToList();

            foreach (var group in grouped)
            {
                // Section header with a checkbox to toggle all flags in the section
                var headerPanel = new DockPanel
                {
                    LastChildFill = true,
                    Margin = new Thickness(0, 8, 0, 4),

                };
                var groupCheckBox = new CheckBox
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(2),
                    IsThreeState = false
                };
                // Initialize checked state based on group's items
                UpdateGroupCheckBoxState(groupCheckBox, group);

                // When checkbox toggled, set all items in the group accordingly
                groupCheckBox.Checked += (s, e) => SetGroupFlags(group, true, groupCheckBox);
                groupCheckBox.Unchecked += (s, e) => SetGroupFlags(group, false, groupCheckBox);

                var titleText = new TextBlock(new Run(group.Key + $" ({group.Count()})")) { FontWeight = FontWeights.Bold, FontSize = 14, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };

                headerPanel.Children.Add(groupCheckBox);
                headerPanel.Children.Add(titleText);

                // Subscribe to item property changes to update group checkbox state
                foreach (var item in group)
                {
                    item.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(BuildFlagItem.IsEnabled))
                        {
                            Dispatcher.Invoke(() => UpdateGroupCheckBoxState(groupCheckBox, group));
                        }
                    };
                }

                // Create bordered container for the section
                var border = new Border
                {
                    BorderBrush = System.Windows.Media.Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8),
                    Margin = new Thickness(0, 8, 0, 8),
                    Background = System.Windows.Media.Brushes.White
                };

                var panel = new StackPanel { Orientation = Orientation.Vertical };
                panel.Children.Add(headerPanel);

                // Grid to show items
                var grid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

                // header row
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                AddText(grid, 0, 0, "Enabled", FontWeights.SemiBold);
                AddText(grid, 0, 1, "Key", FontWeights.SemiBold);
                AddText(grid, 0, 2, "Name", FontWeights.SemiBold);
                AddText(grid, 0, 3, "Description", FontWeights.SemiBold);
                AddText(grid, 0, 4, "Parameters", FontWeights.SemiBold);

                int r = 1;
                foreach (var item in group.OrderBy(i => i.SectionOrder).ThenBy(x => x.Key))
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    var chk = new CheckBox { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2) };
                    chk.SetBinding(CheckBox.IsCheckedProperty, new Binding(nameof(BuildFlagItem.IsEnabled)) { Source = item, Mode = BindingMode.TwoWay });

                    chk.Checked += (s, e) =>
                    {
                        var errorMessage = DependencyResolver.ProcessFlagEnabled(item, AllBuildFlags);
                        // Only show error if the flag is NOT enabled (meaning ProcessFlagEnabled failed)
                        // If the flag IS enabled, it was successfully auto-enabled, so no error
                        if (errorMessage != null && !item.IsEnabled)
                        {
                            MessageBox.Show(
                                errorMessage,
                                "Blocking Dependencies",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                    };
                    chk.Unchecked += (s, e) =>
                    {
                        DependencyResolver.ProcessFlagDisabled(item, AllBuildFlags);
                    };
                    Grid.SetRow(chk, r); Grid.SetColumn(chk, 0); grid.Children.Add(chk);

                    AddText(grid, r, 1, item.Key ?? string.Empty);
                    AddText(grid, r, 2, item.FlagName ?? string.Empty);
                    AddText(grid, r, 3, item.Description ?? string.Empty);

                    var btn = new Button { Content = "Params...", Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(2), Tag = item };
                    btn.Click += (s, e) =>
                    {
                        if ((s as Button)?.Tag is BuildFlagItem bf)
                        {
                            if (bf.Parameters == null || !bf.Parameters.Any()) return;
                            var editor = new ParametersEditorWindow(bf.Parameters, bf.FlagName ?? bf.Key);
                            editor.ShowDialog();
                        }
                    };
                    Grid.SetRow(btn, r);
                    Grid.SetColumn(btn, 4);
                    if (item.Parameters.Any())
                    {
                        grid.Children.Add(btn);
                    }

                    r++;
                }

                panel.Children.Add(grid);
                border.Child = panel;

                doc.Blocks.Add(new BlockUIContainer(border));

                // local helper
                void AddText(Grid g, int row, int col, string text, System.Windows.FontWeight? weight = null)
                {
                    var tb = new TextBlock(new Run(text)) { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 2, 4, 2) };
                    if (weight.HasValue) tb.FontWeight = weight.Value;
                    Grid.SetRow(tb, row); Grid.SetColumn(tb, col);
                    g.Children.Add(tb);
                }
            }




            docView.Document = doc;
        }

        private void EditSelectedParameters_Click(object sender, RoutedEventArgs e)
        {
            if (FlagsDataGrid.SelectedItem is BuildFlagItem item)
            {
                if (item.Parameters == null || !item.Parameters.Any())
                {
                    MessageBox.Show("Selected flag has no parameters.", "No Parameters", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var editor = new ParametersEditorWindow(item.Parameters, item.FlagName ?? item.Key);
                var res = editor.ShowDialog();
                if (res == true)
                {
                    // parameters edited in-place
                }
            }
            else
            {
                MessageBox.Show("No flag selected. Select a flag in the grid and try again.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void UpdateGG_Click(object sender, RoutedEventArgs e)
        {
            // Disable button and show status
            updateGGButton.IsEnabled = false;
            statusText.Text = "⏳ Downloading GUI-Generic repository...";
            statusText.Visibility = Visibility.Visible;

            try
            {
                _repositoryPath = await _gitHubRepoDownloader.DownloadRepositoryAsync(
                    owner: "rkalwak",
                    repo: "GUI-Generic",
                    destinationRoot: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "repo"),
                    destinationSubdir: "gg",
                    branch: "master",
                    cancellationToken: CancellationToken.None);

                // Success status
                statusText.Text = "✓ Repository updated successfully!";
                statusText.Foreground = System.Windows.Media.Brushes.Green;

                // Hide status after 3 seconds
                await Task.Delay(3000);
                statusText.Visibility = Visibility.Collapsed;
                statusText.Foreground = System.Windows.Media.Brushes.DarkBlue;

                MessageBox.Show("Repository updated successfully!", "Update Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // Error status
                statusText.Text = "✗ Repository update failed";
                statusText.Foreground = System.Windows.Media.Brushes.Red;

                // Hide status after 3 seconds
                await Task.Delay(3000);
                statusText.Visibility = Visibility.Collapsed;
                statusText.Foreground = System.Windows.Media.Brushes.DarkBlue;

                MessageBox.Show($"Error updating repository: {ex.Message}", "Update Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Re-enable button
                updateGGButton.IsEnabled = true;
            }
        }

        private void SetGroupFlags(IGrouping<string, BuildFlagItem> group, bool value, CheckBox source)
        {
            foreach (var item in group)
            {
                item.IsEnabled = value;
            }

            // Update checkbox state explicitly after changes
            UpdateGroupCheckBoxState(source, group);
        }

        private void UpdateGroupCheckBoxState(CheckBox checkBox, IGrouping<string, BuildFlagItem> group)
        {
            int total = group.Count();
            int on = group.Count(i => i.IsEnabled);

            if (on == 0)
                checkBox.IsChecked = false;
            else if (on == total)
                checkBox.IsChecked = true;
            else
                checkBox.IsChecked = null; // Indeterminate
        }

        private async void CompileSelected_Click(object sender, RoutedEventArgs e)
        {
            // Check if GUI-Generic repository exists and is not empty
            if (string.IsNullOrEmpty(_repositoryPath) || !Directory.Exists(_repositoryPath))
            {
                MessageBox.Show(
                    "GUI-Generic repository not found!\n\n" +
                    "Please click '1. Update Gui-Generic' button first to download the repository.\n\n" +
                    "The repository is required for firmware compilation.",
                    "Repository Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Check if the repository directory is empty
            if (!Directory.EnumerateFileSystemEntries(_repositoryPath).Any())
            {
                MessageBox.Show(
                    "GUI-Generic repository directory is empty!\n\n" +
                    $"Repository path: {_repositoryPath}\n\n" +
                    "Please click '1. Update Gui-Generic' button to download the repository.",
                    "Empty Repository",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Verify essential files exist in the repository
            var platformioIniPath = Path.Combine(_repositoryPath, "platformio.ini");
            if (!File.Exists(platformioIniPath))
            {
                MessageBox.Show(
                    "GUI-Generic repository appears to be incomplete or corrupted.\n\n" +
                    $"Missing file: platformio.ini\n" +
                    $"Repository path: {_repositoryPath}\n\n" +
                    "Please click '1. Update Gui-Generic' button to re-download the repository.",
                    "Incomplete Repository",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            List<BuildFlagItem> selectedFlags = AllBuildFlags.Where(f => f.IsEnabled).ToList();
            if (!selectedFlags.Any())
            {
                MessageBox.Show("No flags selected. Enable some flags before compiling.", "No Flags", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Get deploy and backup checkbox states
            bool shouldDeploy = deployCheckBox?.IsChecked ?? true;
            bool shouldBackup = backupCheckBox?.IsChecked ?? true;
            bool shouldEraseFlash = eraseFlashCheckBox?.IsChecked ?? false;

            // Validate COM port selection only if deploying
            if (shouldDeploy)
            {
                var selectedComPort = (comPortSelector?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(selectedComPort) || selectedComPort.Equals("None", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(
                        "Please select a COM port before compiling with deployment.\n\n" +
                        "The firmware needs to be uploaded to a device connected via COM port.\n" +
                        "Use '2. Check Device' to auto-detect, or manually select a COM port.\n\n" +
                        "Alternatively, uncheck 'Deploy to device' to compile only without uploading.",
                        "COM Port Required",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            // Disable compile button
            compileButton.IsEnabled = false;

            // Show status indicator
            statusText.Text = "⏳ Compiling firmware...";
            statusText.Foreground = System.Windows.Media.Brushes.Black;
            statusText.Visibility = Visibility.Visible;

            // Show and initialize progress UI for elapsed time tracking
            if (compileProgressBar != null && compileTimerText != null)
            {
                compileProgressBar.Value = 0;
                compileProgressBar.Visibility = Visibility.Visible;
                compileTimerText.Text = "00:00";
                compileTimerText.Visibility = Visibility.Visible;
            }

            // Cancel any existing timer
            _compileTimerCts?.Cancel();
            _compileTimerCts = new CancellationTokenSource();
            var timerToken = _compileTimerCts.Token;

            // Start elapsed time counter task
            var timerTask = Task.Run(async () =>
            {
                int elapsed = 0;
                try
                {
                    while (!timerToken.IsCancellationRequested)
                    {
                        await Task.Delay(1000, timerToken);
                        elapsed++;
                        Dispatcher.Invoke(() =>
                        {
                            if (compileProgressBar != null && compileTimerText != null)
                            {
                                // Update progress bar (capped at maximum)
                                compileProgressBar.Value = Math.Min(elapsed, compileProgressBar.Maximum);
                                
                                // Display elapsed time
                                compileTimerText.Text = TimeSpan.FromSeconds(elapsed).ToString(@"mm\:ss");
                            }
                        });
                    }
                }
                catch (TaskCanceledException) { }
            }, timerToken);

            try
            {

                var ggRequest = new CompileRequest
                {
                    BuildFlags = selectedFlags,
                    Platform = (boardSelector?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty,
                    ProjectPath = Path.Combine(_repositoryPath, "src"),
                    ProjectDirectory = _repositoryPath,
                    LibrariesPath = Path.Combine(_repositoryPath, "lib"),
                    PortCom = (comPortSelector?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty,
                    ShouldDeploy = shouldDeploy,
                    ShouldBackup = shouldBackup,
                    ShouldEraseFlash = shouldEraseFlash
                };
                var handler = new PlatformioCliHandler();
                ICompileHandler compiler = new PlatformioCliHandler();
                var result = await compiler.Handle(ggRequest, CancellationToken.None);

                // Stop timer
                _compileTimerCts?.Cancel();
                try
                {
                    await timerTask;
                }
                catch (TaskCanceledException) { }

                // Hide progress UI
                if (compileProgressBar != null && compileTimerText != null)
                {
                    compileTimerText.Visibility = Visibility.Collapsed;
                    compileProgressBar.Visibility = Visibility.Collapsed;
                }

                if (result.IsSuccessful)
                {
                    // Success status
                    statusText.Text = "✓ Compilation successful!";
                    statusText.Foreground = System.Windows.Media.Brushes.Green;

                    // Hide status after 3 seconds
                    Task.Run(async () =>
                    {
                        await Task.Delay(3000);
                        Dispatcher.Invoke(() =>
                        {
                            statusText.Visibility = Visibility.Collapsed;
                            statusText.Foreground = System.Windows.Media.Brushes.Black;
                        });
                    });

                    // Generate encoded configuration string
                    var encodedConfig = BuildConfigurationHasher.EncodeOptions(selectedFlags);

                    // Save configuration with hash
                    try
                    {
                        var platform = (boardSelector?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;
                        var comPort = (comPortSelector?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;
                        _configManager.SaveConfiguration(
                            result.HashOfOptions ?? string.Empty,
                            selectedFlags,
                            configName: null,
                            platform: platform,
                            comPort: comPort);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to save configuration: {ex.Message}");
                    }

                    // Show success results with encoded configuration string and backup path
                    var resultsWindow = new CompilationResultsWindow(
                        encodedConfig,
                        true,
                        result.BackupFilePath)
                    {
                        Owner = this
                    };
                    resultsWindow.ShowDialog();
                }

                else
                {
                    // Error status
                    statusText.Text = "✗ Compilation failed";
                    statusText.Foreground = System.Windows.Media.Brushes.Red;

                    // Hide status after 3 seconds
                    Task.Run(async () =>
                    {
                        await Task.Delay(3000);
                        Dispatcher.Invoke(() =>
                        {
                            statusText.Visibility = Visibility.Collapsed;
                            statusText.Foreground = System.Windows.Media.Brushes.Black;
                        });
                    });

                    // Show detailed logs in modal window
                    var resultsWindow = new CompilationResultsWindow(result.Logs)
                    {
                        Owner = this
                    };
                    resultsWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                // Stop timer on error
                _compileTimerCts?.Cancel();

                // Hide progress UI
                if (compileProgressBar != null && compileTimerText != null)
                {
                    compileProgressBar.Visibility = Visibility.Collapsed;
                    compileTimerText.Visibility = Visibility.Collapsed;
                }

                // Error status
                statusText.Text = "✗ Compilation error";
                statusText.Foreground = System.Windows.Media.Brushes.Red;

                // Hide status after 3 seconds
                Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    Dispatcher.Invoke(() =>
                    {
                        statusText.Visibility = Visibility.Collapsed;
                        statusText.Foreground = System.Windows.Media.Brushes.Black;
                    });
                });

                MessageBox.Show($"Compilation error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Re-enable compile button
                compileButton.IsEnabled = true;
            }

        }

        private async void CheckConnectedDevice_Click(object sender, RoutedEventArgs e)
        {
            _logger.Information("=== Device Detection Started ===");

            // Disable button and show status
            checkDeviceButton.IsEnabled = false;
            statusText.Text = "⏳ Detecting device...";
            statusText.Visibility = Visibility.Visible;
            statusText.Foreground = System.Windows.Media.Brushes.DarkBlue;

            await Task.Run(async () =>
            {
                try
                {
                    _logger.Debug("Detecting COM port...");
                    var port = _deviceDetector.DetectCOMPortWithUsbBridge();

                    if (port != null)
                    {
                        _logger.Information("COM port detected: {Port}", port);
                    }
                    else
                    {
                        _logger.Warning("No COM port detected");
                    }

                    EspInfo deviceModel = null;
                    if (port != null)
                    {
                        _logger.Debug("Detecting ESP model on port {Port}...", port);
                        deviceModel = await _deviceDetector.DetectEspModelAsync(port);

                        if (deviceModel != null)
                        {
                            _logger.Information("Device detected: ChipType={ChipType}, Model={Model}, FlashSize={FlashSize}, MAC={Mac}",
                                deviceModel.ChipType, deviceModel.Model, deviceModel.FlashSize, deviceModel.Mac);
                        }
                    }

                    Dispatcher.Invoke(() =>
                    {
                        if (!string.IsNullOrWhiteSpace(port))
                        {
                            comPortSelector.SelectedItem = comPortSelector.Items.OfType<ComboBoxItem>().FirstOrDefault(ci => (ci.Tag as string) == port || (ci.Content as string) == port);
                            _logger.Debug("COM port selector updated to: {Port}", port);

                            // If we detected a device model, try to select matching board in the selector

                            var chip = deviceModel?.ChipType ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(chip) && boardSelector != null)
                            {
                                string chipLower = chip.ToLowerInvariant();
                                string selectedTag = null;
                                if (chipLower.Contains("c6") || chipLower.Contains("c-6"))
                                    selectedTag = "GUI_Generic_ESP32C6";
                                else if (chipLower.Contains("c3") || chipLower.Contains("c-3"))
                                    selectedTag = "GUI_Generic_ESP32C3";
                                else if (chipLower.Contains("s3") || chipLower.Contains("s-3"))
                                    selectedTag = "GUI_Generic_ESP32S3";
                                else if (chipLower.Contains("8266") || chipLower.Contains("esp8266"))
                                    selectedTag = "GUI_Generic_ESP8266";
                                else if (chipLower.Contains("esp32") || chipLower.Contains("esp32"))
                                    selectedTag = "GUI_Generic_ESP32";

                                if (selectedTag != null)
                                {
                                    var match = boardSelector.Items.OfType<ComboBoxItem>().FirstOrDefault(ci => (ci.Tag as string) == selectedTag);
                                    if (match != null)
                                    {
                                        boardSelector.SelectedItem = match;
                                        _logger.Information("Board selector updated to: {BoardTag}", selectedTag);
                                    }
                                }

                                // Set flash size selector if available

                                var fs = deviceModel?.FlashSize ?? string.Empty;
                                if (!string.IsNullOrWhiteSpace(fs) && flashSizeSelector != null)
                                {
                                    // Normalize like '16MB' or '16M'
                                    var normalized = fs.Trim().ToUpperInvariant();
                                    // Try to match beginning of string
                                    var fmatch = flashSizeSelector.Items.OfType<ComboBoxItem>().FirstOrDefault(ci => normalized.Contains((ci.Tag as string) ?? (ci.Content as string)));
                                    if (fmatch != null)
                                    {
                                        flashSizeSelector.SelectedItem = fmatch;
                                        _logger.Information("Flash size selector updated to: {FlashSize}", fs);
                                    }
                                }

                            }

                            // Success status
                            statusText.Text = $"✓ Device detected: {chip} on {port}";
                            statusText.Foreground = System.Windows.Media.Brushes.Green;

                            // Hide status after 3 seconds
                            Task.Run(async () =>
                            {
                                await Task.Delay(3000);
                                Dispatcher.Invoke(() =>
                                {
                                    statusText.Visibility = Visibility.Collapsed;
                                    statusText.Foreground = System.Windows.Media.Brushes.DarkBlue;
                                });
                            });
                        }
                        else
                        {
                            _logger.Warning("Device detection completed but no port found");

                            // No device status
                            statusText.Text = "✗ No device detected";
                            statusText.Foreground = System.Windows.Media.Brushes.OrangeRed;

                            // Hide status after 3 seconds
                            Task.Run(async () =>
                            {
                                await Task.Delay(3000);
                                Dispatcher.Invoke(() =>
                                {
                                    statusText.Visibility = Visibility.Collapsed;
                                    statusText.Foreground = System.Windows.Media.Brushes.DarkBlue;
                                });
                            });

                            MessageBox.Show(
                                "No ESP device detected.\n\n" +
                                "Please ensure:\n" +
                                "• Device is connected via USB\n" +
                                "• USB drivers are installed\n" +
                                "• Device is powered on",
                                "Device Not Found",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }

                        // Re-enable button
                        checkDeviceButton.IsEnabled = true;

                        _logger.Information("=== Device Detection Completed ===");
                    });


                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error during device detection");
                    Dispatcher.Invoke(() =>
                    {
                        // Error status
                        statusText.Text = "✗ Device detection error";
                        statusText.Foreground = System.Windows.Media.Brushes.Red;

                        // Hide status after 3 seconds
                        Task.Run(async () =>
                        {
                            await Task.Delay(3000);
                            Dispatcher.Invoke(() =>
                            {
                                statusText.Visibility = Visibility.Collapsed;
                                statusText.Foreground = System.Windows.Media.Brushes.DarkBlue;
                            });
                        });

                        // Re-enable button
                        checkDeviceButton.IsEnabled = true;

                        // Error during device detection - could log or show message
                        MessageBox.Show(
                            $"Device detection error: {ex.Message}\n\n" +
                            $"Check the logs for more details.",
                            "Detection Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    });
                }
            });
        }

        private void EditParameters_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is BuildFlagItem item)
            {
                if (item.Parameters == null || !item.Parameters.Any())
                {
                    MessageBox.Show("This flag has no parameters.", "No Parameters", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var editor = new ParametersEditorWindow(item.Parameters, item.FlagName ?? item.Key);
                var res = editor.ShowDialog();
                if (res == true)
                {
                    // Parameters modified in-place; nothing else required.
                }
            }
        }

        private void LoadConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var managerWindow = new ConfigurationManagerWindow(AllBuildFlags)
                {
                    Owner = this
                };

                if (managerWindow.ShowDialog() == true && managerWindow.SelectedConfiguration != null)
                {
                    LoadConfiguration(managerWindow.SelectedConfiguration);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening configuration manager: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadConfiguration(SavedBuildConfiguration config)
        {
            // Check if this is a placeholder configuration (no flags)
            if (config.EnabledFlagKeys == null || !config.EnabledFlagKeys.Any())
            {
                MessageBox.Show(
                    $"The configuration '{config.ConfigurationName}' has no saved flags.\n\n" +
                    "Please manually select the build flags.",
                    "Manual Configuration Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Disable all flags first
            foreach (var flag in AllBuildFlags)
            {
                flag.IsEnabled = false;
            }

            // Enable flags from the configuration
            foreach (var flagKey in config.EnabledFlagKeys)
            {
                var flag = AllBuildFlags.FirstOrDefault(f =>
                    string.Equals(f.Key, flagKey, StringComparison.OrdinalIgnoreCase));

                if (flag != null)
                {
                    flag.IsEnabled = true;
                }
            }

            // Restore parameter values if available
            if (config.BuildFlagsParameters != null && config.BuildFlagsParameters.Any())
            {
                foreach (var flagParams in config.BuildFlagsParameters)
                {
                    var flag = AllBuildFlags.FirstOrDefault(f =>
                        string.Equals(f.Key, flagParams.Key, StringComparison.OrdinalIgnoreCase));

                    if (flag != null && flag.Parameters != null)
                    {
                        foreach (var paramValue in flagParams.Value)
                        {
                            var parameter = flag.Parameters.FirstOrDefault(p =>
                                string.Equals(p.Identifier, paramValue.Key, StringComparison.OrdinalIgnoreCase));

                            if (parameter != null)
                            {
                                parameter.Value = paramValue.Value;
                            }
                        }
                    }
                }
            }

            // Restore platform selection if available
            if (!string.IsNullOrEmpty(config.Platform) && boardSelector != null)
            {
                var platformItem = boardSelector.Items.OfType<ComboBoxItem>()
                    .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), config.Platform, StringComparison.OrdinalIgnoreCase));

                if (platformItem != null)
                {
                    boardSelector.SelectedItem = platformItem;
                }
            }

            // Restore COM port selection if available
            if (!string.IsNullOrEmpty(config.ComPort) && comPortSelector != null)
            {
                var comPortItem = comPortSelector.Items.OfType<ComboBoxItem>()
                    .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), config.ComPort, StringComparison.OrdinalIgnoreCase));

                if (comPortItem != null)
                {
                    comPortSelector.SelectedItem = comPortItem;
                }
            }

            MessageBox.Show(
                $"Configuration '{config.ConfigurationName}' loaded successfully!\n\n" +
                $"Platform: {config.Platform}\n" +
                $"COM Port: {config.ComPort}\n" +
                $"Enabled flags: {config.EnabledFlagKeys.Count}",
                "Configuration Loaded",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}