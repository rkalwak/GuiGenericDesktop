using CompilationLib;
using System.Diagnostics;
using System.Threading;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            List<BuildFlagItem> BuildFlagsGG = new List<BuildFlagItem>() {
                new BuildFlagItem { FlagName = "SUPLA_EXCLUDE_LITTLEFS_CONFIG" },
                new BuildFlagItem { FlagName = "SUPLA_ENABLE_GUI" },
                new BuildFlagItem { FlagName = "SUPLA_HDC1080" },
                new BuildFlagItem { FlagName = "SUPLA_AHTX0" },
                new BuildFlagItem { FlagName = "SUPLA_DEBUG_MODE" }
            /*
,"SUPLA_OTA"
,"SUPLA_MDNS"
,"SUPLA_ENABLE_GUI"
,"SUPLA_ENABLE_SSL"
,"SUPLA_RELAY"
,"SUPLA_CONDITIONS"
,"SUPLA_BUTTON"
,"SUPLA_ACTION_TRIGGER"
,"SUPLA_LIMIT_SWITCH"
,"SUPLA_ROLLERSHUTTER"
,"SUPLA_CONFIG"
,"SUPLA_LED"
,"SUPLA_DS18B20"
,"SUPLA_DHT11"
,"SUPLA_DHT22"
,"SUPLA_SI7021_SONOFF"
,"SUPLA_BME280"
,"SUPLA_BMP280"
,"SUPLA_SHT3x"
,"SUPLA_SHT_AUTODETECT"
,"SUPLA_SI7021"
,"SUPLA_OLED"
,"SUPLA_MCP23017"
,"SUPLA_PCF8575"
,"SUPLA_PCF8574"
,"SUPLA_VL53L0X"
,"SUPLA_HDC1080"
,"SUPLA_LCD_HD44780"
,"SUPLA_MS5611"
,"SUPLA_AHTX0"
,"SUPLA_SENSIRON_SPS30_KPOP"
,"SUPLA_MAX6675"
,"SUPLA_MAX31855"
,"SUPLA_CC1101"
,"SUPLA_HC_SR04"
,"SUPLA_IMPULSE_COUNTER"
,"SUPLA_HLW8012"
,"SUPLA_RGBW"
,"SUPLA_PUSHOVER"
,"SUPLA_DIRECT_LINKS"
,"SUPLA_PZEM_V_3"
,"SUPLA_CSE7766"
,"SUPLA_DEEP_SLEEP"
,"SUPLA_DIRECT_LINKS_SENSOR_THERMOMETR"
,"SUPLA_RF_BRIDGE"
,"SUPLA_ADE7953"
,"SUPLA_PMSX003_KPOP"
,"SUPLA_VINDRIKTNING_IKEA_KPOP"
,"SUPLA_NTC_10K"
,"SUPLA_MPX_5XXX"
            */

            };
            var ggRequest = new CompileRequest
            {
                BuildFlags = BuildFlagsGG,
                Platform = "GUI_Generic_ESP32",
                //Platform = "GUI_Generic_ESP32C3",
                ProjectName = "src.ino",
                ProjectPath = @"C:/repozytoria/platformio/GUI-Generic/src",
                ProjectDirectory = @"C:/repozytoria/platformio/GUI-Generic",
                LibrariesPath = @"C:/repozytoria/platformio/GUI-Generic/lib",
            };

            // var p = new PlatformioCliHandler();
            // var resulCompilation = p.Handle(ggRequest, CancellationToken.None).GetAwaiter().GetResult();
            /*
            List<string> BuildFlags = new List<string>() { "SUPLA_TEXT=222", "SUPLA_AHTX0", "SUPLA_NUMBER=33" };

            var request = new CompileRequest

            {
                BuildFlags = BuildFlags,
                Platform = "esp32:esp32:esp32",
                ProjectName = "testbuild.ino",
                ProjectPath = @"C:/repozytoria/platformio/testbuild",
                ProjectDirectory = @"C:/repozytoria/platformio/testbuild",
                LibrariesPath = null,
            };
             */
            CompileRequest requestToDo = ggRequest;
           
             //How it works
             //1. Detect device COM port
             //2. Detect device model
             //3. Backup device firmware
             //4. (Optional) Compile new firmware with specific build flags
             //5. (Optional) Deploy new firmware to device
             //6. (Optional) display serial log
             //

            Console.WriteLine("---Detecting arduino-cli---");
            var arduinoCliHandler = new ArduinoCliDetector();

            Console.WriteLine("---Detecting device!---");
            Stopwatch stopwatch = Stopwatch.StartNew();
            var esptoolWrapper = new EsptoolWrapper();
            var deviceDetector = new DeviceDetector(esptoolWrapper);
            var detectionResult = deviceDetector.DetectCOMPort();
            Console.WriteLine($"Detected device on: {detectionResult}");
            Debug.WriteLine($"Detected device on: {detectionResult}");
            var modelInfo = deviceDetector.DetectEspModelAsync(detectionResult).GetAwaiter().GetResult();
            Console.WriteLine($"Detected model: {modelInfo}");
            Debug.WriteLine($"Detected model: {modelInfo}");

            //Console.WriteLine("---Saving backup!---");
            //var backupInfo = esptoolWrapper.ReadFlush(detectionResult, modelInfo.ChipType, _projectPath+"/backup.bin").GetAwaiter().GetResult();

            Console.WriteLine("---Building new firmware!---");




            requestToDo.PortCom= detectionResult;
            ICompileHandler compiler = new PlatformioCliHandler();
            var result = compiler.Handle(requestToDo, CancellationToken.None).GetAwaiter().GetResult();
            Console.WriteLine(result);
            Debug.WriteLine(result);
            /*
            if (result.ExitCode == 0)
            {

                Console.WriteLine("---Deploying new firmware!---");
                var deployHandler = new DeployHandler(esptoolWrapper);
                deployHandler.Deploy(detectionResult, modelInfo.ChipType, $"{result.OutputDirectory}/{result.OutputFile}", CancellationToken.None).GetAwaiter().GetResult();
                stopwatch.Stop();
                Console.WriteLine($"Done in {stopwatch.Elapsed.TotalSeconds}s!");
            }
            */
           
        }

    }
}

