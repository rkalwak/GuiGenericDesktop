using System.IO;
using System.Windows;
using Markdig;
using GuiGenericBuilderDesktop.Localization;

namespace GuiGenericBuilderDesktop
{
    /// <summary>
    /// Window for displaying Help.md with proper Markdown rendering
    /// </summary>
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
            Loaded += HelpWindow_Loaded;
            
            // Subscribe to language changes
            LocalizationManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            // Reload help when language changes
            if (webView?.CoreWebView2 != null)
            {
                LoadHelp();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // Unsubscribe from language changes to prevent memory leaks
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
        }

        private async void HelpWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize WebView2
                await webView.EnsureCoreWebView2Async(null);
                LoadHelp();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize help viewer: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadHelp()
        {
            try
            {
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var currentLanguage = LocalizationManager.CurrentLanguage;
                
                // Try to load language-specific help file first
                var localizedHelpPath = Path.Combine(baseDirectory, $"Help.{currentLanguage}.md");
                var defaultHelpPath = Path.Combine(baseDirectory, "Help.md");
                
                string helpPath;
                if (File.Exists(localizedHelpPath))
                {
                    helpPath = localizedHelpPath;
                }
                else if (File.Exists(defaultHelpPath))
                {
                    helpPath = defaultHelpPath;
                }
                else
                {
                    webView.NavigateToString(CreateErrorHtml("Help file not found"));
                    return;
                }

                var markdown = File.ReadAllText(helpPath);
                var html = Markdown.ToHtml(markdown);

                var styledHtml = CreateStyledHtml(html);
                webView.NavigateToString(styledHtml);
            }
            catch (Exception ex)
            {
                webView.NavigateToString(CreateErrorHtml($"Error loading help: {ex.Message}"));
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
            line-height: 1.7;
            color: #333;
            max-width: 1100px;
            margin: 0 auto;
            padding: 30px 40px;
            background-color: #ffffff;
        }}
        
        h1 {{
            color: #2c3e50;
            border-bottom: 3px solid #27ae60;
            padding-bottom: 12px;
            margin-top: 40px;
            margin-bottom: 20px;
            font-size: 2.4em;
            text-align: center;
            background: linear-gradient(135deg, #27ae60 0%, #219653 100%);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            background-clip: text;
        }}
        
        h2 {{
            color: #2c3e50;
            margin-top: 45px;
            margin-bottom: 18px;
            border-left: 5px solid #27ae60;
            padding-left: 15px;
            padding-top: 5px;
            padding-bottom: 5px;
            font-size: 1.9em;
            background: linear-gradient(to right, #f0f9f4 0%, transparent 100%);
        }}
        
        h3 {{
            color: #34495e;
            margin-top: 30px;
            margin-bottom: 15px;
            font-size: 1.5em;
            border-bottom: 2px solid #ecf0f1;
            padding-bottom: 8px;
        }}
        
        h4 {{
            color: #555;
            margin-top: 25px;
            margin-bottom: 12px;
            font-size: 1.2em;
            font-weight: 600;
        }}
        
        code {{
            background-color: #f4f7f9;
            padding: 2px 7px;
            border-radius: 4px;
            font-family: 'Courier New', 'Consolas', monospace;
            color: #27ae60;
            font-size: 0.9em;
            border: 1px solid #e1e8ed;
        }}
        
        pre {{
            background-color: #2c3e50;
            color: #ecf0f1;
            padding: 18px;
            border-radius: 6px;
            overflow-x: auto;
            border-left: 4px solid #27ae60;
            box-shadow: 0 2px 5px rgba(0,0,0,0.1);
        }}
        
        pre code {{
            background-color: transparent;
            color: #ecf0f1;
            padding: 0;
            border: none;
        }}
        
        ul, ol {{
            padding-left: 35px;
            margin: 12px 0;
        }}
        
        li {{
            margin-bottom: 10px;
            line-height: 1.6;
        }}
        
        ul ul, ol ol {{
            margin-top: 8px;
            margin-bottom: 8px;
        }}
        
        blockquote {{
            border-left: 4px solid #27ae60;
            padding-left: 18px;
            margin-left: 0;
            color: #555;
            font-style: italic;
            background-color: #f0f9f4;
            padding: 12px 18px;
            border-radius: 0 5px 5px 0;
            margin: 18px 0;
        }}
        
        a {{
            color: #27ae60;
            text-decoration: none;
            border-bottom: 1px solid transparent;
            transition: border-bottom 0.2s, color 0.2s;
        }}
        
        a:hover {{
            color: #219653;
            border-bottom: 1px solid #27ae60;
        }}
        
        strong {{
            color: #2c3e50;
            font-weight: 700;
        }}
        
        em {{
            color: #555;
        }}
        
        hr {{
            border: none;
            border-top: 2px solid #ecf0f1;
            margin: 35px 0;
        }}
        
        table {{
            border-collapse: collapse;
            width: 100%;
            margin: 25px 0;
            box-shadow: 0 2px 5px rgba(0,0,0,0.05);
        }}
        
        table th {{
            background-color: #27ae60;
            color: white;
            padding: 14px;
            text-align: left;
            font-weight: 600;
        }}
        
        table td {{
            padding: 12px 14px;
            border-bottom: 1px solid #ecf0f1;
        }}
        
        table tr:hover {{
            background-color: #f0f9f4;
        }}
        
        /* Info boxes */
        p strong:first-child {{
            background-color: #fff3cd;
            padding: 2px 8px;
            border-radius: 3px;
            color: #856404;
        }}
        
        /* Section dividers */
        h2:not(:first-of-type) {{
            margin-top: 60px;
        }}
        
        /* Step numbers */
        h3:has(+ ol) {{
            color: #27ae60;
        }}
        
        /* Keyboard shortcuts */
        kbd {{
            background-color: #f4f7f9;
            border: 1px solid #ccc;
            border-radius: 3px;
            box-shadow: 0 1px 0 rgba(0,0,0,0.2);
            color: #333;
            display: inline-block;
            font-family: 'Courier New', monospace;
            font-size: 0.85em;
            line-height: 1;
            padding: 3px 5px;
            white-space: nowrap;
        }}
        
        /* Note sections */
        p:has(strong:first-child) {{
            background-color: #e8f5e9;
            border-left: 4px solid #27ae60;
            padding: 12px 15px;
            border-radius: 0 5px 5px 0;
            margin: 15px 0;
        }}
        
        /* Problem/Solution sections */
        h3 + p {{
            color: #666;
            font-style: italic;
        }}
        
        /* Scrollbar styling */
        ::-webkit-scrollbar {{
            width: 12px;
        }}
        
        ::-webkit-scrollbar-track {{
            background: #f1f1f1;
        }}
        
        ::-webkit-scrollbar-thumb {{
            background: #27ae60;
            border-radius: 6px;
        }}
        
        ::-webkit-scrollbar-thumb:hover {{
            background: #219653;
        }}
        
        /* Top section styling */
        body > h1:first-child {{
            margin-top: 0;
            padding-top: 20px;
        }}
        
        /* Footer styling */
        p:has(em):last-of-type {{
            text-align: center;
            color: #999;
            margin-top: 50px;
            padding-top: 30px;
            border-top: 2px solid #ecf0f1;
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
