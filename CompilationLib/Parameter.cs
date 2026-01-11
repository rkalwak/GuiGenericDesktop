using Newtonsoft.Json;

namespace CompilationLib
{
    public class Parameter
    {
        [JsonProperty("key")]
        public string Key { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("type")]
        public string Type { get; set; }

        // Optional sample/default value (not serialized in existing files unless written back)
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("defaultValue")]
        public string DefaultValue { get; set; }

        [JsonProperty("isRequired")]
        public bool IsRequired { get; set; }

        [JsonProperty("enumValues")]
        public List<EnumValue> EnumValues { get; set; } = new List<EnumValue>();
        
        /// <summary>
        /// Translations for this parameter. Key is language code (e.g., "en", "pl"), value is ParameterTranslation object.
        /// </summary>
        [JsonProperty("Translations")]
        public Dictionary<string, ParameterTranslation> Translations { get; set; } = new Dictionary<string, ParameterTranslation>();
        
        /// <summary>
        /// Gets the identifier for this parameter. Prefers Key if set, falls back to Name for backward compatibility.
        /// </summary>
        [JsonIgnore]
        public string Identifier => !string.IsNullOrEmpty(Key) ? Key : Name;
    }

    /// <summary>
    /// Represents translations for a single parameter in a specific language
    /// </summary>
    public class ParameterTranslation
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("EnumValues")]
        public List<EnumValue> EnumValues { get; set; } = new List<EnumValue>();
    }

    public class EnumValue
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Description))
            {
                return $"{Name} - {Description}";
            }
            return Name ?? Value;
        }
    }
}