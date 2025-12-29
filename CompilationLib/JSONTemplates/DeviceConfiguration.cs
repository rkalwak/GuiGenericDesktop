using System;
using System.Collections.Generic;
using System.Linq;
using SuplaTemplateBoard.Models;

namespace SuplaTemplateBoard
{
    /// <summary>
    /// Represents the complete device configuration after parsing a template
    /// </summary>
    public class DeviceConfiguration
    {
        public string HostName { get; set; }
        public string FlashSize { get; set; }
        public int MaxRelays { get; private set; }
        public int MaxButtons { get; private set; }
        public int MaxLeds { get; private set; }
        public int MaxRGBW { get; private set; }
        public int MaxLimitSwitches { get; private set; }
        public int MaxDS18B20 { get; set; }
        public int MaxConditions { get; private set; }
        public string ConfigMode { get; set; }

        // GPIO assignments
        private Dictionary<int, string> _gpioFunctions;
        
        // Device collections
        public List<RelayConfig> Relays { get; private set; }
        public List<ButtonConfig> Buttons { get; private set; }
        public List<LedConfig> Leds { get; private set; }
        public List<RGBWConfig> RGBWs { get; private set; }
        public List<LimitSwitchConfig> LimitSwitches { get; private set; }
        public List<Condition> Conditions { get; private set; }
        public List<AnalogButtonConfig> AnalogButtons { get; private set; }
        public List<ExpanderConfig> Expanders { get; private set; }

        public DeviceConfiguration()
        {
            _gpioFunctions = new Dictionary<int, string>();
            Relays = new List<RelayConfig>();
            Buttons = new List<ButtonConfig>();
            Leds = new List<LedConfig>();
            RGBWs = new List<RGBWConfig>();
            LimitSwitches = new List<LimitSwitchConfig>();
            Conditions = new List<Condition>();
            AnalogButtons = new List<AnalogButtonConfig>();
            Expanders = new List<ExpanderConfig>();
            
            ConfigMode = "CONFIG_MODE_10_ON_PRESSES";
        }

        public void Clear()
        {
            _gpioFunctions.Clear();
            Relays.Clear();
            Buttons.Clear();
            Leds.Clear();
            RGBWs.Clear();
            LimitSwitches.Clear();
            Conditions.Clear();
            AnalogButtons.Clear();
            Expanders.Clear();
            
            MaxRelays = 0;
            MaxButtons = 0;
            MaxLeds = 0;
            MaxRGBW = 0;
            MaxLimitSwitches = 0;
            MaxConditions = 0;
            MaxDS18B20 = 0;
        }

        public void SetGPIO(int pin, string function)
        {
            _gpioFunctions[pin] = function;
        }

        public string GetGPIO(int pin)
        {
            return _gpioFunctions.ContainsKey(pin) ? _gpioFunctions[pin] : "OFF";
        }

        public void AddRelay(int number, int gpio, bool inverted)
        {
            Relays.Add(new RelayConfig
            {
                Number = number,
                GPIO = gpio,
                Inverted = inverted
            });
            MaxRelays = Math.Max(MaxRelays, number + 1);
        }

        public void AddButton(int number, int gpio, string eventType, bool pullup, bool inverted, List<int>? actions)
        {
            var buttonAction = ButtonAction.Toggle;
            if (actions != null && number < actions.Count)
            {
                buttonAction = (ButtonAction)actions[number];
            }

            Buttons.Add(new ButtonConfig
            {
                Number = number,
                GPIO = gpio,
                EventType = eventType,
                Pullup = pullup,
                Inverted = inverted,
                Action = buttonAction
            });
            MaxButtons = Math.Max(MaxButtons, number + 1);
        }

        public void AddConfigButton(int gpio)
        {
            // Special config button handling
            SetGPIO(gpio, "BUTTON_CFG");
        }

        public void AddAnalogButton(int number, int expectedValue, List<int>? actions)
        {
            var buttonAction = ButtonAction.Toggle;
            if (actions != null && number < actions.Count)
            {
                buttonAction = (ButtonAction)actions[number];
            }

            AnalogButtons.Add(new AnalogButtonConfig
            {
                Number = number,
                ExpectedValue = expectedValue,
                Action = buttonAction
            });
        }

        public void AddLed(int number, int gpio, bool inverted)
        {
            Leds.Add(new LedConfig
            {
                Number = number,
                GPIO = gpio,
                Inverted = inverted
            });
            MaxLeds = Math.Max(MaxLeds, number + 1);
        }

        public void AddRGBW(int gpio, string channel, bool inverted)
        {
            RGBWs.Add(new RGBWConfig
            {
                GPIO = gpio,
                Channel = channel,
                Inverted = inverted
            });
            MaxRGBW++;
        }

        public void AddLimitSwitch(int number, int gpio)
        {
            LimitSwitches.Add(new LimitSwitchConfig
            {
                Number = number,
                GPIO = gpio
            });
            MaxLimitSwitches = Math.Max(MaxLimitSwitches, number + 1);
        }

        public void AddCondition(Condition condition)
        {
            Conditions.Add(condition);
            MaxConditions++;
        }

        public void AddExpander(string type, List<object> config)
        {
            Expanders.Add(new ExpanderConfig
            {
                Type = type,
                Configuration = config
            });
        }

        public Dictionary<string, object> ToConfigDictionary()
        {
            var config = new Dictionary<string, object>
            {
                ["HostName"] = HostName ?? "SUPLA-Device",
                ["MaxRelays"] = MaxRelays,
                ["MaxButtons"] = MaxButtons,
                ["MaxLeds"] = MaxLeds,
                ["MaxRGBW"] = MaxRGBW,
                ["MaxLimitSwitches"] = MaxLimitSwitches,
                ["MaxConditions"] = MaxConditions,
                ["ConfigMode"] = ConfigMode,
                ["Relays"] = Relays,
                ["Buttons"] = Buttons,
                ["Leds"] = Leds,
                ["RGBWs"] = RGBWs,
                ["LimitSwitches"] = LimitSwitches,
                ["Conditions"] = Conditions,
                ["AnalogButtons"] = AnalogButtons,
                ["Expanders"] = Expanders,
                ["GPIOFunctions"] = _gpioFunctions
            };

            if (!string.IsNullOrEmpty(FlashSize))
            {
                config["FlashSize"] = FlashSize;
            }

            if (MaxDS18B20 > 0)
            {
                config["MaxDS18B20"] = MaxDS18B20;
            }

            return config;
        }
    }

    public class RelayConfig
    {
        public int Number { get; set; }
        public int GPIO { get; set; }
        public bool Inverted { get; set; }
    }

    public class ButtonConfig
    {
        public int Number { get; set; }
        public int GPIO { get; set; }
        public string EventType { get; set; }
        public bool Pullup { get; set; }
        public bool Inverted { get; set; }
        public ButtonAction Action { get; set; }
    }

    public class AnalogButtonConfig
    {
        public int Number { get; set; }
        public int ExpectedValue { get; set; }
        public ButtonAction Action { get; set; }
    }

    public class LedConfig
    {
        public int Number { get; set; }
        public int GPIO { get; set; }
        public bool Inverted { get; set; }
    }

    public class RGBWConfig
    {
        public int GPIO { get; set; }
        public string Channel { get; set; }
        public bool Inverted { get; set; }
    }

    public class LimitSwitchConfig
    {
        public int Number { get; set; }
        public int GPIO { get; set; }
    }

    public class ExpanderConfig
    {
        public string Type { get; set; }
        public List<object> Configuration { get; set; }
    }
}
