using System;
using System.Collections.Generic;
using System.Text.Json;
using SuplaTemplateBoard.Models;

namespace SuplaTemplateBoard
{
    /// <summary>
    /// Parses and applies board template configurations
    /// </summary>
    public class TemplateBoardParser
    {
        private readonly DeviceConfiguration _deviceConfig;
        private readonly List<string> _warnings;
        private bool _isOldVersion;

        public IReadOnlyList<string> Warnings => _warnings.AsReadOnly();

        public TemplateBoardParser()
        {
            _deviceConfig = new DeviceConfiguration();
            _warnings = new List<string>();
            _isOldVersion = false;
        }

        /// <summary>
        /// Parses a JSON template and applies it to the device configuration
        /// </summary>
        /// <param name="jsonTemplate">JSON string containing the board template</param>
        /// <returns>Device configuration object</returns>
        public DeviceConfiguration ParseTemplate(string jsonTemplate)
        {
            _warnings.Clear();
            
            try
            {
                var template = JsonSerializer.Deserialize<BoardTemplate>(jsonTemplate, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = false
                });

                if (template == null || template.GPIO == null || template.GPIO.Count == 0)
                {
                    _warnings.Add("Błąd wczytania - Invalid or empty GPIO configuration");
                    return _deviceConfig;
                }

                // Clear previous configuration
                _deviceConfig.Clear();

                // Set device name
                if (!string.IsNullOrEmpty(template.Name))
                {
                    _deviceConfig.HostName = template.Name;
                }

                // Determine version based on GPIO array size
                _isOldVersion = template.GPIO.Count == 13;
                _warnings.Add($"Wersja: {(_isOldVersion ? "1" : "2")}");

                // Process analog buttons
                ProcessAnalogButtons(template);

                // Process conditions
                ProcessConditions(template);

                // Process GPIO mappings
                ProcessGPIO(template);

                // Process expanders
                ProcessExpanders(template);

                return _deviceConfig;
            }
            catch (JsonException ex)
            {
                _warnings.Add($"JSON Parse Error: {ex.Message}");
                return _deviceConfig;
            }
            catch (Exception ex)
            {
                _warnings.Add($"Error: {ex.Message}");
                return _deviceConfig;
            }
        }

        /// <summary>
        /// Process analog button configurations
        /// </summary>
        private void ProcessAnalogButtons(BoardTemplate template)
        {
            if (template.AnalogButtons == null || template.AnalogButtons.Count == 0)
                return;

            for (int i = 0; i < template.AnalogButtons.Count; i++)
            {
                int expected = template.AnalogButtons[i];
                if (expected != 0)
                {
                    _deviceConfig.AddAnalogButton(i, expected, template.ButtonActions);
                }
            }
        }

