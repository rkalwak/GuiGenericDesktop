using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SuplaTemplateBoard.Models
{
    /// <summary>
    /// Represents a complete board template configuration
    /// </summary>
    public class BoardTemplate
    {
        [JsonPropertyName("NAME")]
        public string Name { get; set; }

        [JsonPropertyName("GPIO")]
        public List<int> GPIO { get; set; }

        [JsonPropertyName("BTNADC")]
        public List<int>? AnalogButtons { get; set; }

        [JsonPropertyName("BTNACTION")]
        public List<int>? ButtonActions { get; set; }

        [JsonPropertyName("COND")]
        public List<List<object>>? Conditions { get; set; }

        [JsonPropertyName("FLASH")]
        public string? FlashSize { get; set; }

        [JsonPropertyName("MCP23017")]
        public List<List<object>>? MCP23017 { get; set; }

        [JsonPropertyName("PCF8574")]
        public List<List<object>>? PCF8574 { get; set; }

        [JsonPropertyName("PCF8575")]
        public List<List<object>>? PCF8575 { get; set; }
    }

    /// <summary>
    /// Condition configuration for automated actions
    /// </summary>
    public class Condition
    {
        public int ExecutiveType { get; set; }          // 0 = Relay, 1 = RGBW
        public int ExecutiveNumber { get; set; }        // Which relay/RGBW (0-based)
        public int SensorType { get; set; }             // Type of sensor (DS18B20, DHT, etc.)
        public int SensorNumber { get; set; }           // Which sensor (0-based)
        public int ConditionType { get; set; }          // Comparison type (less, greater, etc.)
        public string ValueOn { get; set; }             // Trigger value
        public string ValueOff { get; set; }            // Reset value

        public static Condition FromJsonArray(List<object> array)
        {
            return new Condition
            {
                ExecutiveType = Convert.ToInt32(array[0]),
                ExecutiveNumber = Convert.ToInt32(array[1]),
                SensorType = Convert.ToInt32(array[2]),
                SensorNumber = Convert.ToInt32(array[3]),
                ConditionType = Convert.ToInt32(array[4]),
                ValueOn = array[5]?.ToString() ?? "",
                ValueOff = array[6]?.ToString() ?? ""
            };
        }
    }

    /// <summary>
    /// GPIO function mapping
    /// </summary>
    public enum GpioFunction
    {
        None = 0,
        Users = 1,
        
        // I2C
        I2CSCL = 544,
        I2CSDA = 640,
        I2CSCL2 = 608,
        I2CSDA2 = 576,
        
        // Relays (Normal)
        Relay1 = 224,
        Relay2 = 225,
        Relay3 = 226,
        Relay4 = 227,
        Relay5 = 228,
        Relay6 = 229,
        Relay7 = 230,
        Relay8 = 231,
        
        // Relays (Inverted)
        Relay1i = 320,
        Relay2i = 321,
        Relay3i = 322,
        Relay4i = 323,
        Relay5i = 324,
        Relay6i = 325,
        Relay7i = 326,
        Relay8i = 327,
        
        // Switches (Bistable, Pull-up)
        Switch1 = 32,
        Switch2 = 33,
        Switch3 = 34,
        Switch4 = 35,
        Switch5 = 36,
        Switch6 = 37,
        Switch7 = 38,
        
        // Switches (Bistable, No Pull-up)
        Switch1n = 160,
        Switch2n = 161,
        Switch3n = 162,
        Switch4n = 163,
        Switch5n = 164,
        Switch6n = 165,
        Switch7n = 166,
        
        // Buttons (Monostable, Pull-up)
        Button1 = 192,
        Button2 = 193,
        Button3 = 194,
        Button4 = 195,
        
        // Buttons (Monostable, No Pull-up)
        Button1n = 3232,
        Button2n = 3233,
        Button3n = 3234,
        Button4n = 3235,
        
        // LEDs (Active Low)
        Led1 = 288,
        Led2 = 289,
        Led3 = 290,
        Led4 = 291,
        LedLink = 292,
        
        // LEDs (Active High/Inverted)
        Led1i = 352,
        Led2i = 353,
        Led3i = 354,
        Led4i = 355,
        LedLinki = 356,
        
        // PWM/RGBW (Normal)
        PWM1 = 416,
        PWM2 = 417,
        PWM3 = 418,
        PWM4 = 419,
        PWM5 = 420,
        
        // PWM/RGBW (Inverted)
        PWM1i = 448,
        PWM2i = 449,
        PWM3i = 450,
        PWM4i = 451,
        PWM5i = 452,
        
        // Power Monitoring
        HLW8012CF = 2656,
        BL0937CF = 2688,
        HLWBLCF1 = 2624,
        HLWBLSELi = 2720,
        
        // Sensors
        SI7021 = 1248,
        DS18x20 = 1216,
        CSE7766Rx = 2752,
        TemperatureAnalog = 4736,
        ADE7953_IRQ = 3104,
        
        // Binary Inputs
        Binary1 = 3264,
        Binary2 = 3265,
        Binary3 = 3266,
        Binary4 = 3267,
        
        // Ethernet (ESP32)
        EthPOWER = 5024,
        EthMDC = 5056,
        EthMDIO = 5088
    }

    /// <summary>
    /// Button action types
    /// </summary>
    public enum ButtonAction
    {
        TurnOn = 0,
        TurnOff = 1,
        Toggle = 2
    }

    /// <summary>
    /// Executive device types
    /// </summary>
    public enum ExecutiveType
    {
        Relay = 0,
        RGBW = 1
    }

    /// <summary>
    /// Sensor types
    /// </summary>
    public enum SensorType
    {
        None = 0,
        DS18B20 = 1,
        DHT11 = 2,
        DHT22 = 3,
        SI7021_SONOFF = 4,
        HC_SR04 = 5,
        BME280 = 6,
        SHT3x = 7,
        SI7021 = 8,
        MAX6675 = 9,
        NTC_10K = 10,
        BMP280 = 11,
        MPX_5XXX = 12,
        MPX_5XXX_PERCENT = 13,
        ANALOG_READING_MAP = 14,
        VL53L0X = 15,
        DIRECT_LINKS_SENSOR_THERMOMETR = 16,
        HDC1080 = 17,
        HLW8012 = 18,
        PZEM_V3 = 19,
        Binary = 20
    }

    /// <summary>
    /// Condition types
    /// </summary>
    public enum ConditionType
    {
        OnLess = 0,
        OnGreater = 1,
        OnLessHumidity = 2,
        OnGreaterHumidity = 3,
        OnLessVoltage = 4,
        OnLessCurrent = 5,
        OnLessPowerActive = 6,
        GPIO = 7
    }
}
