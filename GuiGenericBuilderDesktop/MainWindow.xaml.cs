using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace GuiGenericBuilderDesktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<BuildFlagItem> AllBuildFlags { get; set; }

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
                var deserialized = JsonSerializer.Deserialize<BuilderConfig>(jsonContent);

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

                // Second pass: evaluate dependencies for each flag and assign the computed enabled state
                var memo = new Dictionary<string, bool>();
                foreach (var flag in AllBuildFlags)
                {
                    flag.IsEnabled = FindOnDependencies(flag, memo, new HashSet<string>());
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
            if (flag.Dependencies == null || flag.Dependencies.Length == 0)
            {
                memo[id] = flag.IsEnabled;
                return memo[id];
            }

            visiting.Add(id);

            foreach (var rawDep in flag.Dependencies)
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

            var grouped = AllBuildFlags.GroupBy(f => f.Section).ToList();

            foreach (var group in grouped)
            {
                // Section header
                var headerPara = new Paragraph();
                var headerRun = new Run(group.Key + $" ({group.Count()})") { FontWeight = FontWeights.Bold, FontSize = 14 };
                headerPara.Inlines.Add(headerRun);
                headerPara.Margin = new Thickness(0, 8, 0, 4);
                doc.Blocks.Add(headerPara);

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
    }

    public class BuildFlagItem : INotifyPropertyChanged
    {
        private bool _isEnabled;

        public string Key { set; get; }
        [JsonPropertyName("name")]
        public string FlagName { get; set; }
        [JsonPropertyName("desc")]
        public string Description { get; set; }
        public string Section { get; set; }
        [JsonPropertyName("defOn")]
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

        [JsonPropertyName("depRel")]
        public string[] Dependencies { get; set; }
        public int SectionOrder { get; internal set; }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
    public class SectionInfo
    {
        public string Name { get; set; }

        public int Order { get; set; }
        [JsonPropertyName("Flags")]
        public Dictionary<string, BuildFlagItem> Flags { get; set; } = new Dictionary<string, BuildFlagItem>();
    }

    public class BuilderConfig
    {

        [JsonPropertyName("Sections")]
        public Dictionary<string, SectionInfo> Sections { get; set; } = new Dictionary<string, SectionInfo>();

        [JsonPropertyName("version")]
        public string Version { get; set; }
    }
}