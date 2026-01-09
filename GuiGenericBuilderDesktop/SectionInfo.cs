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
        
        /// <summary>
        /// Translations for section name. Key is language code (e.g., "en", "pl"), value is the translated name.
        /// </summary>
        [JsonProperty("Translations")]
        public Dictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();
    }

}