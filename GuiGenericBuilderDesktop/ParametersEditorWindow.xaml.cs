using CompilationLib;
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
            // Parameter names are now fixed, no need to validate them
            // Could add value validation here if needed in the future
            
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
