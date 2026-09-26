# To do

- PZEM adresy jako parametry pod flaga SUPLA_PZEM_ADR
- default settings from cloud
- sound when compilation is done
- Niestety ca�kowicie wirtualny termostat (oparty na linkach bezpo�rednich) nie dzia�a. To znaczy dzia�a odczyt temperatury, ale je�li dodamy linki bezpo�rednie do przeka�nika (w��cznika) to modu� odmawia wsp�pracy. 
Zawiesza si�, nie loguje do cloud i trzeba go przeflashowa� na nowo, bo nawet w tryb config wej�� nie chce. Krystian nie da� z tym rady, ale mia�em nadziej�, �e si� "cudownie" naprawi�o. Niestety nie ;-)
- json settings
- building Zigbee Gateway?
- Modbus control?
	- New build flag: SUPLA_MODBUS
	- New configuration window for Modbus settings
	- New class for holding Modbus settings, considering input registers, coils, holding registers and discrete inputs
		- there must be method to serialized settings that can be consumed on ESP32 side
- Fix issues:
	
	- https://forum.supla.org/viewtopic.php?t=17742
	- I2c sensors as kpop
	- https://tasmota.github.io/docs/Components/
	- https://forum.supla.org/viewtopic.php?p=200757#p200757
	- https://forum.supla.org/viewtopic.php?p=201170#p201170
	- https://forum.supla.org/viewtopic.php?p=194354#p194354
	- https://forum.supla.org/viewtopic.php?t=16885

	Zigbee
1. backup as option checkbox before flashing - to avoid bricking device if something goes wrong, default checked
2. something is wrong if you apply newer version - it doesnt boot
3. create integration test for this to ensure correct image size
4. restore from backup button
5. load available firmware as explicit button to avoid API limits
6. cache in file available firmware list to avoid API limits, refresh on demand or on open of application


** SUPLA_RF_BRIDGE having parameters:
** SUPLA_MCP23017 or SUPLA_PCF8575 or SUPLA_PCF8574 having parameters:

compilacja z ARduino ide , opcje do wyboru w GUI:
* Arduino IDE
* PlatformIO 


For testing purposes create new Arduino project with minimal code to test if it compiles and uploads correctly. This will help isolate any issues related to the build environment or specific configurations.
There should be defines for each type of flag, meaning string or numbers.
Create integration test that uses this project for validation.
Test should compile, deploy and then read from serial output indicators that passed values for the flag worked fine

& 'C:\repozytoria\net\GuiGenericV2\CompilationLib.Tests\TestFixtures\ArduinoFlagValidation\arduino-cli.exe' --% compile --clean -b esp32:esp32:esp32 --build-property "build.extra_flags=-D FLAG_STRING=1 "-DFLAG_STRING_TEXT="alpha beta"" -D FLAG_NUMERIC_COUNT=7 -D FLAG_NUMERIC_GPIO=12" "C:\repozytoria\net\GuiGenericV2\CompilationLib.Tests\TestFixtures\ArduinoFlagValidation"