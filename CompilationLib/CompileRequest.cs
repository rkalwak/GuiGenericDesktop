

using System.ComponentModel;

public class CompileRequest
{
    [System.ComponentModel.DefaultValue("GUI_Generic_ESP32")]
    public string Platform { get; set; }

    public List<string> BuildFlags { get; set; } = new List<string>() { "SUPLA_HDC1080" };
    public string ProjectName { get; set; }
    public string ProjectPath { get; set; }
    public string ProjectDirectory { get; set; }
    public string LibrariesPath { get; set; }
    public string PortCom { get;  set; }
}