using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Newtonsoft.Json;
using CompilationLib;

namespace GuiGenericBuilderDesktop
{
    public class SectionInfo
    {
        public string Name { get; set; }

        public int Order { get; set; }
        [JsonProperty("Flags")]
        public Dictionary<string, BuildFlagItem> Flags { get; set; } = new Dictionary<string, BuildFlagItem>();
    }

}