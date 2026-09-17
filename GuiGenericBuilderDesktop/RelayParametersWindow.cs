using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using CompilationLib;
using GuiGenericBuilderDesktop.Localization;

namespace GuiGenericBuilderDesktop
{
    public class RelayParametersWindow : Window
    {
        private readonly BuildFlagItem _flag;
        private readonly IReadOnlyCollection<BuildFlagItem> _allFlags;
        private readonly ObservableCollection<RelayEntryViewModel> _entries;
        private readonly ListView _listView;
        private readonly StackPanel _relaySettingsContent;
        private readonly Border _settingsPanel;
        private readonly int _maxRelays = 15;
        private readonly string _boardName;
        private readonly IReadOnlyList<BoardGpioOption> _gpioOptions;

        public RelayParametersWindow(BuildFlagItem flag, IEnumerable<BuildFlagItem> allFlags, GlobalSettings globalSettings, string boardName = null)
        {
            _flag = flag ?? throw new ArgumentNullException(nameof(flag));
            _allFlags = allFlags?.ToList() ?? new List<BuildFlagItem>();
            _boardName = boardName ?? string.Empty;
            _gpioOptions = globalSettings.GetOptionsForBoard(_boardName);
            _entries = new ObservableCollection<RelayEntryViewModel>();

            Title = LocalizationManager.Get("RelaySettingsTitle");
            Width = 1400;
            Height = 800;
            MinWidth = 1400;
            MinHeight = 800;
            WindowState = WindowState.Maximized;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Brushes.WhiteSmoke;
            SizeChanged += (_, _) => UpdateLayoutSizing();

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
                Text = LocalizationManager.Get("RelaySettingsTitle"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(header, 0);
            rootGrid.Children.Add(header);

            var contentGrid = new Grid();
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(400, GridUnitType.Pixel) });

