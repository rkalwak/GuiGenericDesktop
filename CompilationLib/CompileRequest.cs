using CompilationLib;

public class CompileRequest
{
    [System.ComponentModel.DefaultValue("GUI_Generic_ESP32")]
    public string EnvironmentName { get; set; }

    public List<BuildFlagItem> BuildFlags { get; set; } = new List<BuildFlagItem>() {  };
    public string ProjectName { get; set; }
    public string ProjectPath { get; set; }
    public string ProjectDirectory { get; set; }
    public string LibrariesPath { get; set; }
    public string PortCom { get;  set; }
    public bool ShouldDeploy { get; set; }
    public bool ShouldBackup { get; set; }
    public bool ShouldEraseFlash { get; set; }
    
    /// <summary>
    /// Flash size (e.g., "4MB", "8MB", "16MB", "32MB")
    /// </summary>
    public string FlashSize { get; set; }

    public GlobalSettings GlobalSettings { get; set; } = new GlobalSettings();
    public string Board { get; set; }
    
    /// <summary>
    /// Configuration timestamp in format yyyyMMdd_HHmmss for consistent naming of backup and config files
    /// </summary>
    public string ConfigTimestamp { get; set; }
}