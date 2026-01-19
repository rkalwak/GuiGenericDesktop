# To do

- GPIOS in general  to trzeba siê bêdzie wzi¹æ za pozosta³e GPIO (opis). Teraz np. kompilujê na C3 i w opcjach wyporu GPIO (np. dla przekaŸnika) mam, ¿e GPIO-1 to TX, a domyœlnie dla tego uk³adu jest TX na GPIO21. 
Rozwi¹zania s¹ dwa: albo do wszystkich uk³adów dajemy GPIO bez opisów (TX, RX) albo szukamy rozpiski pinout i wstawiamy (C3 - RX20, TX21, C6 - RX17, TX16, S3- TX43, RX44, dla samego ESP32 - jest OK 1(TX) i 3(RX)).
- zaluzje rs->addTiltFunctions();
- PZEM adresy jako parametry pod flaga SUPLA_PZEM_ADR
- default settings from cloud
- HC_SR04 https://github.com/SUPLA/supla-device/pull/122
- sound when compilation is done
- Niestety ca³kowicie wirtualny termostat (oparty na linkach bezpoœrednich) nie dzia³a. To znaczy dzia³a odczyt temperatury, ale jeœli dodamy linki bezpoœrednie do przekaŸnika (w³¹cznika) to modu³ odmawia wspó³pracy. 
Zawiesza siê, nie loguje do cloud i trzeba go przeflashowaæ na nowo, bo nawet w tryb config wejœæ nie chce. Krystian nie da³ z tym rady, ale mia³em nadziejê, ¿e siê "cudownie" naprawi³o. Niestety nie ;-)
- json settings
- building Zigbee Gateway?
- CC1101 given version downloading
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