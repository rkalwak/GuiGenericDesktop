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
        private TextBlock compileCountdownText;
        private CheckBox deployCheckBox;
        private CancellationTokenSource _compileCountdownCts;
        private readonly ILogger _logger;

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
            //_repositoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "repo", "gg");
            _repositoryPath = @"c:\repozytoria\platformio\GUI-Generic";
            
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

            var updateGGButton = new Button
            {
                Content = "1. Update Gui-Generic",
                Width = 140,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(4)
            };
            updateGGButton.Click += UpdateGG_Click;
         

            // Load Configuration button
           


            var checkBtn = new Button { Content = "2. Check Device", Width = 120, Height = 28, Margin = new Thickness(8, 0, 0, 0) };
            checkBtn.Click += CheckConnectedDevice_Click;
            
            // Progress bar and countdown for compile operation
            compileProgressBar = new ProgressBar 
            { 
                Width = 200, 
                Height = 20, 
                Margin = new Thickness(8, 0, 4, 0), 
                VerticalAlignment = VerticalAlignment.Center,
                Minimum = 0,
                Maximum = 60,
                Value = 60
            };
            
            compileCountdownText = new TextBlock 
            { 
                Text = string.Empty, 
                Margin = new Thickness(4, 0, 8, 0), 
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.SemiBold,
            };
            
            var compileButton = new Button
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
            
            // Progress bar and countdown (these will be hidden initially)
            devicePanel.Children.Add(compileCountdownText);
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


            DockPanel.SetDock(compileButton, Dock.Right);
            devicePanel.Children.Add(compileButton);
            DockPanel.SetDock(deployCheckBox, Dock.Right);
            devicePanel.Children.Add(deployCheckBox);
            
            
            DockPanel.SetDock(checkBtn, Dock.Right);
            devicePanel.Children.Add(checkBtn);
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
                var headerPanel = new DockPanel { 
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
            try
            {
                _repositoryPath = await _gitHubRepoDownloader.DownloadRepositoryAsync(
                    owner: "rkalwak",
                    repo: "GUI-Generic",
                    destinationRoot: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "repo"),
                    destinationSubdir: "gg",
                    branch: "master",
                    cancellationToken: CancellationToken.None);
                
                MessageBox.Show("Repository updated successfully!", "Update Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating repository: {ex.Message}", "Update Failed", MessageBoxButton.OK, MessageBoxImage.Error);
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

            List<BuildFlagItem> selectedFlags = AllBuildFlags.Where(f => f.IsEnabled).ToList();
            if (!selectedFlags.Any())
            {
                MessageBox.Show("No flags selected. Enable some flags before compiling.", "No Flags", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Get deploy checkbox state
            bool shouldDeploy = deployCheckBox?.IsChecked ?? true;

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
            
            // Show and initialize progress UI
            if (compileProgressBar != null && compileCountdownText != null)
            {
                compileProgressBar.Value = 90;
                compileProgressBar.Visibility = Visibility.Visible;
                compileCountdownText.Text = "01:30";
                compileCountdownText.Visibility = Visibility.Visible;
            }
            
            // Cancel any existing countdown
            _compileCountdownCts?.Cancel();
            _compileCountdownCts = new CancellationTokenSource();
            var countdownToken = _compileCountdownCts.Token;

            // Start 60-second countdown task
            var countdownTask = Task.Run(async () =>
            {
                int remaining = 90;
                try
                {
                    while (remaining > 0 && !countdownToken.IsCancellationRequested)
                    {
                        await Task.Delay(1000, countdownToken);
                        remaining--;
                        Dispatcher.Invoke(() =>
                        {
                            if (compileProgressBar != null && compileCountdownText != null)
                            {
                                compileProgressBar.Value = remaining;
                                compileCountdownText.Text = TimeSpan.FromSeconds(remaining).ToString(@"mm\:ss");
                            }
                        });
                    }
                }
                catch (TaskCanceledException) { }
            }, countdownToken);

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
                        ShouldDeploy = shouldDeploy
                    };
                    var handler = new PlatformioCliHandler();
                    ICompileHandler compiler = new PlatformioCliHandler();
                    var result = await compiler.Handle(ggRequest, CancellationToken.None);
          
                // Stop countdown
                _compileCountdownCts?.Cancel();
                try
                {
                    await countdownTask;
                }
                catch (TaskCanceledException) { }

                // Hide progress UI
                if (compileProgressBar != null && compileCountdownText != null)
                {
                    compileCountdownText.Visibility = Visibility.Collapsed;
                    compileProgressBar.Visibility = Visibility.Collapsed;
                }

                if (result.IsSuccessful)
                {
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
                    // Show success results with hash
                    var resultsWindow = new CompilationResultsWindow(result.HashOfOptions ?? string.Empty, true)
                    {
                        Owner = this
                    };
                    resultsWindow.ShowDialog();
                }

                else
                {
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
                // Stop countdown on error
                _compileCountdownCts?.Cancel();
                
                // Hide progress UI
                if (compileProgressBar != null && compileCountdownText != null)
                {
                    compileProgressBar.Visibility = Visibility.Collapsed;
                    compileCountdownText.Visibility = Visibility.Collapsed;
                }
                
                MessageBox.Show($"Compilation error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private async void CheckConnectedDevice_Click(object sender, RoutedEventArgs e)
        {
            _logger.Information("=== Device Detection Started ===");
            
            await Task.Run(async () =>
            {
                try
                {
                    _logger.Debug("Detecting COM port...");
                    var port = _deviceDetector.DetectCOMPort();
                    
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
                        }
                        else
                        {
                            _logger.Warning("Device detection completed but no port found");
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
                        
                        _logger.Information("=== Device Detection Completed ===");
                    });


                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error during device detection");
                    Dispatcher.Invoke(() =>
                    {
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
                                string.Equals(p.Name, paramValue.Key, StringComparison.OrdinalIgnoreCase));
							
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