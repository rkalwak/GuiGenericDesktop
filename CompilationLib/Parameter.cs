using Newtonsoft.Json;

namespace CompilationLib
{
    public class Parameter
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }

        // Optional sample/default value (not serialized in existing files unless written back)
        [JsonProperty("value")]
        public string? Value { get; set; }
    }
}