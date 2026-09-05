using System.ComponentModel;

namespace GuiGenericBuilderDesktop
{
    public class RelayEntryViewModel : INotifyPropertyChanged
    {
        private bool _isEnabled;
        private int _gpio;
        private int _state;
        private bool _lightControl;
        private int _afterResetReaction;
        private int _ledGpio;
        private int _ledActivationState;
        private string _directLinksOn = string.Empty;
        private string _directLinksOff = string.Empty;
        private int _thermostatType;
        private int _thermostatMainTempChannel;
        private int _thermostatAdditionalTempChannel;
        private string _thermostatHisteresis = string.Empty;
        private string _thermostatMinTemp = string.Empty;
        private string _thermostatMaxTemp = string.Empty;

        public int Index { get; set; }

        public int ThermostatType
        {
            get => _thermostatType;
            set
            {
                if (_thermostatType != value)
                {
                    _thermostatType = value;
                    OnPropertyChanged(nameof(ThermostatType));
                }
            }
        }

        public int ThermostatMainTempChannel
        {
            get => _thermostatMainTempChannel;
            set
            {
                if (_thermostatMainTempChannel != value)
                {
                    _thermostatMainTempChannel = value;
                    OnPropertyChanged(nameof(ThermostatMainTempChannel));
                }
            }
        }

        public int ThermostatAdditionalTempChannel
        {
            get => _thermostatAdditionalTempChannel;
            set
            {
                if (_thermostatAdditionalTempChannel != value)
                {
                    _thermostatAdditionalTempChannel = value;
                    OnPropertyChanged(nameof(ThermostatAdditionalTempChannel));
                }
            }
        }

        public string ThermostatHisteresis
        {
            get => _thermostatHisteresis;
            set
            {
                if (_thermostatHisteresis != value)
                {
                    _thermostatHisteresis = value ?? string.Empty;
                    OnPropertyChanged(nameof(ThermostatHisteresis));
                }
            }
        }

        public string ThermostatMinTemp
        {
            get => _thermostatMinTemp;
            set
            {
                if (_thermostatMinTemp != value)
                {
                    _thermostatMinTemp = value ?? string.Empty;
                    OnPropertyChanged(nameof(ThermostatMinTemp));
                }
            }
        }

        public string ThermostatMaxTemp
        {
            get => _thermostatMaxTemp;
            set
            {
                if (_thermostatMaxTemp != value)
                {
                    _thermostatMaxTemp = value ?? string.Empty;
                    OnPropertyChanged(nameof(ThermostatMaxTemp));
                }
            }
        }

        public string DirectLinksOn
        {
            get => _directLinksOn;
            set
            {
                if (_directLinksOn != value)
                {
                    _directLinksOn = value ?? string.Empty;
                    OnPropertyChanged(nameof(DirectLinksOn));
                }
            }
        }

        public string DirectLinksOff
        {
            get => _directLinksOff;
            set
            {
                if (_directLinksOff != value)
                {
                    _directLinksOff = value ?? string.Empty;
                    OnPropertyChanged(nameof(DirectLinksOff));
                }
            }
        }

        public string IndexDisplay => (Index + 1).ToString();

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        public int Gpio
        {
            get => _gpio;
            set
            {
                if (_gpio != value)
                {
                    _gpio = value;
                    OnPropertyChanged(nameof(Gpio));
                }
            }
        }

        public int State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged(nameof(State));
                }
            }
        }

        public bool LightControl
        {
            get => _lightControl;
            set
            {
                if (_lightControl != value)
                {
                    _lightControl = value;
                    OnPropertyChanged(nameof(LightControl));
                    OnPropertyChanged(nameof(Light));
                }
            }
        }

        public int Light
        {
            get => _lightControl ? 1 : 0;
            set
            {
                var newValue = value != 0;
                if (_lightControl != newValue)
                {
                    _lightControl = newValue;
                    OnPropertyChanged(nameof(LightControl));
                    OnPropertyChanged(nameof(Light));
                }
            }
        }

        public int AfterResetReaction
        {
            get => _afterResetReaction;
            set
            {
                if (_afterResetReaction != value)
                {
                    _afterResetReaction = value;
                    OnPropertyChanged(nameof(AfterResetReaction));
                }
            }
        }

        public int LedGpio
        {
            get => _ledGpio;
            set
            {
                if (_ledGpio != value)
                {
                    _ledGpio = value;
                    OnPropertyChanged(nameof(LedGpio));
                }
            }
        }

        public int LedActivationState
        {
            get => _ledActivationState;
            set
            {
                if (_ledActivationState != value)
                {
                    _ledActivationState = value;
                    OnPropertyChanged(nameof(LedActivationState));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
