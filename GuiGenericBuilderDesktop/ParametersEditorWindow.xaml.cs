using CompilationLib;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Globalization;
using GuiGenericBuilderDesktop.Localization;

namespace GuiGenericBuilderDesktop
{
    public partial class ParametersEditorWindow : Window
    {
        private readonly GlobalSettings _globalSettings;
        private readonly string _boardName;
        private List<Parameter> _parameters;
        private bool _handlingRowEditEnding;

        public ParametersEditorWindow(List<Parameter> parameters, string flagTitle, GlobalSettings globalSettings = null, string boardName = null)
        {
            InitializeComponent();
            FlagTitle.Text = flagTitle;
            _globalSettings = globalSettings ?? new GlobalSettings();
            _boardName = boardName ?? string.Empty;

            // Do not allow adding new rows via the UI
            ParamsGrid.CanUserAddRows = false;

            PrepareParameterOptions(parameters);
            ParamsGrid.ItemsSource = parameters;
            _parameters = parameters;

            // Safely retrieve templates from the DataGrid scope (avoid exceptions if missing)
            var textTemplate = ParamsGrid.TryFindResource("TextTemplate") as DataTemplate;
            var numberTemplate = ParamsGrid.TryFindResource("NumberTemplate") as DataTemplate;
            var enumTemplate = ParamsGrid.TryFindResource("EnumTemplate") as DataTemplate;

            if (textTemplate != null && numberTemplate != null && enumTemplate != null)
            {
                this.Resources["TextTemplate"] = textTemplate;
                this.Resources["NumberTemplate"] = numberTemplate;
                this.Resources["EnumTemplate"] = enumTemplate;

                // Add template selector resource
                this.Resources["ValueEditorSelector"] = new ValueEditorTemplateSelector
                {
                    TextTemplate = textTemplate,
                    NumberTemplate = numberTemplate,
                    EnumTemplate = enumTemplate
                };
            }
        }

        private void PrepareParameterOptions(List<Parameter> parameters)
        {
            if (parameters == null)
            {
                return;
            }

            foreach (var parameter in parameters)
            {
                if (parameter == null)
                {
                    continue;
                }

                parameter.PopulateGpioEnumValues(_globalSettings, _boardName);
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateParameters())
            {
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateParameters()
        {
            // Collect all validation errors first
            var errors = new List<string>();
            
            // Validate that all required parameters have values
            foreach (var param in _parameters)
            {
                // Only validate if parameter is required
                if (param.IsRequired && string.IsNullOrWhiteSpace(param.Value))
                {
                    var paramName = param.GetLocalizedName();
                    errors.Add($"� {paramName} (required)");
                }
                
                // For enum/gpio types, validate that the selected value exists in EnumValues, but keep gpio values numeric.
                if (!string.IsNullOrWhiteSpace(param.Value))
                {
                    var type = param.Type ?? string.Empty;
                    if (string.Equals(type, "gpio", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!int.TryParse(param.Value, out _))
                        {
                            var paramName = param.GetLocalizedName();
                            errors.Add($"� {paramName} (invalid GPIO value)");
                        }
                        else if (param.EnumValues != null && param.EnumValues.Any() &&
                                 !param.EnumValues.Any(ev => ev.Value == param.Value))
                        {
                            var paramName = param.GetLocalizedName();
                            errors.Add($"� {paramName} (invalid GPIO value)");
                        }
                    }
                    else if (string.Equals(type, "enum", StringComparison.OrdinalIgnoreCase) &&
                             param.EnumValues != null &&
                             param.EnumValues.Any())
                    {
                        if (!param.EnumValues.Any(ev => ev.Value == param.Value))
                        {
                            var paramName = param.GetLocalizedName();
                            errors.Add($"� {paramName} (invalid value)");
                        }
                    }
                }
            }
            
            // If there are errors, show them all at once
            if (errors.Any())
            {
                var errorMessage = "Please fix the following validation errors:\n\n" + string.Join("\n", errors);
                MessageBox.Show(
                    errorMessage,
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }
            
            return true;
        }

        private void ParamsGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (_handlingRowEditEnding) return;

            try
            {
                _handlingRowEditEnding = true;
                ParamsGrid.CommitEdit(DataGridEditingUnit.Row, true);
                
                // Don't validate here - let user finish editing all parameters
                // Validation will happen when user clicks OK button
            }
            finally
            {
                _handlingRowEditEnding = false;
            }
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow digits, decimal separator, and minus sign
            e.Handled = !e.Text.All(c => char.IsDigit(c) || c == '.' || c == ',' || c == '-');
        }
    }

    public class ValueEditorTemplateSelector : DataTemplateSelector
    {
        public DataTemplate NumberTemplate { get; set; }
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate EnumTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is Parameter p)
            {
                var t = (p.Type ?? string.Empty).Trim().ToLowerInvariant();
                
                // Check for enum/gpio types - should use dropdown when options are available
                if ((t == "enum" || t == "gpio") && p.EnumValues != null && p.EnumValues.Any())
                {
                    return EnumTemplate;
                }

                // Check for number/gpio types - fallback to integer input when no board options are available
                if (t == "number" || t == "gpio")
                {
                    return NumberTemplate;
                }
                
                // Default to text input
                return TextTemplate;
            }
            return base.SelectTemplate(item, container);
        }
    }

    /// <summary>
    /// Converter to get localized parameter name
    /// </summary>
    public class ParameterNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Parameter param)
            {
                return param.GetLocalizedName();
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter to get localized enum value display (Name - Description)
    /// </summary>
    public class EnumValueDisplayConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is EnumValue enumValue && values[1] is Parameter param)
            {
                var localizedName = enumValue.GetLocalizedName(param);
                var localizedDesc = enumValue.GetLocalizedDescription(param);
                
                if (!string.IsNullOrEmpty(localizedDesc))
                {
                    return $"{localizedName} - {localizedDesc}";
                }
                return localizedName;
            }
            return string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnumValueToNameConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[1] is Parameter param)
            {
                var currentValue = values[0]?.ToString() ?? string.Empty;
                
                if ((param.Type?.ToLowerInvariant() == "enum" || param.Type?.ToLowerInvariant() == "gpio") && param.EnumValues != null)
                {
                    var enumValue = param.EnumValues.FirstOrDefault(ev => ev.Value == currentValue);
                    if (enumValue != null)
                    {
                        var localizedName = enumValue.GetLocalizedName(param);
                        return $"{localizedName} ({enumValue.Value})";
                    }
                }
            }
            
            return values[0]?.ToString() ?? string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
