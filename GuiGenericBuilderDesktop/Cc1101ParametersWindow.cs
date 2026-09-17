using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CompilationLib;
using GuiGenericBuilderDesktop.Localization;

namespace GuiGenericBuilderDesktop
{
    public class Cc1101ParametersWindow : Window
    {
        private const int MaxSensors = 10;
        private const string SensorChannelPrefix = "SensorChannel";

        private readonly BuildFlagItem _flag;
        private readonly string _boardName;
        private readonly IReadOnlyList<BoardGpioOption> _gpioOptions;
        private readonly ObservableCollection<Cc1101SensorEntryViewModel> _entries;
        private readonly ListView _listView;
        private readonly Cc1101PinViewModel _pins;

        public Cc1101ParametersWindow(BuildFlagItem flag, GlobalSettings globalSettings, string boardName = null)
        {
            _flag = flag ?? throw new ArgumentNullException(nameof(flag));
            _boardName = boardName ?? string.Empty;
            _gpioOptions = globalSettings?.GetOptionsForBoard(_boardName) ?? Array.Empty<BoardGpioOption>();
            _entries = new ObservableCollection<Cc1101SensorEntryViewModel>();
            _pins = new Cc1101PinViewModel();

            Title = LocalizationManager.Get("CC1101SettingsTitle");
            Width = 1400;
            Height = 620;
            MinWidth = 1400;
            MinHeight = 620;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            SizeToContent = SizeToContent.Manual;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            Background = Brushes.WhiteSmoke;
            SizeChanged += (_, _) => UpdateListHeight();

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
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new TextBlock
            {
                Text = LocalizationManager.Get("CC1101SettingsTitle"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(header, 0);
            rootGrid.Children.Add(header);

            var pinPanel = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var pinGrid = new Grid();
            for (var columnIndex = 0; columnIndex < 6; columnIndex++)
            {
                pinGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            var pinEditors = new[]
            {
                CreatePinEditor(LocalizationManager.Get("CC1101PinMISO"), nameof(Cc1101PinViewModel.Miso)),
                CreatePinEditor(LocalizationManager.Get("CC1101PinMOSI"), nameof(Cc1101PinViewModel.Mosi)),
                CreatePinEditor(LocalizationManager.Get("CC1101PinCLK"), nameof(Cc1101PinViewModel.Clk)),
                CreatePinEditor(LocalizationManager.Get("CC1101PinCS"), nameof(Cc1101PinViewModel.Cs)),
                CreatePinEditor(LocalizationManager.Get("CC1101PinGDO0"), nameof(Cc1101PinViewModel.Gdo0)),
                CreatePinEditor(LocalizationManager.Get("CC1101PinGDO2"), nameof(Cc1101PinViewModel.Gdo2))
            };

            for (var index = 0; index < pinEditors.Length; index++)
            {
                Grid.SetColumn(pinEditors[index], index);
                pinGrid.Children.Add(pinEditors[index]);
            }

            pinPanel.Child = pinGrid;
            Grid.SetRow(pinPanel, 1);
            rootGrid.Children.Add(pinPanel);

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

            var gridView = new GridView { AllowsColumnReorder = false };
            gridView.Columns.Add(new GridViewColumn { Header = LocalizationManager.Get("Number"), Width = 70, DisplayMemberBinding = new Binding(nameof(Cc1101SensorEntryViewModel.IndexDisplay)) });
            gridView.Columns.Add(new GridViewColumn { Header = LocalizationManager.Get("Enabled"), Width = 70, CellTemplate = CreateCheckBoxTemplate(nameof(Cc1101SensorEntryViewModel.IsEnabled)) });
            gridView.Columns.Add(new GridViewColumn { Header = LocalizationManager.Get("CC1101SensorType"), Width = 250, CellTemplate = CreateEnumComboBoxTemplate(nameof(Cc1101SensorEntryViewModel.SensorType), GetEnumOptionsForSensorType()) });
            gridView.Columns.Add(new GridViewColumn { Header = LocalizationManager.Get("CC1101SensorId"), Width = 240, CellTemplate = CreateTextBoxTemplate(nameof(Cc1101SensorEntryViewModel.SensorId), 190) });
            gridView.Columns.Add(new GridViewColumn { Header = LocalizationManager.Get("CC1101SensorKey"), Width = 260, CellTemplate = CreateTextBoxTemplate(nameof(Cc1101SensorEntryViewModel.SensorKey), 230) });
            gridView.Columns.Add(new GridViewColumn { Header = LocalizationManager.Get("CC1101SensorProperty"), Width = 220, CellTemplate = CreateEnumComboBoxTemplate(nameof(Cc1101SensorEntryViewModel.SensorProperty), GetEnumOptionsForSensorProperty()) });
            gridView.Columns.Add(new GridViewColumn { Header = LocalizationManager.Get("CC1101SensorChannel"), Width = 220, CellTemplate = CreateEnumComboBoxTemplate(nameof(Cc1101SensorEntryViewModel.SensorChannel), GetEnumOptionsForSensorChannel()) });
            _listView.View = gridView;
            dataContainer.Child = _listView;
            Grid.SetRow(dataContainer, 2);
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
            Grid.SetRow(footer, 3);
            rootGrid.Children.Add(footer);

            root.Child = rootGrid;
            Content = root;

            LoadFromFlag();
            UpdateListHeight();
        }

        private StackPanel CreatePinEditor(string label, string propertyName)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
            var textBlock = new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            panel.Children.Add(textBlock);

            var comboBox = new ComboBox
            {
                MinWidth = 140,
                Width = 150,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                ItemsSource = _gpioOptions,
                DisplayMemberPath = nameof(BoardGpioOption.Display),
                SelectedValuePath = nameof(BoardGpioOption.Value)
            };
            comboBox.SetBinding(ComboBox.SelectedValueProperty, new Binding(propertyName)
            {
                Source = _pins,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

            panel.Children.Add(comboBox);
            return panel;
        }

        private void UpdateListHeight()
        {
            var visibleRows = Math.Min(_entries.Count, MaxSensors);
            const int estimatedRowHeight = 28;
            const int headerHeight = 28;
            var desiredHeight = Math.Max(420, (visibleRows * estimatedRowHeight) + headerHeight + 16);
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

        private static DataTemplate CreateTextBoxTemplate(string propertyName, double width)
        {
            var factory = new FrameworkElementFactory(typeof(TextBox));
            factory.SetBinding(TextBox.TextProperty, new Binding(propertyName)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            factory.SetValue(TextBox.WidthProperty, width);
            factory.SetValue(TextBox.MarginProperty, new Thickness(4, 2, 4, 2));
            factory.SetValue(TextBox.VerticalAlignmentProperty, VerticalAlignment.Center);
            return new DataTemplate { VisualTree = factory };
        }

        private static DataTemplate CreateEnumComboBoxTemplate(string propertyName, IReadOnlyList<Cc1101EnumOption> options)
        {
            var factory = new FrameworkElementFactory(typeof(ComboBox));
            factory.SetValue(ComboBox.WidthProperty, 180d);
            factory.SetValue(ComboBox.MarginProperty, new Thickness(4, 2, 4, 2));
            factory.SetValue(ComboBox.DisplayMemberPathProperty, nameof(Cc1101EnumOption.Display));
            factory.SetValue(ComboBox.SelectedValuePathProperty, nameof(Cc1101EnumOption.Value));
            factory.SetBinding(ComboBox.ItemsSourceProperty, new Binding { Source = options });
            factory.SetBinding(ComboBox.SelectedValueProperty, new Binding(propertyName)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            return new DataTemplate { VisualTree = factory };
        }

        private IReadOnlyList<Cc1101EnumOption> GetEnumOptionsForSensorType()
        {
            return GetEnumOptions("SensorType1");
        }

        private IReadOnlyList<Cc1101EnumOption> GetEnumOptionsForSensorProperty()
        {
            return GetEnumOptions("SensorProperty1");
        }

        private IReadOnlyList<Cc1101EnumOption> GetEnumOptionsForSensorChannel()
        {
            return GetEnumOptions($"{SensorChannelPrefix}1");
        }

        private IReadOnlyList<Cc1101EnumOption> GetEnumOptions(string parameterKey)
        {
            var parameter = GetParameter(parameterKey);
            if (parameter == null || parameter.EnumValues == null || parameter.EnumValues.Count == 0)
            {
                return Array.Empty<Cc1101EnumOption>();
            }

            return parameter.EnumValues
                .Where(x => x != null)
                .Select(x => new Cc1101EnumOption
                {
                    Value = x.Value ?? string.Empty,
                    Display = !string.IsNullOrWhiteSpace(x.Name) ? x.Name : (x.Value ?? string.Empty)
                })
                .ToList();
        }

        private void LoadFromFlag()
        {
            _entries.Clear();
            _pins.Miso = ParseGpioValue(GetParameterValue("MISO"));
            _pins.Mosi = ParseGpioValue(GetParameterValue("MOSI"));
            _pins.Clk = ParseGpioValue(GetParameterValue("CLK"));
            _pins.Cs = ParseGpioValue(GetParameterValue("CS"));
            _pins.Gdo0 = ParseGpioValue(GetParameterValue("GDO0"));
            _pins.Gdo2 = ParseGpioValue(GetParameterValue("GDO2"));

            for (var index = 1; index <= MaxSensors; index++)
            {
                var entry = new Cc1101SensorEntryViewModel
                {
                    Index = index,
                    IsEnabled = IsEnabledValue(GetParameterValue($"Enabled{index}")),
                    SensorType = GetParameterValue($"SensorType{index}"),
                    SensorId = GetParameterValue($"SensorID{index}"),
                    SensorKey = GetParameterValue($"SensorKey{index}"),
                    SensorProperty = GetParameterValue($"SensorProperty{index}"),
                    SensorChannel = GetParameterValue($"{SensorChannelPrefix}{index}")
                };

                _entries.Add(entry);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var parameters = BuildParametersForSave(_flag.Parameters, _pins, _entries);

            var hasConfiguredPins = new[] { _pins.Miso, _pins.Mosi, _pins.Clk, _pins.Cs, _pins.Gdo0, _pins.Gdo2 }.Any(pin => pin > 0);
            var hasEnabledSensors = _entries.Any(entry => entry.IsEnabled);
            _flag.IsEnabled = hasConfiguredPins || hasEnabledSensors;
            _flag.Parameters = parameters;
            DialogResult = true;
            Close();
        }

        internal static List<Parameter> BuildParametersForSave(IEnumerable<Parameter> existingParameters, Cc1101PinViewModel pins, IEnumerable<Cc1101SensorEntryViewModel> entries)
        {
            var templates = CreateParameterTemplateLookup(existingParameters);
            var parameters = CreateWorkingParameterList(existingParameters);

            SetParameterValue(parameters, templates, "MISO", pins?.Miso.ToString() ?? "0");
            SetParameterValue(parameters, templates, "MOSI", pins?.Mosi.ToString() ?? "0");
            SetParameterValue(parameters, templates, "CLK", pins?.Clk.ToString() ?? "0");
            SetParameterValue(parameters, templates, "CS", pins?.Cs.ToString() ?? "0");
            SetParameterValue(parameters, templates, "GDO0", pins?.Gdo0.ToString() ?? "0");
            SetParameterValue(parameters, templates, "GDO2", pins?.Gdo2.ToString() ?? "0");

            foreach (var entry in (entries ?? Enumerable.Empty<Cc1101SensorEntryViewModel>()).Where(x => x != null && x.IsEnabled).OrderBy(x => x.Index))
            {
                SetParameterValue(parameters, templates, $"Enabled{entry.Index}", "1");
                SetParameterValue(parameters, templates, $"SensorType{entry.Index}", entry.SensorType ?? string.Empty);
                SetParameterValue(parameters, templates, $"SensorID{entry.Index}", entry.SensorId ?? string.Empty);
                SetParameterValue(parameters, templates, $"SensorKey{entry.Index}", entry.SensorKey ?? string.Empty);
                SetParameterValue(parameters, templates, $"SensorProperty{entry.Index}", entry.SensorProperty ?? string.Empty);
                SetParameterValue(parameters, templates, $"{SensorChannelPrefix}{entry.Index}", entry.SensorChannel ?? string.Empty);
            }

            return parameters;
        }

        private static Dictionary<string, Parameter> CreateParameterTemplateLookup(IEnumerable<Parameter> existingParameters)
        {
            var lookup = new Dictionary<string, Parameter>(StringComparer.OrdinalIgnoreCase);

            foreach (var parameter in existingParameters ?? Enumerable.Empty<Parameter>())
            {
                if (parameter == null)
                    continue;

                var identifier = parameter.Key ?? parameter.Name ?? parameter.Identifier;
                if (string.IsNullOrWhiteSpace(identifier) || lookup.ContainsKey(identifier))
                    continue;

                lookup[identifier] = CloneParameter(parameter);
            }

            return lookup;
        }

        private static List<Parameter> CreateWorkingParameterList(IEnumerable<Parameter> existingParameters)
        {
            var parameters = new List<Parameter>();

            foreach (var parameter in existingParameters ?? Enumerable.Empty<Parameter>())
            {
                if (parameter == null)
                    continue;

                var identifier = parameter.Key ?? parameter.Name ?? parameter.Identifier;
                if (IsSensorSlotParameter(identifier))
                    continue;

                parameters.Add(CloneParameter(parameter));
            }

            return parameters;
        }

        private static bool IsSensorSlotParameter(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            string[] sensorPrefixes =
            {
                "Enabled",
                "SensorType",
                "SensorID",
                "SensorKey",
                "SensorProperty",
                SensorChannelPrefix
            };

            foreach (var sensorPrefix in sensorPrefixes)
            {
                if (!identifier.StartsWith(sensorPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                return int.TryParse(identifier.Substring(sensorPrefix.Length), out var sensorIndex) && sensorIndex > 0;
            }

            return false;
        }

        private static Parameter CloneParameter(Parameter parameter)
        {
            return new Parameter
            {
                Key = parameter.Key,
                Name = parameter.Name,
                Type = parameter.Type,
                Value = parameter.Value,
                DefaultValue = parameter.DefaultValue,
                IsRequired = parameter.IsRequired,
                EnumValues = parameter.EnumValues?.Select(enumValue => new EnumValue
                {
                    Value = enumValue.Value,
                    Name = enumValue.Name,
                    Description = enumValue.Description
                }).ToList() ?? new List<EnumValue>(),
                Translations = parameter.Translations?.ToDictionary(
                    pair => pair.Key,
                    pair => new ParameterTranslation
                    {
                        Name = pair.Value?.Name,
                        Description = pair.Value?.Description,
                        EnumValues = pair.Value?.EnumValues?.Select(enumValue => new EnumValue
                        {
                            Value = enumValue.Value,
                            Name = enumValue.Name,
                            Description = enumValue.Description
                        }).ToList() ?? new List<EnumValue>()
                    }) ?? new Dictionary<string, ParameterTranslation>()
            };
        }

        private static int ParseGpioValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            var normalized = value.Trim();
            if (int.TryParse(normalized, out var numericValue))
            {
                return numericValue;
            }

            if (normalized.StartsWith("GPIO", StringComparison.OrdinalIgnoreCase) && int.TryParse(normalized.Substring(4), out numericValue))
            {
                return numericValue;
            }

            return 0;
        }

        private static void SetParameterValue(List<Parameter> parameters, Dictionary<string, Parameter> templates, string key, string value)
        {
            if (parameters == null)
            {
                return;
            }

            var target = parameters.FirstOrDefault(x => string.Equals(x?.Key ?? x?.Name ?? x?.Identifier, key, StringComparison.OrdinalIgnoreCase));
            if (target == null)
            {
                var parameter = templates != null && templates.TryGetValue(key, out var template)
                    ? CloneParameter(template)
                    : new Parameter
                    {
                        Key = key,
                        Name = key,
                        Type = "string"
                    };

                parameter.Value = value ?? string.Empty;
                if (string.IsNullOrEmpty(parameter.Key))
                    parameter.Key = key;
                if (string.IsNullOrEmpty(parameter.Name))
                    parameter.Name = key;

                parameters.Add(parameter);
                return;
            }

            target.Value = value ?? string.Empty;
            if (string.IsNullOrEmpty(target.Key))
                target.Key = key;
            if (string.IsNullOrEmpty(target.Name))
                target.Name = key;
        }

        private Parameter GetParameter(string key)
        {
            return _flag.Parameters?.FirstOrDefault(x => string.Equals(x?.Key ?? x?.Name ?? x?.Identifier, key, StringComparison.OrdinalIgnoreCase));
        }

        private string GetParameterValue(string key)
        {
            var parameter = GetParameter(key);
            if (parameter == null)
            {
                return string.Empty;
            }

            return parameter.Value ?? parameter.DefaultValue ?? string.Empty;
        }

        private static bool IsEnabledValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value.Trim() == "1" || value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) || value.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase);
        }
    }

    public class Cc1101PinViewModel : INotifyPropertyChanged
    {
        private int _miso;
        private int _mosi;
        private int _clk;
        private int _cs;
        private int _gdo0;
        private int _gdo2;

        public int Miso
        {
            get => _miso;
            set
            {
                if (_miso != value)
                {
                    _miso = value;
                    OnPropertyChanged(nameof(Miso));
                }
            }
        }

        public int Mosi
        {
            get => _mosi;
            set
            {
                if (_mosi != value)
                {
                    _mosi = value;
                    OnPropertyChanged(nameof(Mosi));
                }
            }
        }

        public int Clk
        {
            get => _clk;
            set
            {
                if (_clk != value)
                {
                    _clk = value;
                    OnPropertyChanged(nameof(Clk));
                }
            }
        }

        public int Cs
        {
            get => _cs;
            set
            {
                if (_cs != value)
                {
                    _cs = value;
                    OnPropertyChanged(nameof(Cs));
                }
            }
        }

        public int Gdo0
        {
            get => _gdo0;
            set
            {
                if (_gdo0 != value)
                {
                    _gdo0 = value;
                    OnPropertyChanged(nameof(Gdo0));
                }
            }
        }

        public int Gdo2
        {
            get => _gdo2;
            set
            {
                if (_gdo2 != value)
                {
                    _gdo2 = value;
                    OnPropertyChanged(nameof(Gdo2));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Cc1101SensorEntryViewModel : INotifyPropertyChanged
    {
        private bool _isEnabled;
        private string _sensorType = string.Empty;
        private string _sensorId = string.Empty;
        private string _sensorKey = string.Empty;
        private string _sensorProperty = string.Empty;
        private string _sensorChannel = string.Empty;

        public int Index { get; set; }

        public string IndexDisplay => Index.ToString();

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        public string SensorType
        {
            get => _sensorType;
            set
            {
                if (_sensorType != value)
                {
                    _sensorType = value ?? string.Empty;
                    OnPropertyChanged(nameof(SensorType));
                }
            }
        }

        public string SensorId
        {
            get => _sensorId;
            set
            {
                if (_sensorId != value)
                {
                    _sensorId = value ?? string.Empty;
                    OnPropertyChanged(nameof(SensorId));
                }
            }
        }

        public string SensorKey
        {
            get => _sensorKey;
            set
            {
                if (_sensorKey != value)
                {
                    _sensorKey = value ?? string.Empty;
                    OnPropertyChanged(nameof(SensorKey));
                }
            }
        }

        public string SensorProperty
        {
            get => _sensorProperty;
            set
            {
                if (_sensorProperty != value)
                {
                    _sensorProperty = value ?? string.Empty;
                    OnPropertyChanged(nameof(SensorProperty));
                }
            }
        }

        public string SensorChannel
        {
            get => _sensorChannel;
            set
            {
                if (_sensorChannel != value)
                {
                    _sensorChannel = value ?? string.Empty;
                    OnPropertyChanged(nameof(SensorChannel));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed class Cc1101EnumOption
    {
        public string Value { get; init; } = string.Empty;
        public string Display { get; init; } = string.Empty;
    }
}