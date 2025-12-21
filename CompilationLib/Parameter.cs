using Newtonsoft.Json;
using System.Collections.Generic;

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
        /// Gets the identifier for this parameter. Prefers Key if set, falls back to Name for backward compatibility.
        /// </summary>
        [JsonIgnore]
        public string Identifier => !string.IsNullOrEmpty(Key) ? Key : Name;
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