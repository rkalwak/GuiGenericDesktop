using System.ComponentModel;

namespace GuiGenericBuilderDesktop
{
    public class LimitSwitchEntryViewModel : INotifyPropertyChanged
    {
        private bool _isEnabled;
        private int _gpio;
        private bool _pullUp;

        public int Index { get; set; }

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

        public bool PullUp
        {
            get => _pullUp;
            set
            {
                if (_pullUp != value)
                {
                    _pullUp = value;
                    OnPropertyChanged(nameof(PullUp));
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