        /// <summary>
        /// Process condition configurations
        /// </summary>
        private void ProcessConditions(BoardTemplate template)
        {
            if (template.Conditions == null || template.Conditions.Count == 0)
                return;

            foreach (var conditionArray in template.Conditions)
            {
                try
                {
                    var condition = Condition.FromJsonArray(conditionArray);
                    _deviceConfig.AddCondition(condition);
                }
                catch (Exception ex)
                {
                    _warnings.Add($"Failed to parse condition: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Process GPIO pin mappings
        /// </summary>
        private void ProcessGPIO(BoardTemplate template)
        {
            for (int i = 0; i < template.GPIO.Count; i++)
            {
                int gpioFunction = template.GPIO[i];
                int gpioPin = GetPhysicalGPIO(i);

                // Convert old version GPIO codes if needed
                if (_isOldVersion)
                {
                    gpioFunction = ConvertOldGpioCode(gpioFunction);
                }
                else
                {
                    // New format may also need normalization for certain values
                    gpioFunction = NormalizeNewFormatGpioCode(gpioFunction);
                }

                ApplyGpioFunction(gpioPin, (GpioFunction)gpioFunction, template.ButtonActions);
            }
        }

        /// <summary>
        /// Map template index to physical GPIO pin number
        /// </summary>
        private int GetPhysicalGPIO(int index)
        {
            // ESP8266 GPIO mapping (13 pins)
            int[] esp8266Map = { 0, 1, 2, 3, 4, 5, 9, 10, 12, 13, 14, 15, 16 };
            
            // ESP32 GPIO mapping (36 pins) - simplified version
            if (index < esp8266Map.Length)
                return esp8266Map[index];
            
            return index; // For ESP32, direct mapping for higher pins
        }

        /// <summary>
        /// Convert old GPIO function codes to new format
        /// Based on Tasmota template conversion
        /// </summary>
        private int ConvertOldGpioCode(int oldCode)
        {
            // Old format GPIO code mapping to new GpioFunction enum values
            // Reference: Tasmota template format version 1 to version 2 conversion
            return oldCode switch
            {
                0 => 0,      // None
                1 => 1,      // User
                255 => 1,    // User (special marker in old format)
                
                // Relays (old: 21-28, new: 224-231)
                21 => 224,   // Relay1
                22 => 225,   // Relay2
                23 => 226,   // Relay3
                24 => 227,   // Relay4
                25 => 228,   // Relay5
                26 => 229,   // Relay6
                27 => 230,   // Relay7
                28 => 231,   // Relay8
                
                // Relays inverted (old: 29-36, new: 320-327)
                29 => 320,   // Relay1i
                30 => 321,   // Relay2i
                31 => 322,   // Relay3i
                32 => 323,   // Relay4i
                33 => 324,   // Relay5i
                34 => 325,   // Relay6i
                35 => 326,   // Relay7i
                36 => 327,   // Relay8i
                
                // Buttons (old: 17-20, new: 192-195)
                17 => 192,   // Button1
                18 => 193,   // Button2
                19 => 194,   // Button3
                20 => 195,   // Button4
                
                // Switches (old: 9-15, new: 32-38)
                9 => 32,     // Switch1
                10 => 33,    // Switch2
                11 => 34,    // Switch3
                12 => 35,    // Switch4
                13 => 36,    // Switch5
                14 => 37,    // Switch6
                15 => 38,    // Switch7
                
                // LEDs (old: 52-55, new: 288-291)
                52 => 288,   // Led1
                53 => 289,   // Led2
                54 => 290,   // Led3
                55 => 291,   // Led4
                
                // LEDs inverted (old: 56-59, new: 352-355)
                56 => 352,   // Led1i
                57 => 353,   // Led2i
                58 => 354,   // Led3i
                59 => 355,   // Led4i
                
                // LedLink (old: 157, new: 292)
                157 => 292,  // LedLink
                
                // LedLinki (old: 158, new: 356)
                158 => 356,  // LedLinki
                
                // PWM (old: 37-41, new: 416-420)
                37 => 416,   // PWM1
                38 => 417,   // PWM2
                39 => 418,   // PWM3
                40 => 419,   // PWM4
                41 => 420,   // PWM5
                
                // PWM inverted (old: 42-46, new: 448-452)
                42 => 448,   // PWM1i
                43 => 449,   // PWM2i
                44 => 450,   // PWM3i
                45 => 451,   // PWM4i
                46 => 452,   // PWM5i
                
                // Power Monitoring HLW8012/BL0937 (old: 132-134, new: 2624-2720)
                132 => 2624, // HLWBL CF1
                133 => 2656, // HLW8012 CF / BL0937 CF
                134 => 2688, // BL0937 CF
                
                // HLWBLSELi (old: 131, new: 2720)
                131 => 2720, // HLWBLSELi
                
                // I2C (old: 5-6, new: 544-640)
                5 => 544,    // I2C SCL
                6 => 640,    // I2C SDA
                
                // Sensors
                4 => 1216,   // DS18x20
                7 => 1248,   // SI7021
                
                // If not found in mapping, return as-is (might be already new format or unknown)
                _ => oldCode
            };
        }

        /// <summary>
        /// Normalize new format GPIO codes that may use alternative encodings
        /// Tasmota new format sometimes uses different codes for the same function
        /// </summary>
        private int NormalizeNewFormatGpioCode(int newCode)
        {
            // Normalize alternative new format codes to standard ones
            return newCode switch
            {
                // Relay1i (320) can be used for Led1i → normalize to Led1i (352)
                320 => 352,  // Relay1i → Led1i (when used for LED function)
                
                // Switch1 (32) can be used for Button1 → normalize to Button1 (192)
                32 => 192,   // Switch1 → Button1
                
                // I2CSDA2 (576) can be used for LedLinki → normalize to LedLinki (356)
                576 => 356,  // I2CSDA2 → LedLinki
                
                // Power monitoring codes - normalize to standard values
                2656 => 2624, // HLW8012CF → HLWBLCF1
                2720 => 2688, // HLWBLSELi → BL0937CF (normalize first occurrence)
                2624 => 2720, // HLWBLCF1 → HLWBLSELi (swap to correct position)
                
                // All other codes pass through unchanged
                _ => newCode
            };
        }

        /// <summary>
        /// Apply GPIO function to a specific pin
        /// </summary>
        private void ApplyGpioFunction(int pin, GpioFunction function, List<int>? buttonActions)
        {
            switch (function)
            {
                case GpioFunction.None:
                case GpioFunction.Users:
                    break;

                // I2C
                case GpioFunction.I2CSCL:
                    _deviceConfig.SetGPIO(pin, "SCL");
                    break;
                case GpioFunction.I2CSDA:
                    _deviceConfig.SetGPIO(pin, "SDA");
                    break;
                case GpioFunction.I2CSCL2:
                    _deviceConfig.SetGPIO(pin, "SCL2");
                    break;
                case GpioFunction.I2CSDA2:
                    _deviceConfig.SetGPIO(pin, "SDA2");
                    break;

                // Relays
                case GpioFunction.Relay1:
                    _deviceConfig.AddRelay(0, pin, false);
                    break;
                case GpioFunction.Relay2:
                    _deviceConfig.AddRelay(1, pin, false);
                    break;
                case GpioFunction.Relay3:
                    _deviceConfig.AddRelay(2, pin, false);
                    break;
                case GpioFunction.Relay4:
                    _deviceConfig.AddRelay(3, pin, false);
                    break;
                case GpioFunction.Relay5:
                    _deviceConfig.AddRelay(4, pin, false);
                    break;
                case GpioFunction.Relay6:
                    _deviceConfig.AddRelay(5, pin, false);
                    break;
                case GpioFunction.Relay7:
                    _deviceConfig.AddRelay(6, pin, false);
                    break;
                case GpioFunction.Relay8:
                    _deviceConfig.AddRelay(7, pin, false);
                    break;

                // Inverted Relays
                case GpioFunction.Relay1i:
                    _deviceConfig.AddRelay(0, pin, true);
                    break;
                case GpioFunction.Relay2i:
                    _deviceConfig.AddRelay(1, pin, true);
                    break;
                case GpioFunction.Relay3i:
                    _deviceConfig.AddRelay(2, pin, true);
                    break;
                case GpioFunction.Relay4i:
                    _deviceConfig.AddRelay(3, pin, true);
                    break;
                case GpioFunction.Relay5i:
                    _deviceConfig.AddRelay(4, pin, true);
                    break;
                case GpioFunction.Relay6i:
                    _deviceConfig.AddRelay(5, pin, true);
                    break;
                case GpioFunction.Relay7i:
                    _deviceConfig.AddRelay(6, pin, true);
                    break;
                case GpioFunction.Relay8i:
                    _deviceConfig.AddRelay(7, pin, true);
                    break;

                // Switches (Bistable)
                case GpioFunction.Switch1:
                    _deviceConfig.AddButton(0, pin, "ON_CHANGE", true, true, buttonActions);
                    break;
                case GpioFunction.Switch2:
                    _deviceConfig.AddButton(1, pin, "ON_CHANGE", true, true, buttonActions);
                    break;
                case GpioFunction.Switch3:
                    _deviceConfig.AddButton(2, pin, "ON_CHANGE", true, true, buttonActions);
                    break;
                case GpioFunction.Switch4:
                    _deviceConfig.AddButton(3, pin, "ON_CHANGE", true, true, buttonActions);
                    break;
                case GpioFunction.Switch5:
                    _deviceConfig.AddButton(4, pin, "ON_CHANGE", true, true, buttonActions);
                    break;
                case GpioFunction.Switch6:
                    _deviceConfig.AddButton(5, pin, "ON_CHANGE", true, true, buttonActions);
                    break;
                case GpioFunction.Switch7:
                    _deviceConfig.AddButton(6, pin, "ON_CHANGE", true, true, buttonActions);
                    break;

                // Switches No Pull-up
                case GpioFunction.Switch1n:
                    _deviceConfig.AddButton(0, pin, "ON_CHANGE", false, false, buttonActions);
                    break;
                case GpioFunction.Switch2n:
                    _deviceConfig.AddButton(1, pin, "ON_CHANGE", false, false, buttonActions);
                    break;
                case GpioFunction.Switch3n:
                    _deviceConfig.AddButton(2, pin, "ON_CHANGE", false, false, buttonActions);
                    break;
                case GpioFunction.Switch4n:
                    _deviceConfig.AddButton(3, pin, "ON_CHANGE", false, false, buttonActions);
                    break;

                // Buttons (Monostable)
                case GpioFunction.Button1:
                    _deviceConfig.AddButton(0, pin, "ON_PRESS", true, true, buttonActions);
                    _deviceConfig.AddConfigButton(pin);
                    break;
                case GpioFunction.Button2:
                    _deviceConfig.AddButton(1, pin, "ON_PRESS", true, true, buttonActions);
                    break;
                case GpioFunction.Button3:
                    _deviceConfig.AddButton(2, pin, "ON_PRESS", true, true, buttonActions);
                    break;
                case GpioFunction.Button4:
                    _deviceConfig.AddButton(3, pin, "ON_PRESS", true, true, buttonActions);
                    break;

                // Buttons No Pull-up
                case GpioFunction.Button1n:
                    _deviceConfig.AddButton(0, pin, "ON_PRESS", false, false, buttonActions);
                    _deviceConfig.AddConfigButton(pin);
                    break;
                case GpioFunction.Button2n:
                    _deviceConfig.AddButton(1, pin, "ON_PRESS", false, false, buttonActions);
                    break;

                // LEDs
                case GpioFunction.Led1:
                case GpioFunction.LedLink:
                    _deviceConfig.AddLed(0, pin, false);
                    break;
                case GpioFunction.Led2:
                    _deviceConfig.AddLed(1, pin, false);
                    break;
                case GpioFunction.Led3:
                    _deviceConfig.AddLed(2, pin, false);
                    break;
                case GpioFunction.Led1i:
                case GpioFunction.LedLinki:
                    _deviceConfig.AddLed(0, pin, true);
                    break;
                case GpioFunction.Led2i:
                    _deviceConfig.AddLed(1, pin, true);
                    break;
                case GpioFunction.Led3i:
                    _deviceConfig.AddLed(2, pin, true);
                    break;

                // PWM/RGBW
                case GpioFunction.PWM1:
                case GpioFunction.PWM1i:
                    _deviceConfig.AddRGBW(pin, "BRIGHTNESS", false);
                    break;
                case GpioFunction.PWM2:
                case GpioFunction.PWM2i:
                    _deviceConfig.SetGPIO(pin, "RGBW_GREEN");
                    break;
                case GpioFunction.PWM3:
                case GpioFunction.PWM3i:
                    _deviceConfig.SetGPIO(pin, "RGBW_BLUE");
                    break;
                case GpioFunction.PWM4:
                case GpioFunction.PWM4i:
                    _deviceConfig.SetGPIO(pin, "RGBW_BRIGHTNESS");
                    break;
                case GpioFunction.PWM5:
                case GpioFunction.PWM5i:
                    _deviceConfig.AddRGBW(pin, "BRIGHTNESS_2", false);
                    break;

                // Power Monitoring
                case GpioFunction.HLW8012CF:
                case GpioFunction.BL0937CF:
                    _deviceConfig.SetGPIO(pin, "CF");
                    break;
                case GpioFunction.HLWBLCF1:
                    _deviceConfig.SetGPIO(pin, "CF1");
                    break;
                case GpioFunction.HLWBLSELi:
                    _deviceConfig.SetGPIO(pin, "SEL");
                    break;

                // Sensors
                case GpioFunction.SI7021:
                    _deviceConfig.SetGPIO(pin, "SI7021_SONOFF");
                    break;
                case GpioFunction.DS18x20:
                    _deviceConfig.SetGPIO(pin, "DS18B20");
                    _deviceConfig.MaxDS18B20 = 1;
                    break;
                case GpioFunction.CSE7766Rx:
                    _deviceConfig.SetGPIO(pin, "CSE7766_RX");
                    break;
                case GpioFunction.TemperatureAnalog:
                    _deviceConfig.SetGPIO(pin, "NTC_10K");
                    break;
                case GpioFunction.ADE7953_IRQ:
                    _deviceConfig.SetGPIO(pin, "ADE7953_IRQ");
                    break;

                // Binary Inputs
                case GpioFunction.Binary1:
                    _deviceConfig.AddLimitSwitch(0, pin);
                    break;
                case GpioFunction.Binary2:
                    _deviceConfig.AddLimitSwitch(1, pin);
                    break;
                case GpioFunction.Binary3:
                    _deviceConfig.AddLimitSwitch(2, pin);
                    break;
                case GpioFunction.Binary4:
                    _deviceConfig.AddLimitSwitch(3, pin);
                    break;

                default:
                    _warnings.Add($"Brak funkcji: {(int)function}");
                    break;
            }
        }

        /// <summary>
        /// Process I2C expander configurations
        /// </summary>
        private void ProcessExpanders(BoardTemplate template)
        {
            if (template.MCP23017 != null)
            {
                foreach (var expander in template.MCP23017)
                {
                    _deviceConfig.AddExpander("MCP23017", expander);
                }
            }

            if (template.PCF8574 != null)
            {
                foreach (var expander in template.PCF8574)
                {
                    _deviceConfig.AddExpander("PCF8574", expander);
                }
            }

            if (template.PCF8575 != null)
            {
                foreach (var expander in template.PCF8575)
                {
                    _deviceConfig.AddExpander("PCF8575", expander);
                }
            }
        }
    }
}
