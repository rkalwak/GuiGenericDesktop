using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CompilationLib;
using GuiGenericBuilderDesktop.Localization;

namespace GuiGenericBuilderDesktop
{
    public class LimitSwitchParametersWindow : Window
    {
        private readonly BuildFlagItem _flag;
        private readonly ObservableCollection<LimitSwitchEntryViewModel> _entries;
        private readonly ListView _listView;
        private readonly int _maxLimitSwitches = 20;
        private readonly string _boardName;
        private readonly IReadOnlyList<BoardGpioOption> _gpioOptions;

        public LimitSwitchParametersWindow(BuildFlagItem flag, GlobalSettings globalSettings, string boardName = null)
        {
            _flag = flag ?? throw new ArgumentNullException(nameof(flag));
            _boardName = boardName ?? string.Empty;
            _gpioOptions = globalSettings.GetOptionsForBoard(_boardName);
            _entries = new ObservableCollection<LimitSwitchEntryViewModel>();

            Title = string.Format(LocalizationManager.Get("LimitSwitchesTitle"), _maxLimitSwitches);
            Width = 820;
            Height = 620;
            MinWidth = 640;
            MinHeight = 540;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Brushes.WhiteSmoke;
            SizeChanged += LimitSwitchParametersWindow_SizeChanged;

            var root = new Border
            {
                Background = Brushes.WhiteSmoke,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(8)
            };

            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new TextBlock
            {
                Text = string.Format(LocalizationManager.Get("LimitSwitchesTitle"), _maxLimitSwitches),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(header, 0);
            rootGrid.Children.Add(header);

            var dataContainer = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 0, 8)
            };

            _listView = new ListView
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0),
                Background = Brushes.White,
                MinHeight = 420,
                ItemsSource = _entries,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                SelectionMode = SelectionMode.Single,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var gridView = new GridView
            {
                AllowsColumnReorder = false
            };

            gridView.Columns.Add(new GridViewColumn
            {
                Header = LocalizationManager.Get("Number"),
                Width = 70,
                DisplayMemberBinding = new Binding(nameof(LimitSwitchEntryViewModel.IndexDisplay))
            });

            gridView.Columns.Add(new GridViewColumn
            {
                Header = LocalizationManager.Get("Enabled"),
                Width = 90,
                CellTemplate = CreateCheckBoxTemplate(nameof(LimitSwitchEntryViewModel.IsEnabled))
            });

            gridView.Columns.Add(new GridViewColumn
            {
                Header = LocalizationManager.Get("GPIO"),
                Width = 140,
                CellTemplate = CreateGpioComboBoxTemplate(nameof(LimitSwitchEntryViewModel.Gpio), _gpioOptions)
            });

            gridView.Columns.Add(new GridViewColumn
            {
                Header = LocalizationManager.Get("InternalPullUp"),
                Width = 180,
                CellTemplate = CreateCheckBoxTemplate(nameof(LimitSwitchEntryViewModel.PullUp))
            });

            _listView.View = gridView;
            dataContainer.Child = _listView;
            Grid.SetRow(dataContainer, 1);
            rootGrid.Children.Add(dataContainer);

            var footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var saveButton = new Button
            {
                Content = LocalizationManager.Get("Save"),
                Width = 90,
                Margin = new Thickness(0, 0, 8, 0),
                IsDefault = true
            };
            saveButton.Click += Save_Click;

            var cancelButton = new Button
            {
                Content = LocalizationManager.Get("Cancel"),
                Width = 90,
                IsCancel = true
            };

            footer.Children.Add(saveButton);
            footer.Children.Add(cancelButton);
            Grid.SetRow(footer, 2);
            rootGrid.Children.Add(footer);

            root.Child = rootGrid;
            Content = root;

