using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Newtonsoft.Json;
using CompilationLib;

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

        // UI fields inside FlowDocument to show detected device
        private TextBlock? deviceModelText;
        private ComboBox? boardSelector;
        private ComboBox? comPortSelector;
        private ComboBox? flashSizeSelector;
        private ProgressBar? compileProgressBar;
        private TextBlock? compileCountdownText;
        private CancellationTokenSource? _compileCountdownCts;

        public MainWindow()
        {
            InitializeComponent();
            AllBuildFlags = new List<BuildFlagItem>();

            InitializeBuildFlags();

            // Add the Parameters column dynamically so it's visible in the grid
            AddParametersColumnDynamically();

            FlagsDataGrid.ItemsSource = AllBuildFlags;
            _repositoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "repo", "gg");
            //_repositoryPath = @"c:\repozytoria\platformio\GUI-Generic";
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
            var modelLabel = new TextBlock(new Run("Model:")) { FontWeight = FontWeights.SemiBold, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            deviceModelText = new TextBlock(new Run("Unknown")) { Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };

            // Board selector ComboBox
            var boardLabel = new TextBlock(new Run("Board:")) { FontWeight = FontWeights.SemiBold, Margin = new Thickness(12, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };
            boardSelector = new ComboBox { Width = 220, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP32 (default)", Tag = "GUI_Generic_ESP32", IsSelected = true });
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP32-C3", Tag = "GUI_Generic_ESP32C3" });
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP8266", Tag = "GUI_Generic_ESP8266" });
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP32-C6", Tag = "GUI_Generic_ESP32C6" });

            var updateGGButton = new Button
            {
                Content = "1. Update Gui-Generic",
                Width = 140,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(4)
            };
            updateGGButton.Click += UpdateGG_Click;
         


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
            for (int i = 1; i <= 10; i++)
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
            devicePanel.Children.Add(modelLabel);
            devicePanel.Children.Add(deviceModelText);
            DockPanel.SetDock(checkBtn, Dock.Right);
            DockPanel.SetDock(compileButton, Dock.Right);
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
            devicePanel.Children.Add(compileCountdownText);
            devicePanel.Children.Add(compileProgressBar);
            devicePanel.Children.Add(compileButton);
            devicePanel.Children.Add(checkBtn);
            // right-align the button inside the DockPanel
            DockPanel.SetDock(updateGGButton, Dock.Right);
            devicePanel.Children.Add(updateGGButton);

            

           
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
                        var deps = DependencyResolver.FindOnDependencies(item, AllBuildFlags);
                        foreach (var d in deps) d.IsEnabled = true;
                    };
                    chk.Unchecked += (s, e) =>
                    {
                        var deps = DependencyResolver.FindOffDependencies(item, AllBuildFlags);
                        foreach (var d in deps) d.IsEnabled = false;
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

            // Show and initialize progress UI
            if (compileProgressBar != null && compileCountdownText != null)
            {
                compileProgressBar.Value = 60;
                compileProgressBar.Visibility = Visibility.Visible;
                compileCountdownText.Text = "01:00";
                compileCountdownText.Visibility = Visibility.Visible;
            }
            
            // Cancel any existing countdown
            _compileCountdownCts?.Cancel();
            _compileCountdownCts = new CancellationTokenSource();
            var countdownToken = _compileCountdownCts.Token;

            // Start 60-second countdown task
            var countdownTask = Task.Run(async () =>
            {
                int remaining = 60;
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
                        ShouldDeploy = true
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
                }

                if (result.IsSuccessful)
                {
                    MessageBox.Show($"Compilation finished. {(ggRequest.ShouldDeploy?" Software deployed.":"")})", "Compilation Finished", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                else
                {
                    // Show detailed logs in modal window
                    var logWindow = new CompilationLogWindow(result.Logs)
                    {
                        Owner = this
                    };
                    logWindow.ShowDialog();
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

        private async void CheckConnectedDevice_Click(object? sender, RoutedEventArgs e)
        {
            if (deviceModelText != null) deviceModelText.Text = string.Empty;

            await Task.Run(async () =>
            {
                try
                {

                    var port = _deviceDetector.DetectCOMPort();
                    EspInfo deviceModel = null;
                    if (port != null)
                    {
                        deviceModel = await _deviceDetector.DetectEspModelAsync(port);
                    }
                    Dispatcher.Invoke(() =>
                    {
                        if (!string.IsNullOrWhiteSpace(port))
                        {
                            deviceModelText?.Text = deviceModel?.ChipType?.Trim();
                            comPortSelector.SelectedItem = comPortSelector.Items.OfType<ComboBoxItem>().FirstOrDefault(ci => (ci.Tag as string) == port || (ci.Content as string) == port);

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
                                else if (chipLower.Contains("8266") || chipLower.Contains("esp8266"))
                                    selectedTag = "GUI_Generic_ESP8266";
                                else if (chipLower.Contains("esp32") || chipLower.Contains("esp32"))
                                    selectedTag = "GUI_Generic_ESP32";

                                if (selectedTag != null)
                                {
                                    var match = boardSelector.Items.OfType<ComboBoxItem>().FirstOrDefault(ci => (ci.Tag as string) == selectedTag);
                                    if (match != null)
                                        boardSelector.SelectedItem = match;
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
                                    }
                                }

                            }
                        }
                    });


                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        deviceModelText?.Text = ex.Message;
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
    }
}