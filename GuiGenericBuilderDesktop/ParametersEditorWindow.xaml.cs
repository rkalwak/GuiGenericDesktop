using CompilationLib;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Globalization;

namespace GuiGenericBuilderDesktop
{
    public partial class ParametersEditorWindow : Window
    {
        private List<Parameter> _parameters;
        private bool _handlingRowEditEnding;

        public ParametersEditorWindow(List<Parameter> parameters, string flagTitle)
        {
            InitializeComponent();
            FlagTitle.Text = flagTitle;

            // Do not allow adding new rows via the UI
            ParamsGrid.CanUserAddRows = false;

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
                    var paramName = !string.IsNullOrEmpty(param.Name) ? param.Name : param.Key;
                    errors.Add($"• {paramName} (required)");
                }
                
                // For enum type, validate that the value exists in EnumValues (only if value is provided)
                if (!string.IsNullOrWhiteSpace(param.Value) && 
                    param.Type?.ToLowerInvariant() == "enum" && 
                    param.EnumValues != null && 
                    param.EnumValues.Any())
                {
                    if (!param.EnumValues.Any(ev => ev.Value == param.Value))
                    {
                        var paramName = !string.IsNullOrEmpty(param.Name) ? param.Name : param.Key;
                        errors.Add($"• {paramName} (invalid value)");
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
                
                // Check for enum type - should use dropdown
                if (t == "enum" && p.EnumValues != null && p.EnumValues.Any())
                {
                    return EnumTemplate;
                }
                
                // Check for number type - should use number input
                if (t == "number")
                {
                    return NumberTemplate;
                }
                
                // Default to text input
                return TextTemplate;
            }
            return base.SelectTemplate(item, container);
        }
    }

    public class EnumValueToNameConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[1] is Parameter param)
            {
                var currentValue = values[0]?.ToString() ?? string.Empty;
                
                if (param.Type?.ToLowerInvariant() == "enum" && param.EnumValues != null)
                {
                    var enumValue = param.EnumValues.FirstOrDefault(ev => ev.Value == currentValue);
                    if (enumValue != null)
                    {
                        return $"{enumValue.Name} ({enumValue.Value})";
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
