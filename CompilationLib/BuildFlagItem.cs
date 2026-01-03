using System.ComponentModel;
using Newtonsoft.Json;

namespace CompilationLib
{
    public class BuildFlagItem : INotifyPropertyChanged
    {
        private bool _isEnabled;

        public string Key { set; get; }
        [JsonProperty("name")]
        public string FlagName { get; set; }
        [JsonProperty("desc")]
        public string Description { get; set; }
        public string Section { get; set; }
        [JsonProperty("defOn")]
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the list of flags that will auto-enable THIS flag when any of them is enabled.
        /// In builder.json, this is `depOn` - this flag depends on these flags being enabled.
        /// Example: SUPLA_RELAY has depOn:["SUPLA_ROLLERSHUTTER"] means when ROLLERSHUTTER is enabled, RELAY auto-enables.
        /// </summary>
        [JsonProperty("depOn")]
        public List<string> EnabledByFlags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of flags that THIS flag will disable when enabled (mutual exclusion).
        /// In builder.json, this is `depRel` - relationship/release flags that conflict with this flag.
        /// Example: SUPLA_NTC_10K has depRel:["SUPLA_MPX_5XXX"] means enabling NTC disables MPX (both use analog pin).
        /// </summary>
        [JsonProperty("depRel")]
        public List<string> DependenciesToDisable { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of flags that THIS flag will auto-enable when enabled.
        /// In builder.json, this is `depOpt` - optional dependencies that this flag enables.
        /// Example: SUPLA_THERMOSTAT has depOpt:["SUPLA_RELAY","SUPLA_LED"] means enabling THERMOSTAT enables RELAY and LED.
        /// </summary>
        [JsonProperty("depOpt")]
        public List<string> DependenciesToEnable { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of flags that MUST be enabled before THIS flag can be enabled (required prerequisites).
        /// In builder.json, this is `depOff` - flags that when disabled/off block this flag.
        /// Example: SUPLA_BUTTON has depOff:["SUPLA_RELAY"] means RELAY must be enabled before BUTTON can be enabled.
        /// </summary>
        [JsonProperty("depOff")]
        public List<string> BlockedByDisabledFlags { get; set; } = new List<string>();
        public int SectionOrder { get; internal set; }

        [JsonProperty("parameters")]
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();
        [JsonProperty("disabledOnPlatforms")]
        public List<string> DisabledOnPlatforms { get; set; } = new List<string>();

        public event PropertyChangedEventHandler PropertyChanged;
    }
}