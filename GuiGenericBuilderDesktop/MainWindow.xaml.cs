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
        private TextBlock deviceModelText;
        private ComboBox boardSelector;
        private ComboBox comPortSelector;
        private ComboBox flashSizeSelector;

        public MainWindow()
        {
            InitializeComponent();
            AllBuildFlags = new List<BuildFlagItem>();

            InitializeBuildFlags();

            // Add the Parameters column dynamically so it's visible in the grid
            AddParametersColumnDynamically();

            FlagsDataGrid.ItemsSource = AllBuildFlags;
            _repositoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "repo", "gg");
            _repositoryPath = @"c:\repozytoria\platformio\GUI-Generic";
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

        private bool FindOnDependencies(BuildFlagItem flag, Dictionary<string, bool>? memo = null, HashSet<string>? visiting = null)
        {
            memo ??= new Dictionary<string, bool>();
            visiting ??= new HashSet<string>();

            if (flag == null)
                return false;

            // Key to identify flags uniquely for memoization/visiting (case-insensitive)
            string id = $"{flag.Section}::{flag.Key}".ToLowerInvariant();

            // If already computed, return cached result
            if (memo.TryGetValue(id, out var cached))
                return cached;

            // If currently visiting, we found a cycle -> treat as unsatisfied
            if (visiting.Contains(id))
            {
                memo[id] = false;
                return false;
            }

            // If no dependencies declared, use the default value loaded from JSON (`defOn`)
            if (flag.DependenciesToEnable == null || flag.DependenciesToEnable.Any())
            {
                memo[id] = flag.IsEnabled;
                return memo[id];
            }

            visiting.Add(id);

            foreach (var rawDep in flag.DependenciesToEnable)
            {
                if (string.IsNullOrWhiteSpace(rawDep))
                {
                    memo[id] = false;
                    visiting.Remove(id);
                    return false;
                }

                var dep = rawDep.Trim();
                string depSection = null;
                string depKey = null;

                // Support separators: ':', '/', '.'
                int sepIndex = dep.IndexOf(':');
                if (sepIndex < 0) sepIndex = dep.IndexOf('/');
                if (sepIndex < 0) sepIndex = dep.IndexOf('.');
                if (sepIndex >= 0)
                {
                    depSection = dep.Substring(0, sepIndex).Trim();
                    depKey = dep.Substring(sepIndex + 1).Trim();
                }
                else
                {
                    depKey = dep;
                }

                // Find the dependency flag in AllBuildFlags
                BuildFlagItem? depFlag = null;
                if (!string.IsNullOrEmpty(depSection))
                {
                    depFlag = AllBuildFlags.FirstOrDefault(f =>
                        string.Equals(f.Section, depSection, System.StringComparison.OrdinalIgnoreCase)
                        && string.Equals(f.Key, depKey, System.StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    // If no section specified, match by key (first match)
                    depFlag = AllBuildFlags.FirstOrDefault(f =>
                        string.Equals(f.Key, depKey, System.StringComparison.OrdinalIgnoreCase));
                }

                if (depFlag == null)
                {
                    // Missing dependency -> unsatisfied
                    memo[id] = false;
                    visiting.Remove(id);
                    return false;
                }

                // Recursively evaluate dependency
                bool depSatisfied = FindOnDependencies(depFlag, memo, visiting);
                if (!depSatisfied)
                {
                    memo[id] = false;
                    visiting.Remove(id);
                    return false;
                }
            }

            // All dependencies satisfied
            visiting.Remove(id);
            memo[id] = true;
            return true;
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
            boardSelector = new ComboBox { Width = 220, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP32 (default)", Tag = "GUI_Generic_ESP32", IsSelected = true });
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP32-C3", Tag = "GUI_Generic_ESP32C3" });
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP8266", Tag = "GUI_Generic_ESP8266" });
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP32-C6", Tag = "GUI_Generic_ESP32C6" });

            var updateGGButton = new Button
            {
                Content = "Update Gui-Generic",
                Width = 140,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(4)
            };
            updateGGButton.Click += UpdateGG_Click;
            // right-align the button inside the DockPanel
            DockPanel.SetDock(updateGGButton, Dock.Right);
            devicePanel.Children.Add(updateGGButton);


            var checkBtn = new Button { Content = "Check Device", Width = 120, Height = 28, Margin = new Thickness(8, 0, 0, 0) };
            checkBtn.Click += CheckConnectedDevice_Click;

            // COM port selector (COM1..COM10)
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

            devicePanel.Children.Add(comPortSelector);
            devicePanel.Children.Add(boardSelector);
            devicePanel.Children.Add(modelLabel);
            devicePanel.Children.Add(deviceModelText);
            DockPanel.SetDock(checkBtn, Dock.Right);
            devicePanel.Children.Add(checkBtn);

            // Flash size selector
            flashSizeSelector = new ComboBox { Width = 120, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = "Auto", Tag = "AUTO", IsSelected = true });
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = "4MB", Tag = "4MB" });
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = "8MB", Tag = "8MB" });
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = "16MB", Tag = "16MB" });
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = "32MB", Tag = "32MB" });
            flashSizeSelector.Items.Add(new ComboBoxItem { Content = "64MB", Tag = "64MB" });
            devicePanel.Children.Add(flashSizeSelector);

            var compileButton = new Button
            {
                Content = "Compile",
                Width = 140,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(4)
            };
            compileButton.Click += CompileSelected_Click;
            // right-align the button inside the DockPanel
            DockPanel.SetDock(compileButton, Dock.Right);
            devicePanel.Children.Add(compileButton);
            doc.Blocks.Add(new BlockUIContainer(devicePanel));

            var grouped = AllBuildFlags.GroupBy(f => f.Section).ToList();

            foreach (var group in grouped)
            {
                // Section header with a checkbox to toggle all flags in the section
                var headerPanel = new DockPanel { LastChildFill = true, Margin = new Thickness(0, 8, 0, 4) };
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

                // Host headerPanel inside BlockUIContainer
                headerPanel.Children.Add(groupCheckBox);
                headerPanel.Children.Add(titleText);
                var headerContainer = new BlockUIContainer(headerPanel);
                doc.Blocks.Add(headerContainer);

                // Subscribe to item property changes to update group checkbox state
                foreach (var item in group)
                {
                    item.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(BuildFlagItem.IsEnabled))
                        {
                            // Update checkbox state on UI thread
                            Dispatcher.Invoke(() => UpdateGroupCheckBoxState(groupCheckBox, group));
                        }
                    };
                }

                // Table for group's flags
                var table = new Table();
                table.CellSpacing = 4;
                table.Columns.Add(new TableColumn { Width = new GridLength(50) });   // Enabled
                table.Columns.Add(new TableColumn { Width = new GridLength(300) });  // Key
                table.Columns.Add(new TableColumn { Width = new GridLength(300) });  // Name
                table.Columns.Add(new TableColumn { Width = new GridLength(800) }); // Description
                table.Columns.Add(new TableColumn { Width = new GridLength(120) }); // Parameters (button)

                var trg = new TableRowGroup();
                table.RowGroups.Add(trg);

                // Header row
                var headerRow = new TableRow();
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Enabled")) { FontWeight = FontWeights.SemiBold }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Key")) { FontWeight = FontWeights.SemiBold }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Name")) { FontWeight = FontWeights.SemiBold }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Description")) { FontWeight = FontWeights.SemiBold }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Parameters")) { FontWeight = FontWeights.SemiBold }));
                trg.Rows.Add(headerRow);

                foreach (var item in group.OrderBy(i => i.SectionOrder).ThenBy(x => x.Key))
                {
                    var row = new TableRow();

                    // Enabled (CheckBox) using BlockUIContainer to host a control and bind to the item
                    var checkBox = new CheckBox { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2) };
                    var binding = new Binding(nameof(BuildFlagItem.IsEnabled)) { Source = item, Mode = BindingMode.TwoWay };
                    checkBox.SetBinding(CheckBox.IsCheckedProperty, binding);

                    // When user checks the checkbox, validate dependencies. If not satisfied, revert and notify.
                    checkBox.Checked += (s, e) =>
                    {
                        // After the UI and binding updated the item's IsEnabled, verify dependencies
                        bool ok = FindOnDependencies(item);
                        if (!ok)
                        {
                            // Revert change and notify user
                            Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show($"Cannot enable '{item.FlagName ?? item.Key}' because required dependencies are not satisfied.", "Dependency Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                                item.IsEnabled = false;
                            });
                        }
                    };

                    var enabledCell = new TableCell(new BlockUIContainer(checkBox));
                    row.Cells.Add(enabledCell);

                    // Key
                    row.Cells.Add(new TableCell(new Paragraph(new Run(item.Key ?? string.Empty))));

                    // Name
                    row.Cells.Add(new TableCell(new Paragraph(new Run(item.FlagName ?? string.Empty))));

                    // Description
                    row.Cells.Add(new TableCell(new Paragraph(new Run(item.Description ?? string.Empty))));

                    // Parameters button in the row
                    var paramsBtn = new Button { Content = "Params...", Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(2) };
                    paramsBtn.Tag = item;
                    paramsBtn.Click += (s, e) =>
                    {
                        if ((s as Button)?.Tag is BuildFlagItem bf)
                        {
                            if (bf.Parameters == null || !bf.Parameters.Any())
                            {
                                // Do not display editor when there are no parameters
                                return;
                            }

                            var editor = new ParametersEditorWindow(bf.Parameters, bf.FlagName ?? bf.Key);
                            editor.ShowDialog();
                        }
                    };
                    var paramsCell = new TableCell(new BlockUIContainer(paramsBtn));
                    row.Cells.Add(paramsCell);

                    trg.Rows.Add(row);
                }

                doc.Blocks.Add(table);
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

        private void UpdateGG_Click(object sender, RoutedEventArgs e)
        {
            _repositoryPath = _gitHubRepoDownloader.DownloadRepositoryAsync(owner: "rkalwak",
                repo: "GUI-Generic",
                destinationRoot: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "repo"),
                destinationSubdir: "gg",
                branch: "master",
                cancellationToken: CancellationToken.None).GetAwaiter().GetResult();

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

            MessageBox.Show("Compilation started. This may take several minutes.", "Compiling", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    ShouldDeploy = false
                };
                var handler = new PlatformioCliHandler();
                ICompileHandler compiler = new PlatformioCliHandler();
                var result = await compiler.Handle(ggRequest, CancellationToken.None);

                if (result.IsSuccessful)
                {
                    MessageBox.Show($"Compilation finished.", "Compilation Finished", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                else
                {
                    MessageBox.Show($"Compilation finished with errors:\r\n {result.Logs}.", "Compilation Finished", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
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
                                    if (normalized.EndsWith("B")) ; // keep
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