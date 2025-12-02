using System.ComponentModel;
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
        string _repositoryPath = string.Empty;

        // UI fields inside FlowDocument to show detected device
        private TextBlock devicePortText;
        private TextBlock deviceModelText;
        private ComboBox boardSelector;

        public MainWindow()
        {
            InitializeComponent();
            AllBuildFlags = new List<BuildFlagItem>();

            InitializeBuildFlags();
            FlagsDataGrid.ItemsSource = AllBuildFlags;
          
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
                var button = deserialized.Sections["CONTROL"].Flags["SUPLA_BUTTON"];
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
                        flagItem.Value.SectionOrder = sectionItem.Value.Order;
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
            var portLabel = new TextBlock(new Run("Port:")) { FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
            devicePortText = new TextBlock(new Run("Not checked")) { Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            var modelLabel = new TextBlock(new Run("Model:")) { FontWeight = FontWeights.SemiBold, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            deviceModelText = new TextBlock(new Run("Unknown")) { Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };

            // Board selector ComboBox
            boardSelector = new ComboBox { Width = 220, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP32 (default)", Tag = "GUI_Generic_ESP32", IsSelected = true });
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP32-C3", Tag = "GUI_Generic_ESP32C3" });
            boardSelector.Items.Add(new ComboBoxItem { Content = "ESP8266", Tag = "GUI_Generic_ESP8266" });

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
            devicePanel.Children.Add(portLabel);
            devicePanel.Children.Add(devicePortText);
            devicePanel.Children.Add(boardSelector);
            devicePanel.Children.Add(modelLabel);
            devicePanel.Children.Add(deviceModelText);
            DockPanel.SetDock(checkBtn, Dock.Right);
            devicePanel.Children.Add(checkBtn);


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

                var trg = new TableRowGroup();
                table.RowGroups.Add(trg);

                // Header row
                var headerRow = new TableRow();
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Enabled")) { FontWeight = FontWeights.SemiBold }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Key")) { FontWeight = FontWeights.SemiBold }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Name")) { FontWeight = FontWeights.SemiBold }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Description")) { FontWeight = FontWeights.SemiBold }));
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

                    trg.Rows.Add(row);
                }

                doc.Blocks.Add(table);
            }





            docView.Document = doc;
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
            MessageBox.Show("Compilation started. This may take several minutes.", "Compiling", MessageBoxButton.OK, MessageBoxImage.Information);
            var selectedFlags = AllBuildFlags
                .Where(f => f.IsEnabled)
                .Select(f => f.Key)
                .Where(k => !string.IsNullOrEmpty(k))
                .ToList();

            if (!selectedFlags.Any())
            {
                MessageBox.Show("No flags selected. Enable some flags before compiling.", "No Flags", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }


            try
            {
                var ggRequest = new CompileRequest
                {
                    BuildFlags = selectedFlags,
                    Platform = (boardSelector?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "GUI_Generic_ESP32",
                    ProjectPath = Path.Combine(_repositoryPath,"src"),
                    ProjectDirectory = _repositoryPath,
                    LibrariesPath = Path.Combine(_repositoryPath,"lib"),
                    PortCom = devicePortText.Text
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
                    MessageBox.Show($"Compilation finished with log\r\n {result.Logs}.", "Compilation Finished", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Compilation error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private async void CheckConnectedDevice_Click(object? sender, RoutedEventArgs e)
        {
            if (devicePortText != null) devicePortText.Text = "Checking...";
            if (deviceModelText != null) deviceModelText.Text = string.Empty;

            await Task.Run(async () =>
            {
                try
                {

                    var deviceDetector = new DeviceDetector(new EsptoolWrapper());
                    var port = deviceDetector.DetectCOMPort();
                    EspInfo deviceModel = null;
                    if (port != null)
                    {
                        deviceModel = await deviceDetector.DetectEspModelAsync(port);
                    }
                    Dispatcher.Invoke(() =>
                    {
                        if (!string.IsNullOrWhiteSpace(port))
                        {
                            devicePortText.Text = port.Length > 0 ? port.Trim() : "No device";
                            deviceModelText.Text = deviceModel?.ChipType?.Trim();
                        }
                        else
                        {
                            devicePortText.Text = "No device detected";
                            deviceModelText.Text = string.Empty;
                        }
                    });


                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        devicePortText.Text = "Error";
                        deviceModelText.Text = ex.Message;
                    });
                }
            });
        }
    }

    public class BuildFlagItem : INotifyPropertyChanged
    {
        private bool _isEnabled;

        public string Key { set; get; }
        [JsonProperty("name")]
        public string FlagName { get; set; }
        [JsonProperty("desc")]
        public string Description { get; set; }
        public string Section { get; set; }
        [JsonProperty("defOn")]
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
                }
            }
        }
        //"[opcja] tablica SUPLA_OPTION, których włączenie automatycznie włącza daną opcję"
        [JsonProperty("depOn")]
        public List<string>? DependenciesToEnable { get; set; }

        //"[opcja] tablica SULPA_OPTION, które zostaną wyłączone po włączeniu danej opcji",
        [JsonProperty("depRel")]
        public List<string>? DepRel { get; set; }

        // "[opcja] tablica SULPA_OPTION, które zostaną włączone po włączeniu danej opcji",
        [JsonProperty("depOpt")]
        public List<string>? DependenciesOptional { get; set; }

        //"[opcja] tablica SUPLA_OPTION, których wyłączenie nie pozwala włączyć danej opcji",
        [JsonProperty("depOff")]
        public List<string>? DependenciesToDisable { get; set; }
        public int SectionOrder { get; internal set; }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
    public class SectionInfo
    {
        public string Name { get; set; }

        public int Order { get; set; }
        [JsonProperty("Flags")]
        public Dictionary<string, BuildFlagItem> Flags { get; set; } = new Dictionary<string, BuildFlagItem>();
    }

    public class BuilderConfig
    {

        [JsonProperty("Sections")]
        public Dictionary<string, SectionInfo> Sections { get; set; } = new Dictionary<string, SectionInfo>();

        [JsonProperty("version")]
        public string Version { get; set; }
    }

}