            var dataContainer = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 8, 8)
            };

            _listView = new ListView
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0),
                Background = Brushes.White,
                MinHeight = 320,
                ItemsSource = _entries,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                SelectionMode = SelectionMode.Single,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var gridView = new GridView { AllowsColumnReorder = false };
            gridView.Columns.Add(new GridViewColumn { Header = LocalizationManager.Get("Number"), Width = 50, DisplayMemberBinding = new Binding(nameof(RelayEntryViewModel.IndexDisplay)) });
            gridView.Columns.Add(new GridViewColumn { Header = LocalizationManager.Get("Enabled"), Width = 70, CellTemplate = CreateCheckBoxTemplate(nameof(RelayEntryViewModel.IsEnabled)) });
            gridView.Columns.Add(new GridViewColumn { Header = LocalizationManager.Get("GPIO"), Width = 140, CellTemplate = CreateGpioComboBoxTemplate(nameof(RelayEntryViewModel.Gpio), _gpioOptions) });
            gridView.Columns.Add(new GridViewColumn { Header = LocalizationManager.Get("RelayState"), Width = 140, CellTemplate = CreateEnumComboBoxTemplate(nameof(RelayEntryViewModel.State), new[]
            {
                new EnumValue { Value = "0", Name = LocalizationManager.Get("RelayStateLow") },
                new EnumValue { Value = "1", Name = LocalizationManager.Get("RelayStateHigh") }
            }) });
            gridView.Columns.Add(new GridViewColumn { Header = LocalizationManager.Get("RelayLight"), Width = 140, CellTemplate = CreateCheckBoxTemplate(nameof(RelayEntryViewModel.LightControl)) });
            gridView.Columns.Add(new GridViewColumn { Header = LocalizationManager.Get("RelayAfterReset"), Width = 140, CellTemplate = CreateEnumComboBoxTemplate(nameof(RelayEntryViewModel.AfterResetReaction), new[]
            {
                new EnumValue { Value = "0", Name = LocalizationManager.Get("RelayAfterResetOff") },
                new EnumValue { Value = "1", Name = LocalizationManager.Get("RelayAfterResetOn") },
                new EnumValue { Value = "2", Name = LocalizationManager.Get("RelayAfterResetRememberState") }
            }) });
            gridView.Columns.Add(new GridViewColumn { Header = LocalizationManager.Get("Settings"), Width = 100, CellTemplate = CreateSettingsButtonTemplate() });
            _listView.View = gridView;
            dataContainer.Child = _listView;
            Grid.SetColumn(dataContainer, 0);
            contentGrid.Children.Add(dataContainer);

            _settingsPanel = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8),
                Visibility = Visibility.Collapsed
            };

            var settingsTitle = new TextBlock
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8),
                Text = LocalizationManager.Get("Settings")
            };

            _relaySettingsContent = new StackPanel();
            _relaySettingsContent.Children.Add(new TextBlock
            {
                Text = LocalizationManager.Get("RelaySettingsHint"),
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 8, 0, 0)
            });
            _settingsPanel.Visibility = Visibility.Visible;

            var settingsContent = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = _relaySettingsContent
            };

            var settingsRoot = new StackPanel();
            settingsRoot.Children.Add(settingsTitle);
            settingsRoot.Children.Add(settingsContent);
            _settingsPanel.Child = settingsRoot;
            Grid.SetColumn(_settingsPanel, 1);
            contentGrid.Children.Add(_settingsPanel);

            Grid.SetRow(contentGrid, 1);
            rootGrid.Children.Add(contentGrid);

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

            var firstEnabledRelay = _entries.FirstOrDefault(entry => entry.IsEnabled);
            if (firstEnabledRelay != null)
            {
                ShowRelaySettings(firstEnabledRelay);
            }
        }

        private void UpdateLayoutSizing()
        {
            if (_listView?.View is not GridView gridView || gridView.Columns.Count == 0)
                return;

            var width = Math.Max(0, _listView.ActualWidth - 30);
            if (width <= 0)
                return;

            var fixedWidths = new List<double> { 50, 90, 140, 140, 140, 140, 140 };
            var totalFixedWidth = fixedWidths.Sum();
            var remaining = Math.Max(140, width - totalFixedWidth);

            for (var i = 0; i < gridView.Columns.Count; i++)
            {
                if (i < fixedWidths.Count)
                {
                    gridView.Columns[i].Width = fixedWidths[i];
                }
                else
                {
                    gridView.Columns[i].Width = remaining;
                }
            }
        }

        private DataTemplate CreateSettingsButtonTemplate()
        {
            var factory = new FrameworkElementFactory(typeof(Button));
            factory.SetValue(Button.ContentProperty, LocalizationManager.Get("Settings"));
            factory.SetValue(Button.MinWidthProperty, 80d);
            factory.SetValue(Button.PaddingProperty, new Thickness(4, 2, 4, 2));
            factory.AddHandler(Button.ClickEvent, new RoutedEventHandler((sender, args) =>
            {
                if (sender is not Button button || button.DataContext is not RelayEntryViewModel relay)
                    return;

                ShowRelaySettings(relay);
            }));
            return new DataTemplate { VisualTree = factory };
        }

        private void ShowRelaySettings(RelayEntryViewModel relay)
        {
            if (!relay.IsEnabled)
            {
                _relaySettingsContent.Children.Clear();
                _settingsPanel.Visibility = Visibility.Collapsed;
                return;
            }

            _relaySettingsContent.Children.Clear();
            _relaySettingsContent.Children.Add(new TextBlock
            {
                Text = string.Format(LocalizationManager.Get("RelayNumber"), relay.Index + 1),
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            });

            var hasLedFlag = _allFlags.Any(flag => flag != null && flag.IsEnabled && string.Equals(flag.Key, "SUPLA_LED", StringComparison.OrdinalIgnoreCase));
            var hasDirectLinksFlag = _allFlags.Any(flag => flag != null && flag.IsEnabled && string.Equals(flag.Key, "SUPLA_DIRECT_LINKS", StringComparison.OrdinalIgnoreCase));
            var hasThermostatFlag = _allFlags.Any(flag => flag != null && flag.IsEnabled && string.Equals(flag.Key, "SUPLA_THERMOSTAT", StringComparison.OrdinalIgnoreCase));
            if (hasLedFlag || hasDirectLinksFlag || hasThermostatFlag)
            {
                if (hasLedFlag)
                {
                    var ledGroup = new GroupBox
                    {
                        Header = LocalizationManager.Get("RelayLed"),
                        Margin = new Thickness(0, 0, 0, 6),
                        Padding = new Thickness(8)
                    };

                    var ledGrid = new Grid();
                    ledGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    ledGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    ledGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    ledGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    var gpioLabel = new TextBlock { Text = LocalizationManager.Get("RelayGpioLabel"), Margin = new Thickness(0, 0, 8, 4), VerticalAlignment = VerticalAlignment.Center };
                    Grid.SetRow(gpioLabel, 0);
                    Grid.SetColumn(gpioLabel, 0);
                    ledGrid.Children.Add(gpioLabel);

                    var gpioCombo = new ComboBox
                    {
                        MinWidth = 140,
                        ItemsSource = _gpioOptions,
                        DisplayMemberPath = nameof(BoardGpioOption.Display),
                        SelectedValuePath = nameof(BoardGpioOption.Value),
                        Margin = new Thickness(0, 0, 0, 4)
                    };
                    gpioCombo.SetBinding(ComboBox.SelectedValueProperty, new Binding(nameof(RelayEntryViewModel.LedGpio))
                    {
                        Source = relay,
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    });
                    Grid.SetRow(gpioCombo, 0);
                    Grid.SetColumn(gpioCombo, 1);
                    ledGrid.Children.Add(gpioCombo);

                    var stateLabel = new TextBlock { Text = LocalizationManager.Get("RelayActivationState"), Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
                    Grid.SetRow(stateLabel, 1);
                    Grid.SetColumn(stateLabel, 0);
                    ledGrid.Children.Add(stateLabel);

                    var stateCombo = new ComboBox
                    {
                        MinWidth = 140,
                        ItemsSource = new[]
                        {
                            new EnumValue { Value = "0", Name = LocalizationManager.Get("RelayStateLow") },
                            new EnumValue { Value = "1", Name = LocalizationManager.Get("RelayStateHigh") }
                        },
                        DisplayMemberPath = nameof(EnumValue.Name),
                        SelectedValuePath = nameof(EnumValue.Value),
                        Margin = new Thickness(0, 0, 0, 0)
                    };
                    stateCombo.SetBinding(ComboBox.SelectedValueProperty, new Binding(nameof(RelayEntryViewModel.LedActivationState))
                    {
                        Source = relay,
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    });
                    Grid.SetRow(stateCombo, 1);
                    Grid.SetColumn(stateCombo, 1);
                    ledGrid.Children.Add(stateCombo);

                    ledGroup.Content = ledGrid;
                    _relaySettingsContent.Children.Add(ledGroup);
                }

                if (hasDirectLinksFlag)
                {
                    var directLinksGroup = new GroupBox
                    {
                        Header = LocalizationManager.Get("RelayDirectLinks"),
                        Margin = new Thickness(0, 0, 0, 6),
                        Padding = new Thickness(8)
                    };

                    var directLinksGrid = new Grid();
                    directLinksGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    directLinksGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    directLinksGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    directLinksGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    var onLabel = new TextBlock { Text = LocalizationManager.Get("RelayOn"), Margin = new Thickness(0, 0, 8, 4), VerticalAlignment = VerticalAlignment.Center };
                    Grid.SetRow(onLabel, 0);
                    Grid.SetColumn(onLabel, 0);
                    directLinksGrid.Children.Add(onLabel);

                    var onTextBox = new TextBox
                    {
                        MinWidth = 180,
                        MaxLength = 32,
                        Margin = new Thickness(0, 0, 0, 4)
                    };
                    onTextBox.SetBinding(TextBox.TextProperty, new Binding(nameof(RelayEntryViewModel.DirectLinksOn))
                    {
                        Source = relay,
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    });
                    Grid.SetRow(onTextBox, 0);
                    Grid.SetColumn(onTextBox, 1);
                    directLinksGrid.Children.Add(onTextBox);

                    var offLabel = new TextBlock { Text = LocalizationManager.Get("RelayOff"), Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
                    Grid.SetRow(offLabel, 1);
                    Grid.SetColumn(offLabel, 0);
                    directLinksGrid.Children.Add(offLabel);

                    var offTextBox = new TextBox
                    {
                        MinWidth = 180,
                        MaxLength = 32,
                        Margin = new Thickness(0, 0, 0, 0)
                    };
                    offTextBox.SetBinding(TextBox.TextProperty, new Binding(nameof(RelayEntryViewModel.DirectLinksOff))
                    {
                        Source = relay,
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    });
                    Grid.SetRow(offTextBox, 1);
                    Grid.SetColumn(offTextBox, 1);
                    directLinksGrid.Children.Add(offTextBox);

                    directLinksGroup.Content = directLinksGrid;
                    _relaySettingsContent.Children.Add(directLinksGroup);
                }

                if (hasThermostatFlag)
                {
                    var thermostatGroup = new GroupBox
                    {
                        Header = LocalizationManager.Get("RelayThermostat"),
                        Margin = new Thickness(0, 0, 0, 6),
                        Padding = new Thickness(8)
                    };

                    var thermostatGrid = new Grid();
                    thermostatGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    thermostatGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    thermostatGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    thermostatGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    thermostatGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    thermostatGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    thermostatGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    thermostatGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    var thermostatTypeOptions = new[]
                    {
                        new EnumValue { Value = "0", Name = "OFF" },
                        new EnumValue { Value = "1", Name = "Heat" },
                        new EnumValue { Value = "2", Name = "Cool" },
                        new EnumValue { Value = "3", Name = "Auto" },
                        new EnumValue { Value = "4", Name = "DomesticHotWater" },
                        new EnumValue { Value = "5", Name = "Differential" }
                    };

                    var typeLabel = new TextBlock { Text = LocalizationManager.Get("RelayType"), Margin = new Thickness(0, 0, 8, 4), VerticalAlignment = VerticalAlignment.Center };
                    Grid.SetRow(typeLabel, 0);
                    Grid.SetColumn(typeLabel, 0);
                    thermostatGrid.Children.Add(typeLabel);

                    var typeCombo = new ComboBox
                    {
                        MinWidth = 180,
                        ItemsSource = thermostatTypeOptions,
                        DisplayMemberPath = nameof(EnumValue.Name),
                        SelectedValuePath = nameof(EnumValue.Value),
                        Margin = new Thickness(0, 0, 0, 4)
                    };
                    typeCombo.SetBinding(ComboBox.SelectedValueProperty, new Binding(nameof(RelayEntryViewModel.ThermostatType))
                    {
                        Source = relay,
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    });
                    Grid.SetRow(typeCombo, 0);
                    Grid.SetColumn(typeCombo, 1);
                    thermostatGrid.Children.Add(typeCombo);

                    var mainTempLabel = new TextBlock { Text = LocalizationManager.Get("RelayMainTempChannel"), Margin = new Thickness(0, 0, 8, 4), VerticalAlignment = VerticalAlignment.Center };
                    Grid.SetRow(mainTempLabel, 1);
                    Grid.SetColumn(mainTempLabel, 0);
                    thermostatGrid.Children.Add(mainTempLabel);

                    var mainTempBox = new TextBox { MinWidth = 180, Margin = new Thickness(0, 0, 0, 4) };
                    mainTempBox.SetBinding(TextBox.TextProperty, new Binding(nameof(RelayEntryViewModel.ThermostatMainTempChannel))
                    {
                        Source = relay,
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    });
                    Grid.SetRow(mainTempBox, 1);
                    Grid.SetColumn(mainTempBox, 1);
                    thermostatGrid.Children.Add(mainTempBox);

                    var additionalTempLabel = new TextBlock { Text = LocalizationManager.Get("RelayAdditionalTempChannel"), Margin = new Thickness(0, 0, 8, 4), VerticalAlignment = VerticalAlignment.Center };
                    Grid.SetRow(additionalTempLabel, 2);
                    Grid.SetColumn(additionalTempLabel, 0);
                    thermostatGrid.Children.Add(additionalTempLabel);

                    var additionalTempBox = new TextBox { MinWidth = 180, Margin = new Thickness(0, 0, 0, 4) };
                    additionalTempBox.SetBinding(TextBox.TextProperty, new Binding(nameof(RelayEntryViewModel.ThermostatAdditionalTempChannel))
                    {
                        Source = relay,
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    });
                    Grid.SetRow(additionalTempBox, 2);
                    Grid.SetColumn(additionalTempBox, 1);
                    thermostatGrid.Children.Add(additionalTempBox);

                    var hysteresisLabel = new TextBlock { Text = LocalizationManager.Get("RelayHysteresis"), Margin = new Thickness(0, 0, 8, 4), VerticalAlignment = VerticalAlignment.Center };
                    Grid.SetRow(hysteresisLabel, 3);
                    Grid.SetColumn(hysteresisLabel, 0);
                    thermostatGrid.Children.Add(hysteresisLabel);

                    var hysteresisBox = new TextBox { MinWidth = 180, Margin = new Thickness(0, 0, 0, 4) };
                    hysteresisBox.SetBinding(TextBox.TextProperty, new Binding(nameof(RelayEntryViewModel.ThermostatHisteresis))
                    {
                        Source = relay,
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    });
                    Grid.SetRow(hysteresisBox, 3);
                    Grid.SetColumn(hysteresisBox, 1);
                    thermostatGrid.Children.Add(hysteresisBox);

                    var minTempLabel = new TextBlock { Text = LocalizationManager.Get("RelayMinTemp"), Margin = new Thickness(0, 0, 8, 4), VerticalAlignment = VerticalAlignment.Center };
                    Grid.SetRow(minTempLabel, 4);
                    Grid.SetColumn(minTempLabel, 0);
                    thermostatGrid.Children.Add(minTempLabel);

                    var minTempBox = new TextBox { MinWidth = 180, Margin = new Thickness(0, 0, 0, 4) };
                    minTempBox.SetBinding(TextBox.TextProperty, new Binding(nameof(RelayEntryViewModel.ThermostatMinTemp))
                    {
                        Source = relay,
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    });
                    Grid.SetRow(minTempBox, 4);
                    Grid.SetColumn(minTempBox, 1);
                    thermostatGrid.Children.Add(minTempBox);

                    var maxTempLabel = new TextBlock { Text = LocalizationManager.Get("RelayMaxTemp"), Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
                    Grid.SetRow(maxTempLabel, 5);
                    Grid.SetColumn(maxTempLabel, 0);
                    thermostatGrid.Children.Add(maxTempLabel);

                    var maxTempBox = new TextBox { MinWidth = 180, Margin = new Thickness(0, 0, 0, 0) };
                    maxTempBox.SetBinding(TextBox.TextProperty, new Binding(nameof(RelayEntryViewModel.ThermostatMaxTemp))
                    {
                        Source = relay,
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    });
                    Grid.SetRow(maxTempBox, 5);
                    Grid.SetColumn(maxTempBox, 1);
                    thermostatGrid.Children.Add(maxTempBox);

                    thermostatGroup.Content = thermostatGrid;
                    _relaySettingsContent.Children.Add(thermostatGroup);
                }
            }
            else
            {
                _relaySettingsContent.Children.Add(new TextBlock { Text = LocalizationManager.Get("RelayNoRelatedSettings") });
            }

            _settingsPanel.Visibility = Visibility.Visible;
        }

        private static DataTemplate CreateCheckBoxTemplate(string propertyName)
        {
            var factory = new FrameworkElementFactory(typeof(CheckBox));
            factory.SetBinding(CheckBox.IsCheckedProperty, new Binding(propertyName) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            var template = new DataTemplate { VisualTree = factory };
            return template;
        }

        private static DataTemplate CreateIntBoxTemplate(string propertyName)
        {
            var factory = new FrameworkElementFactory(typeof(TextBox));
            factory.SetValue(TextBox.MinWidthProperty, 70d);
            factory.SetValue(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center);
            factory.SetBinding(TextBox.TextProperty, new Binding(propertyName) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            factory.AddHandler(TextBox.PreviewTextInputEvent, new TextCompositionEventHandler((sender, e) =>
            {
                var text = (sender as TextBox)?.Text ?? string.Empty;
                var proposed = text.Insert((sender as TextBox)?.SelectionStart ?? 0, e.Text);
                e.Handled = !int.TryParse(proposed, out _);
            }));
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

        private static DataTemplate CreateEnumComboBoxTemplate(string propertyName, IEnumerable<EnumValue> enumValues)
        {
            var factory = new FrameworkElementFactory(typeof(ComboBox));
            factory.SetValue(ComboBox.WidthProperty, 100d);
            factory.SetValue(ComboBox.DisplayMemberPathProperty, nameof(EnumValue.Name));
            factory.SetValue(ComboBox.SelectedValuePathProperty, nameof(EnumValue.Value));
            factory.SetBinding(ComboBox.ItemsSourceProperty, new Binding { Source = enumValues });
            factory.SetBinding(ComboBox.SelectedValueProperty, new Binding(propertyName) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            return new DataTemplate { VisualTree = factory };
        }

        private static bool TryConvertThermostatValue(string value, out string convertedValue)
        {
            convertedValue = string.Empty;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
            {
                convertedValue = (intValue / 100m).ToString(CultureInfo.InvariantCulture);
                return true;
            }

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
            {
                convertedValue = decimalValue.ToString(CultureInfo.InvariantCulture);
                return true;
            }

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out decimalValue))
            {
                convertedValue = decimalValue.ToString(CultureInfo.InvariantCulture);
                return true;
            }

            return false;
        }

        private static string ParseThermostatDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue) ||
                decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out decimalValue))
            {
                return (decimalValue * 100m).ToString(CultureInfo.InvariantCulture);
            }

            return string.Empty;
        }

        private void LoadFromFlag()
        {
            var directLinksOnByIndex = new Dictionary<int, string>();
            var directLinksOffByIndex = new Dictionary<int, string>();
            var thermostatTypeByIndex = new Dictionary<int, int>();
            var thermostatMainTempChannelByIndex = new Dictionary<int, int>();
            var thermostatAdditionalTempChannelByIndex = new Dictionary<int, int>();
            var thermostatHisteresisByIndex = new Dictionary<int, string>();
            var thermostatMinTempByIndex = new Dictionary<int, string>();
            var thermostatMaxTempByIndex = new Dictionary<int, string>();
            var enabledRelayCount = 0;

            if (_flag.Parameters != null)
            {
                foreach (var parameter in _flag.Parameters.Where(p => p != null))
                {
                    var identifier = parameter.Key ?? parameter.Name ?? parameter.Identifier ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(identifier))
                        continue;

                    var countMatch = System.Text.RegularExpressions.Regex.Match(identifier, @"(?i)^(?:Parameter_SUPLA_RELAY_)?Count$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                    if (countMatch.Success && int.TryParse(parameter.Value, out var count))
                    {
                        enabledRelayCount = Math.Clamp(count, 0, _maxRelays);
                        continue;
                    }

                    if (string.Equals(identifier, "SUPLA_DIRECT_LINKS_On", StringComparison.OrdinalIgnoreCase))
                    {
                        directLinksOnByIndex[0] = parameter.Value ?? string.Empty;
                        continue;
                    }

                    if (string.Equals(identifier, "SUPLA_DIRECT_LINKS_Off", StringComparison.OrdinalIgnoreCase))
                    {
                        directLinksOffByIndex[0] = parameter.Value ?? string.Empty;
                        continue;
                    }

                    if (string.Equals(identifier, "SUPLA_THERMOSTAT_Type", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(parameter.Value, out var thermostatType))
                            thermostatTypeByIndex[0] = thermostatType;
                        continue;
                    }

                    if (string.Equals(identifier, "SUPLA_THERMOSTAT_MainTempChannel", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(parameter.Value, out var mainTempChannel))
                            thermostatMainTempChannelByIndex[0] = mainTempChannel;
                        continue;
                    }

                    if (string.Equals(identifier, "SUPLA_THERMOSTAT_AdditionalTempChannel", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(parameter.Value, out var additionalTempChannel))
                            thermostatAdditionalTempChannelByIndex[0] = additionalTempChannel;
                        continue;
                    }

                    if (string.Equals(identifier, "SUPLA_THERMOSTAT_Histeresis", StringComparison.OrdinalIgnoreCase))
                    {
                        if (TryConvertThermostatValue(parameter.Value, out var hystValue))
                            thermostatHisteresisByIndex[0] = hystValue;
                        continue;
                    }

                    if (string.Equals(identifier, "SUPLA_THERMOSTAT_MinTemp", StringComparison.OrdinalIgnoreCase))
                    {
                        if (TryConvertThermostatValue(parameter.Value, out var minTempParsed))
                            thermostatMinTempByIndex[0] = minTempParsed;
                        continue;
                    }

                    if (string.Equals(identifier, "SUPLA_THERMOSTAT_MaxTemp", StringComparison.OrdinalIgnoreCase))
                    {
                        if (TryConvertThermostatValue(parameter.Value, out var maxTempParsed))
                            thermostatMaxTempByIndex[0] = maxTempParsed;
                        continue;
                    }

                    var directLinksOnMatch = System.Text.RegularExpressions.Regex.Match(identifier, @"(?i)^(?:Parameter_SUPLA_RELAY_)?GPIO(\d+)DirectLinksOn$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                    if (directLinksOnMatch.Success && int.TryParse(directLinksOnMatch.Groups[1].Value, out var directLinksOnIndex))
                    {
                        directLinksOnByIndex[directLinksOnIndex] = parameter.Value ?? string.Empty;
                        continue;
                    }

                    var directLinksOffMatch = System.Text.RegularExpressions.Regex.Match(identifier, @"(?i)^(?:Parameter_SUPLA_RELAY_)?GPIO(\d+)DirectLinksOff$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                    if (directLinksOffMatch.Success && int.TryParse(directLinksOffMatch.Groups[1].Value, out var directLinksOffIndex))
                    {
                        directLinksOffByIndex[directLinksOffIndex] = parameter.Value ?? string.Empty;
                        continue;
                    }

                    var thermostatTypeMatch = System.Text.RegularExpressions.Regex.Match(identifier, @"(?i)^(?:Parameter_SUPLA_RELAY_)?GPIO(\d+)ThermostatType$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                    if (thermostatTypeMatch.Success && int.TryParse(thermostatTypeMatch.Groups[1].Value, out var thermostatIndex) && int.TryParse(parameter.Value, out var thermostatTypeValue))
                    {
                        thermostatTypeByIndex[thermostatIndex] = thermostatTypeValue;
                        continue;
                    }

                    var thermostatMainTempChannelMatch = System.Text.RegularExpressions.Regex.Match(identifier, @"(?i)^(?:Parameter_SUPLA_RELAY_)?GPIO(\d+)ThermostatMainTempChannel$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                    if (thermostatMainTempChannelMatch.Success && int.TryParse(thermostatMainTempChannelMatch.Groups[1].Value, out var mainTempIndex) && int.TryParse(parameter.Value, out var mainTempChannelValue))
                    {
                        thermostatMainTempChannelByIndex[mainTempIndex] = mainTempChannelValue;
                        continue;
                    }

                    var thermostatAdditionalTempChannelMatch = System.Text.RegularExpressions.Regex.Match(identifier, @"(?i)^(?:Parameter_SUPLA_RELAY_)?GPIO(\d+)ThermostatAdditionalTempChannel$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                    if (thermostatAdditionalTempChannelMatch.Success && int.TryParse(thermostatAdditionalTempChannelMatch.Groups[1].Value, out var additionalTempIndex) && int.TryParse(parameter.Value, out var additionalTempChannelValue))
                    {
                        thermostatAdditionalTempChannelByIndex[additionalTempIndex] = additionalTempChannelValue;
                        continue;
                    }

                    var thermostatHisteresisMatch = System.Text.RegularExpressions.Regex.Match(identifier, @"(?i)^(?:Parameter_SUPLA_RELAY_)?GPIO(\d+)ThermostatHisteresis$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                    if (thermostatHisteresisMatch.Success && int.TryParse(thermostatHisteresisMatch.Groups[1].Value, out var hysteresisIndex) && TryConvertThermostatValue(parameter.Value, out var hysteresisValue))
                    {
                        thermostatHisteresisByIndex[hysteresisIndex] = hysteresisValue;
                        continue;
                    }

                    var thermostatMinTempMatch = System.Text.RegularExpressions.Regex.Match(identifier, @"(?i)^(?:Parameter_SUPLA_RELAY_)?GPIO(\d+)ThermostatMinTemp$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                    if (thermostatMinTempMatch.Success && int.TryParse(thermostatMinTempMatch.Groups[1].Value, out var minTempIndex) && TryConvertThermostatValue(parameter.Value, out var minTempValue))
                    {
                        thermostatMinTempByIndex[minTempIndex] = minTempValue;
                        continue;
                    }

                    var thermostatMaxTempMatch = System.Text.RegularExpressions.Regex.Match(identifier, @"(?i)^(?:Parameter_SUPLA_RELAY_)?GPIO(\d+)ThermostatMaxTemp$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                    if (thermostatMaxTempMatch.Success && int.TryParse(thermostatMaxTempMatch.Groups[1].Value, out var maxTempIndex) && TryConvertThermostatValue(parameter.Value, out var maxTempValue))
                    {
                        thermostatMaxTempByIndex[maxTempIndex] = maxTempValue;
                    }
                }
            }

            for (var i = 0; i < _maxRelays; i++)
            {
                var gpioValue = 0;
                var stateValue = 0;
                var lightValue = 0;
                var afterResetReaction = 0;
                var ledGpioValue = 0;
                var ledActivationState = 0;
                var relayNumber = i + 1;
                var directLinksOnValue = directLinksOnByIndex.TryGetValue(relayNumber, out var directLinksOn) ? directLinksOn : directLinksOnByIndex.TryGetValue(0, out var legacyDirectLinksOn) ? legacyDirectLinksOn : string.Empty;
                var directLinksOffValue = directLinksOffByIndex.TryGetValue(relayNumber, out var directLinksOff) ? directLinksOff : directLinksOffByIndex.TryGetValue(0, out var legacyDirectLinksOff) ? legacyDirectLinksOff : string.Empty;
                var thermostatTypeValue = thermostatTypeByIndex.TryGetValue(relayNumber, out var thermostatType) ? thermostatType : thermostatTypeByIndex.TryGetValue(0, out var legacyThermostatType) ? legacyThermostatType : 0;
                var thermostatMainTempChannelValue = thermostatMainTempChannelByIndex.TryGetValue(relayNumber, out var thermostatMainTempChannel) ? thermostatMainTempChannel : thermostatMainTempChannelByIndex.TryGetValue(0, out var legacyMainTempChannel) ? legacyMainTempChannel : 0;
                var thermostatAdditionalTempChannelValue = thermostatAdditionalTempChannelByIndex.TryGetValue(relayNumber, out var thermostatAdditionalTempChannel) ? thermostatAdditionalTempChannel : thermostatAdditionalTempChannelByIndex.TryGetValue(0, out var legacyAdditionalTempChannel) ? legacyAdditionalTempChannel : 0;
                var thermostatHisteresisValue = thermostatHisteresisByIndex.TryGetValue(relayNumber, out var thermostatHisteresis) ? thermostatHisteresis : thermostatHisteresisByIndex.TryGetValue(0, out var legacyThermostatHisteresis) ? legacyThermostatHisteresis : string.Empty;
                var thermostatMinTempValue = thermostatMinTempByIndex.TryGetValue(relayNumber, out var thermostatMinTemp) ? thermostatMinTemp : thermostatMinTempByIndex.TryGetValue(0, out var legacyThermostatMinTemp) ? legacyThermostatMinTemp : string.Empty;
                var thermostatMaxTempValue = thermostatMaxTempByIndex.TryGetValue(relayNumber, out var thermostatMaxTemp) ? thermostatMaxTemp : thermostatMaxTempByIndex.TryGetValue(0, out var legacyThermostatMaxTemp) ? legacyThermostatMaxTemp : string.Empty;

                if (_flag.Parameters != null)
                {
                    var gpioParam = _flag.Parameters.FirstOrDefault(p => string.Equals(p?.Key, $"GPIO{i + 1}", StringComparison.OrdinalIgnoreCase));
                    if (gpioParam != null && int.TryParse(gpioParam.Value, out var gpio))
                        gpioValue = gpio;

                    var stateParam = _flag.Parameters.FirstOrDefault(p => string.Equals(p?.Key, $"GPIO{i + 1}State", StringComparison.OrdinalIgnoreCase));
                    if (stateParam != null && int.TryParse(stateParam.Value, out var state))
                        stateValue = state;

                    var lightParam = _flag.Parameters.FirstOrDefault(p => string.Equals(p?.Key, $"GPIO{i + 1}LightControl", StringComparison.OrdinalIgnoreCase));
                    if (lightParam == null)
                    {
                        lightParam = _flag.Parameters.FirstOrDefault(p => string.Equals(p?.Key, $"GPIO{i + 1}Light", StringComparison.OrdinalIgnoreCase));
                    }
                    if (lightParam != null && int.TryParse(lightParam.Value, out var light))
                        lightValue = light;

                    var resetParam = _flag.Parameters.FirstOrDefault(p => string.Equals(p?.Key, $"GPIO{i + 1}ReactionAfterReset", StringComparison.OrdinalIgnoreCase));
                    if (resetParam == null)
                    {
                        resetParam = _flag.Parameters.FirstOrDefault(p => string.Equals(p?.Key, $"GPIO{i + 1}AfterResetReaction", StringComparison.OrdinalIgnoreCase));
                    }
                    if (resetParam != null && int.TryParse(resetParam.Value, out var reset))
                        afterResetReaction = reset;

                    var ledGpioParam = _flag.Parameters.FirstOrDefault(p => string.Equals(p?.Key, $"GPIO{i + 1}LedGPIO", StringComparison.OrdinalIgnoreCase));
                    if (ledGpioParam == null)
                        ledGpioParam = _flag.Parameters.FirstOrDefault(p => string.Equals(p?.Key, $"GPIO{i + 1}LedGpio", StringComparison.OrdinalIgnoreCase));
                    if (ledGpioParam != null && int.TryParse(ledGpioParam.Value, out var ledGpio))
                        ledGpioValue = ledGpio;

                    var ledActivationStateParam = _flag.Parameters.FirstOrDefault(p => string.Equals(p?.Key, $"GPIO{i + 1}LedActivationState", StringComparison.OrdinalIgnoreCase));
                    if (ledActivationStateParam == null)
                        ledActivationStateParam = _flag.Parameters.FirstOrDefault(p => string.Equals(p?.Key, $"GPIO{i + 1}ActivationState", StringComparison.OrdinalIgnoreCase));
                    if (ledActivationStateParam != null &&
                        int.TryParse(ledActivationStateParam.Value, out var ledActivationStateValue))
                        ledActivationState = ledActivationStateValue;
                }

                _entries.Add(new RelayEntryViewModel
                {
                    Index = i,
                    IsEnabled = i < enabledRelayCount,
                    Gpio = gpioValue,
                    State = stateValue,
                    Light = lightValue,
                    AfterResetReaction = afterResetReaction,
                    LedGpio = ledGpioValue,
                    LedActivationState = ledActivationState,
                    DirectLinksOn = directLinksOnValue,
                    DirectLinksOff = directLinksOffValue,
                    ThermostatType = thermostatTypeValue,
                    ThermostatMainTempChannel = thermostatMainTempChannelValue,
                    ThermostatAdditionalTempChannel = thermostatAdditionalTempChannelValue,
                    ThermostatHisteresis = thermostatHisteresisValue,
                    ThermostatMinTemp = thermostatMinTempValue,
                    ThermostatMaxTemp = thermostatMaxTempValue
                });
            }
            if (_entries.Count == 0)
            {
                for (var i = 0; i < _maxRelays; i++)
                {
                    _entries.Add(new RelayEntryViewModel
                    {
                        Index = i,
                        IsEnabled = i < enabledRelayCount,
                        Gpio = 0,
                        State = 0,
                        Light = 0,
                        AfterResetReaction = 0,
                        LedGpio = 0,
                        LedActivationState = 0,
                        DirectLinksOn = string.Empty,
                        DirectLinksOff = string.Empty,
                        ThermostatType = 0,
                        ThermostatMainTempChannel = 0,
                        ThermostatAdditionalTempChannel = 0,
                        ThermostatHisteresis = string.Empty,
                        ThermostatMinTemp = string.Empty,
                        ThermostatMaxTemp = string.Empty
                    });
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var enabledEntries = _entries.Where(entry => entry.IsEnabled).ToList();
            var parameters = new List<Parameter>
            {
                new Parameter { Key = "Count", Name = "Count", Type = "number", Value = enabledEntries.Count.ToString() }
            };

            var hasLedFlag = _allFlags.Any(flag => flag != null && string.Equals(flag.Key, "SUPLA_LED", StringComparison.OrdinalIgnoreCase) && flag.IsEnabled);
            var hasDirectLinksFlag = _allFlags.Any(flag => flag != null && string.Equals(flag.Key, "SUPLA_DIRECT_LINKS", StringComparison.OrdinalIgnoreCase) && flag.IsEnabled);
            var hasThermostatFlag = _allFlags.Any(flag => flag != null && string.Equals(flag.Key, "SUPLA_THERMOSTAT", StringComparison.OrdinalIgnoreCase) && flag.IsEnabled);

            foreach (var entry in enabledEntries)
            {
                if (!_gpioOptions.Any(option => option.Value == entry.Gpio))
                {
                    MessageBox.Show(LocalizationManager.GetFormat("RelayInvalidGpio", entry.Index + 1, string.IsNullOrWhiteSpace(_boardName) ? LocalizationManager.Get("Board") : _boardName), LocalizationManager.Get("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (entry.Gpio <= 0)
                {
                    MessageBox.Show(LocalizationManager.GetFormat("RelayInvalidGpio", entry.Index + 1, string.IsNullOrWhiteSpace(_boardName) ? LocalizationManager.Get("Board") : _boardName), LocalizationManager.Get("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (entry.State is < 0 or > 1)
                {
                    MessageBox.Show(LocalizationManager.GetFormat("RelayInvalidState", entry.Index + 1), LocalizationManager.Get("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (entry.AfterResetReaction is < 0 or > 2)
                {
                    MessageBox.Show(LocalizationManager.GetFormat("RelayInvalidAfterReset", entry.Index + 1), LocalizationManager.Get("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var relayIndex = entry.Index + 1;
                parameters.Add(new Parameter { Key = $"GPIO{relayIndex}", Name = $"GPIO{relayIndex}", Type = "number", Value = entry.Gpio.ToString() });
                parameters.Add(new Parameter
                {
                    Key = $"GPIO{relayIndex}State",
                    Name = $"GPIO{relayIndex}State",
                    Type = "enum",
                    Value = entry.State.ToString(),
                    EnumValues = new List<EnumValue>
                    {
                        new() { Value = "0", Name = LocalizationManager.Get("RelayStateLow") },
                        new() { Value = "1", Name = LocalizationManager.Get("RelayStateHigh") }
                    }
                });
                parameters.Add(new Parameter
                {
                    Key = $"GPIO{relayIndex}LightControl",
                    Name = $"GPIO{relayIndex}LightControl",
                    Type = "enum",
                    Value = entry.Light.ToString(),
                    EnumValues = new List<EnumValue>
                    {
                        new() { Value = "0", Name = LocalizationManager.Get("RelayLightNo") },
                        new() { Value = "1", Name = LocalizationManager.Get("RelayLightYes") }
                    }
                });
                parameters.Add(new Parameter
                {
                    Key = $"GPIO{relayIndex}ReactionAfterReset",
                    Name = $"GPIO{relayIndex}ReactionAfterReset",
                    Type = "enum",
                    Value = entry.AfterResetReaction.ToString(),
                    EnumValues = new List<EnumValue>
                    {
                        new() { Value = "0", Name = LocalizationManager.Get("RelayAfterResetOff") },
                        new() { Value = "1", Name = LocalizationManager.Get("RelayAfterResetOn") },
                        new() { Value = "2", Name = LocalizationManager.Get("RelayAfterResetRememberState") }
                    }
                });

                if (hasLedFlag)
                {
                    if (entry.LedGpio > 0 && !_gpioOptions.Any(option => option.Value == entry.LedGpio))
                    {
                        MessageBox.Show(LocalizationManager.GetFormat("RelayInvalidGpio", entry.Index + 1, string.IsNullOrWhiteSpace(_boardName) ? LocalizationManager.Get("Board") : _boardName), LocalizationManager.Get("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (entry.LedGpio < 0 || entry.LedActivationState is < 0 or > 1)
                    {
                        MessageBox.Show(LocalizationManager.GetFormat("RelayInvalidState", entry.Index + 1), LocalizationManager.Get("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    parameters.Add(new Parameter { Key = $"GPIO{relayIndex}LedGPIO", Name = $"GPIO{relayIndex}LedGPIO", Type = "number", Value = entry.LedGpio.ToString() });
                    parameters.Add(new Parameter
                    {
                        Key = $"GPIO{relayIndex}LedActivationState",
                        Name = $"GPIO{relayIndex}LedActivationState",
                        Type = "enum",
                        Value = entry.LedActivationState.ToString(),
                        EnumValues = new List<EnumValue>
                        {
                            new() { Value = "0", Name = LocalizationManager.Get("RelayStateLow") },
                            new() { Value = "1", Name = LocalizationManager.Get("RelayStateHigh") }
                        }
                    });
                }

                if (hasDirectLinksFlag)
                {
                    var directLinksOn = entry.DirectLinksOn ?? string.Empty;
                    var directLinksOff = entry.DirectLinksOff ?? string.Empty;
                    if (directLinksOn.Length > 32 || directLinksOff.Length > 32)
                    {
                        MessageBox.Show("SUPLA_DIRECT_LINKS parameters must be at most 32 characters long.", LocalizationManager.Get("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    parameters.Add(new Parameter { Key = $"GPIO{relayIndex}DirectLinksOn", Name = $"GPIO{relayIndex}DirectLinksOn", Type = "string", Value = directLinksOn });
                    parameters.Add(new Parameter { Key = $"GPIO{relayIndex}DirectLinksOff", Name = $"GPIO{relayIndex}DirectLinksOff", Type = "string", Value = directLinksOff });
                }

                if (hasThermostatFlag)
                {
                    var thermostatType = entry.ThermostatType;
                    var thermostatMainTempChannel = entry.ThermostatMainTempChannel;
                    var thermostatAdditionalTempChannel = entry.ThermostatAdditionalTempChannel;
                    var thermostatHisteresis = ParseThermostatDecimal(entry.ThermostatHisteresis ?? string.Empty);
                    var thermostatMinTemp = ParseThermostatDecimal(entry.ThermostatMinTemp ?? string.Empty);
                    var thermostatMaxTemp = ParseThermostatDecimal(entry.ThermostatMaxTemp ?? string.Empty);

                    parameters.Add(new Parameter
                    {
                        Key = $"GPIO{relayIndex}ThermostatType",
                        Name = $"GPIO{relayIndex}ThermostatType",
                        Type = "enum",
                        Value = thermostatType.ToString(),
                        EnumValues = new List<EnumValue>
                        {
                            new() { Value = "0", Name = "OFF" },
                            new() { Value = "1", Name = "Heat" },
                            new() { Value = "2", Name = "Cool" },
                            new() { Value = "3", Name = "Auto" },
                            new() { Value = "4", Name = "DomesticHotWater" },
                            new() { Value = "5", Name = "Differential" }
                        }
                    });
                    parameters.Add(new Parameter { Key = $"GPIO{relayIndex}ThermostatMainTempChannel", Name = $"GPIO{relayIndex}ThermostatMainTempChannel", Type = "number", Value = thermostatMainTempChannel.ToString() });
                    parameters.Add(new Parameter { Key = $"GPIO{relayIndex}ThermostatAdditionalTempChannel", Name = $"GPIO{relayIndex}ThermostatAdditionalTempChannel", Type = "number", Value = thermostatAdditionalTempChannel.ToString() });
                    parameters.Add(new Parameter { Key = $"GPIO{relayIndex}ThermostatHisteresis", Name = $"GPIO{relayIndex}ThermostatHisteresis", Type = "number", Value = thermostatHisteresis });
                    parameters.Add(new Parameter { Key = $"GPIO{relayIndex}ThermostatMinTemp", Name = $"GPIO{relayIndex}ThermostatMinTemp", Type = "number", Value = thermostatMinTemp });
                    parameters.Add(new Parameter { Key = $"GPIO{relayIndex}ThermostatMaxTemp", Name = $"GPIO{relayIndex}ThermostatMaxTemp", Type = "number", Value = thermostatMaxTemp });
                }
            }

            if (parameters.Count == 1 && enabledEntries.Count == 0)
            {
                parameters[0].Value = "0";
            }

            _flag.Parameters = parameters;
            DialogResult = true;
            Close();
        }
    }
}
