using Newtonsoft.Json;

namespace GuiGenericBuilderDesktop
{
    public class BuilderConfig
    {

        [JsonProperty("Sections")]
        public Dictionary<string, SectionInfo> Sections { get; set; } = new Dictionary<string, SectionInfo>();

        [JsonProperty("version")]
        public string Version { get; set; }
    }

}