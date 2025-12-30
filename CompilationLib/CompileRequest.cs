using CompilationLib;

public class CompileRequest
{
    [System.ComponentModel.DefaultValue("GUI_Generic_ESP32")]
    public string Platform { get; set; }

    public List<BuildFlagItem> BuildFlags { get; set; } = new List<BuildFlagItem>() {  };
    public string ProjectName { get; set; }
    public string ProjectPath { get; set; }
    public string ProjectDirectory { get; set; }
    public string LibrariesPath { get; set; }
    public string PortCom { get;  set; }
    public bool ShouldDeploy { get; set; }
    public bool ShouldBackup { get; set; }
    public bool ShouldEraseFlash { get; set; }
}