            LoadFromFlag();
        }

        private void LimitSwitchParametersWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateColumnWidths();
            UpdateListHeight();
        }

        private void UpdateColumnWidths()
        {
            if (_listView?.View is not GridView gridView || gridView.Columns.Count == 0)
                return;

            var currentWidth = Math.Max(0, _listView.ActualWidth - 24);
            if (currentWidth <= 0)
                return;

            var fixedWidth = 70 + 90 + 180;
            var remaining = Math.Max(120, currentWidth - fixedWidth);

            gridView.Columns[0].Width = 70;
            gridView.Columns[1].Width = 90;
            gridView.Columns[2].Width = Math.Max(120, remaining / 2d);
            gridView.Columns[3].Width = Math.Max(160, remaining / 2d);
        }

        private void UpdateListHeight()
        {
            if (_listView == null)
                return;

            const int estimatedRowHeight = 28;
            const int headerHeight = 28;
            var visibleRows = Math.Min(_entries.Count, _maxLimitSwitches);
            var desiredHeight = Math.Max(420, (visibleRows * estimatedRowHeight) + headerHeight + 12);

            _listView.Height = desiredHeight;
        }

        private static DataTemplate CreateCheckBoxTemplate(string propertyName)
        {
            var factory = new FrameworkElementFactory(typeof(CheckBox));
            factory.SetBinding(CheckBox.IsCheckedProperty, new Binding(propertyName)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            factory.SetValue(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            factory.SetValue(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Center);
            factory.SetValue(CheckBox.FocusableProperty, true);

            return new DataTemplate { VisualTree = factory };
        }

        private static DataTemplate CreateTextBoxTemplate(string propertyName)
        {
            var factory = new FrameworkElementFactory(typeof(TextBox));
            factory.SetBinding(TextBox.TextProperty, new Binding(propertyName)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            factory.SetValue(TextBox.WidthProperty, 100d);
            factory.SetValue(TextBox.MarginProperty, new Thickness(4, 2, 4, 2));
            factory.SetValue(TextBox.VerticalAlignmentProperty, VerticalAlignment.Center);

            return new DataTemplate { VisualTree = factory };
        }

        private static DataTemplate CreateGpioComboBoxTemplate(string propertyName, IReadOnlyList<BoardGpioOption> gpioOptions)
        {
            var factory = new FrameworkElementFactory(typeof(ComboBox));
            factory.SetValue(ComboBox.WidthProperty, 120d);
            factory.SetValue(ComboBox.DisplayMemberPathProperty, nameof(BoardGpioOption.Display));
            factory.SetValue(ComboBox.SelectedValuePathProperty, nameof(BoardGpioOption.Value));
            factory.SetBinding(ComboBox.ItemsSourceProperty, new Binding { Source = gpioOptions });
            factory.SetBinding(ComboBox.SelectedValueProperty, new Binding(propertyName) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            return new DataTemplate { VisualTree = factory };
        }

        private void LoadFromFlag()
        {
            _entries.Clear();

            var gpioByIndex = new Dictionary<int, int>();
            var pullUpByIndex = new Dictionary<int, bool>();
            var enabledCount = 0;

            if (_flag.Parameters != null)
            {
                foreach (var parameter in _flag.Parameters)
                {
                    if (parameter == null)
                        continue;

                    var identifier = (parameter.Key ?? parameter.Name ?? parameter.Identifier ?? string.Empty).Trim();
                    var value = parameter.Value ?? parameter.DefaultValue ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(identifier))
                        continue;

                    var countMatch = System.Text.RegularExpressions.Regex.Match(identifier, @"(?i)^(?:Parameter_SUPLA_LIMIT_SWITCH_)?Count$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                    if (countMatch.Success && int.TryParse(value, out var count))
                    {
                        enabledCount = Math.Clamp(count, 0, _maxLimitSwitches);
                    }

                    var gpioMatch = System.Text.RegularExpressions.Regex.Match(identifier, @"(?i)^(?:Parameter_SUPLA_LIMIT_SWITCH_)?GPIO(\d+)$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                    if (gpioMatch.Success && int.TryParse(value, out var gpio))
                    {
                        var index = int.Parse(gpioMatch.Groups[1].Value) - 1;
                        if (index >= 0 && index < _maxLimitSwitches && index < enabledCount)
                        {
                            gpioByIndex[index] = gpio;
                        }
                    }

                    var pullupMatch = System.Text.RegularExpressions.Regex.Match(identifier, @"(?i)^(?:Parameter_SUPLA_LIMIT_SWITCH_)?GPIO(\d+)Pullup$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                    if (pullupMatch.Success)
                    {
                        var index = int.Parse(pullupMatch.Groups[1].Value) - 1;
                        if (index >= 0 && index < _maxLimitSwitches && index < enabledCount)
                        {
                            var normalizedValue = (value ?? string.Empty).Trim();

                            if (bool.TryParse(normalizedValue, out var pullup))
                            {
                                pullUpByIndex[index] = pullup;
                            }
                            else if (normalizedValue.Equals("1", StringComparison.OrdinalIgnoreCase))
                            {
                                pullUpByIndex[index] = true;
                            }
                            else if (normalizedValue.Equals("0", StringComparison.OrdinalIgnoreCase))
                            {
                                pullUpByIndex[index] = false;
                            }
                            else
                            {
                                pullUpByIndex[index] = false;
                            }
                        }
                    }
                }
            }

            for (var index = 0; index < _maxLimitSwitches; index++)
            {
                var entry = new LimitSwitchEntryViewModel
                {
                    Index = index,
                    IsEnabled = index < enabledCount && gpioByIndex.TryGetValue(index, out var gpio) && gpio > 0,
                    Gpio = gpioByIndex.TryGetValue(index, out var configuredGpio) ? configuredGpio : 0,
                    PullUp = pullUpByIndex.TryGetValue(index, out var configuredPullUp) ? configuredPullUp : false
                };

                entry.PropertyChanged += Entry_PropertyChanged;
                _entries.Add(entry);
            }

            UpdateListHeight();
        }

        private void Entry_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(LimitSwitchEntryViewModel.IsEnabled) || sender is not LimitSwitchEntryViewModel changedEntry)
                return;

            var enabledIndices = _entries
                .Where(x => x.IsEnabled)
                .Select(x => x.Index)
                .OrderBy(x => x)
                .ToList();

            if (enabledIndices.Count == 0)
                return;

            for (var i = 0; i < enabledIndices.Count; i++)
            {
                if (enabledIndices[i] != i)
                {
                    changedEntry.IsEnabled = false;
                    MessageBox.Show(
                        LocalizationManager.Get("LimitSwitchSequenceMessage"),
                        LocalizationManager.Get("Warning"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var enabledEntries = _entries.Where(x => x.IsEnabled).OrderBy(x => x.Index).ToList();

            for (var i = 0; i < enabledEntries.Count; i++)
            {
                if (enabledEntries[i].Index != i)
                {
                    MessageBox.Show(
                        LocalizationManager.Get("LimitSwitchSequenceMessage"),
                        LocalizationManager.Get("Warning"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            var duplicateGpios = enabledEntries
                .Where(x => x.Gpio > 0)
                .GroupBy(x => x.Gpio)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .OrderBy(x => x)
                .ToList();

            if (duplicateGpios.Count > 0)
            {
                MessageBox.Show(
                    LocalizationManager.GetFormat("LimitSwitchDuplicateGpioMessage", string.Join(", ", duplicateGpios)),
                    LocalizationManager.Get("Warning"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var parameters = new List<Parameter>
            {
                new Parameter
                {
                    Key = "Count",
                    Name = "Count",
                    Type = "number",
                    Value = enabledEntries.Count.ToString()
                }
            };

            foreach (var entry in enabledEntries)
            {
                if (!_gpioOptions.Any(option => option.Value == entry.Gpio))
                {
                    MessageBox.Show(
                        LocalizationManager.GetFormat("LimitSwitchInvalidGpioMessage", entry.Index + 1),
                        LocalizationManager.Get("Warning"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var gpioNumber = entry.Index + 1;

                parameters.Add(new Parameter
                {
                    Key = $"GPIO{gpioNumber}",
                    Name = $"GPIO{gpioNumber}",
                    Type = "number",
                    Value = entry.Gpio.ToString()
                });

                parameters.Add(new Parameter
                {
                    Key = $"GPIO{gpioNumber}Pullup",
                    Name = $"GPIO{gpioNumber}Pullup",
                    Type = "number",
                    Value = entry.PullUp ? "1" : "0"
                });
            }

            _flag.Parameters = parameters;
            DialogResult = true;
            Close();
        }
    }
}
