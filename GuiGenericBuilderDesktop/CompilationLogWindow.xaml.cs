using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace GuiGenericBuilderDesktop
{
    /// <summary>
    /// Interaction logic for CompilationLogWindow.xaml
    /// </summary>
    public partial class CompilationLogWindow : Window
    {
        public CompilationLogWindow(string logs)
        {
            InitializeComponent();
            LogTextBox.Text = logs ?? "No logs available.";
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(LogTextBox.Text))
                {
                    Clipboard.SetText(LogTextBox.Text);
                    MessageBox.Show("Logs copied to clipboard successfully!", "Copy Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy to clipboard: {ex.Message}", "Copy Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Log files (*.log)|*.log|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    DefaultExt = ".log",
                    FileName = $"compilation_errors_{DateTime.Now:yyyyMMdd_HHmmss}.log",
                    Title = "Save Compilation Logs"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveFileDialog.FileName, LogTextBox.Text);
                    MessageBox.Show($"Logs saved successfully to:\n{saveFileDialog.FileName}", "Save Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save logs: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
