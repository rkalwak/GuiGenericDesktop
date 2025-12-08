using CompilationLib;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

            // simple list of types
            this.Tag = new List<string> { "string", "number", "boolean" };

            // Provide suggestions for parameter names as a resource
            var nameSuggestions = new List<string>
            {
                "Altitude",
                "Interval",
                "Threshold",
                "Enabled",
                "Pin",
                "Address",
                "BaudRate"
            };
            this.Resources["NameSuggestions"] = nameSuggestions;

            // Safely retrieve templates from the DataGrid scope (avoid exceptions if missing)
            var textTemplate = ParamsGrid.TryFindResource("TextTemplate") as DataTemplate;
            var numberTemplate = ParamsGrid.TryFindResource("NumberTemplate") as DataTemplate;
            if (textTemplate != null && numberTemplate != null)
            {
                this.Resources["TextTemplate"] = textTemplate;
                this.Resources["NumberTemplate"] = numberTemplate;

                // Add template selector resource
                this.Resources["ValueEditorSelector"] = new ValueEditorTemplateSelector
                {
                    TextTemplate = textTemplate,
                    NumberTemplate = numberTemplate
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
            // Check for empty names
            var empty = _parameters.FirstOrDefault(p => string.IsNullOrWhiteSpace(p.Name));
            if (empty != null)
            {
                MessageBox.Show("Parameter name cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Check for duplicates (case-insensitive)
            var dup = _parameters.GroupBy(p => p.Name?.Trim().ToLowerInvariant()).FirstOrDefault(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key));
            if (dup != null)
            {
                MessageBox.Show($"Duplicate parameter name found: '{dup.Key}'. Each parameter name must be unique.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                if (!ValidateParameters())
                {
                    var source = _parameters.ToList();
                    ParamsGrid.ItemsSource = null;
                    ParamsGrid.ItemsSource = source;
                    _parameters.Clear();
                    _parameters.AddRange(source);
                }
            }
            finally
            {
                _handlingRowEditEnding = false;
            }
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow digits and decimal separator
            e.Handled = !e.Text.All(c => char.IsDigit(c) || c == '.' || c == ',');
        }
    }

    public class ValueEditorTemplateSelector : DataTemplateSelector
    {
        public DataTemplate NumberTemplate { get; set; }
        public DataTemplate TextTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is Parameter p)
            {
                var t = (p.Type ?? string.Empty).Trim().ToLowerInvariant();
                if (t == "number") return NumberTemplate;
                return TextTemplate;
            }
            return base.SelectTemplate(item, container);
        }
    }
}
