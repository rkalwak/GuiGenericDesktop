using System.IO;
using System.Windows;
using Markdig;

namespace GuiGenericBuilderDesktop
{
    /// <summary>
    /// Window for displaying Changelog.md with proper Markdown rendering
    /// </summary>
    public partial class ChangelogWindow : Window
    {
        public ChangelogWindow()
        {
            InitializeComponent();
            Loaded += ChangelogWindow_Loaded;
        }

        private async void ChangelogWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize WebView2
                await webView.EnsureCoreWebView2Async(null);
                LoadChangelog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize changelog viewer: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadChangelog()
        {
            try
            {
                var changelogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Changelog.md");

                if (!File.Exists(changelogPath))
                {
                    webView.NavigateToString(CreateErrorHtml("Changelog file not found"));
                    return;
                }

                var markdown = File.ReadAllText(changelogPath);
                var html = Markdown.ToHtml(markdown);

                var styledHtml = CreateStyledHtml(html);
                webView.NavigateToString(styledHtml);
            }
            catch (Exception ex)
            {
                webView.NavigateToString(CreateErrorHtml($"Error loading changelog: {ex.Message}"));
            }
        }

        private string CreateStyledHtml(string content)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 1100px;
            margin: 0 auto;
            padding: 30px 40px;
            background-color: #ffffff;
        }}
        
        h1 {{
            color: #2c3e50;
            border-bottom: 3px solid #3498db;
            padding-bottom: 12px;
            margin-top: 40px;
            margin-bottom: 20px;
            font-size: 2.2em;
        }}
        
        h2 {{
            color: #34495e;
            margin-top: 35px;
            margin-bottom: 15px;
            border-bottom: 2px solid #ecf0f1;
            padding-bottom: 8px;
            font-size: 1.8em;
            background: linear-gradient(to right, #f8f9fa 0%, transparent 100%);
            padding-left: 10px;
        }}
        
        h3 {{
            color: #555;
            margin-top: 25px;
            margin-bottom: 12px;
            font-size: 1.4em;
            border-left: 4px solid #3498db;
            padding-left: 12px;
        }}
        
        h4 {{
            color: #666;
            margin-top: 20px;
            margin-bottom: 10px;
            font-size: 1.1em;
        }}
        
        code {{
            background-color: #f4f4f4;
            padding: 2px 6px;
            border-radius: 3px;
            font-family: 'Courier New', 'Consolas', monospace;
            color: #e74c3c;
            font-size: 0.9em;
        }}
        
        pre {{
            background-color: #2c3e50;
            color: #ecf0f1;
            padding: 15px;
            border-radius: 5px;
            overflow-x: auto;
            border-left: 4px solid #3498db;
        }}
        
        pre code {{
            background-color: transparent;
            color: #ecf0f1;
            padding: 0;
        }}
        
        ul, ol {{
            padding-left: 30px;
            margin: 10px 0;
        }}
        
        li {{
            margin-bottom: 8px;
            line-height: 1.5;
        }}
        
        ul ul, ol ol {{
            margin-top: 5px;
            margin-bottom: 5px;
        }}
        
        blockquote {{
            border-left: 4px solid #3498db;
            padding-left: 15px;
            margin-left: 0;
            color: #555;
            font-style: italic;
            background-color: #f8f9fa;
            padding: 10px 15px;
            border-radius: 0 4px 4px 0;
        }}
        
        a {{
            color: #3498db;
            text-decoration: none;
            border-bottom: 1px solid transparent;
            transition: border-bottom 0.2s;
        }}
        
        a:hover {{
            border-bottom: 1px solid #3498db;
        }}
        
        strong {{
            color: #2c3e50;
            font-weight: 600;
        }}
        
        em {{
            color: #555;
        }}
        
        hr {{
            border: none;
            border-top: 2px solid #ecf0f1;
            margin: 30px 0;
        }}
        
        table {{
            border-collapse: collapse;
            width: 100%;
            margin: 20px 0;
        }}
        
        table th {{
            background-color: #3498db;
            color: white;
            padding: 12px;
            text-align: left;
            font-weight: 600;
        }}
        
        table td {{
            padding: 10px 12px;
            border-bottom: 1px solid #ecf0f1;
        }}
        
        table tr:hover {{
            background-color: #f8f9fa;
        }}
        
        /* Version badge styling */
        h2:first-of-type {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 15px 20px;
            border-radius: 8px;
            border-bottom: none;
        }}
        
        /* Feature sections */
        h3 + ul {{
            background-color: #f8f9fa;
            padding: 15px;
            border-radius: 5px;
            border-left: 4px solid #28a745;
        }}
        
        /* Scrollbar styling */
        ::-webkit-scrollbar {{
            width: 12px;
        }}
        
        ::-webkit-scrollbar-track {{
            background: #f1f1f1;
        }}
        
        ::-webkit-scrollbar-thumb {{
            background: #888;
            border-radius: 6px;
        }}
        
        ::-webkit-scrollbar-thumb:hover {{
            background: #555;
        }}
    </style>
</head>
<body>
{content}
</body>
</html>";
        }

        private string CreateErrorHtml(string errorMessage)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
            background-color: #f5f5f5;
        }}
        .error {{
            text-align: center;
            padding: 40px;
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        .error h1 {{
            color: #e74c3c;
            margin-bottom: 20px;
        }}
        .error p {{
            color: #666;
        }}
    </style>
</head>
<body>
    <div class='error'>
        <h1>?? Error</h1>
        <p>{errorMessage}</p>
    </div>
</body>
</html>";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
