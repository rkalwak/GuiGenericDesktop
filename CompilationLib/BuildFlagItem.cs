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
        //"[opcja] tablica SUPLA_OPTION, których włączenie automatycznie włącza daną opcję"
        [JsonProperty("depOn")]
        public List<string>? DependenciesToEnable { get; set; }

        //"[opcja] tablica SULPA_OPTION, które zostaną wyłączone po włączeniu danej opcji",
        [JsonProperty("depRel")]
        public List<string>? DepRel { get; set; }

        // "[opcja] tablica SULPA_OPTION, które zostaną włączone po włączeniu danej opcji",
        [JsonProperty("depOpt")]
        public List<string>? DependenciesOptional { get; set; }

        //"[opcja] tablica SUPLA_OPTION, których wyłączenie nie pozwala włączyć danej opcji",
        [JsonProperty("depOff")]
        public List<string>? DependenciesToDisable { get; set; }
        public int SectionOrder { get; internal set; }

        [JsonProperty("parameters")]
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}