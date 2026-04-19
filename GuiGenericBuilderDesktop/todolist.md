# To do

- GPIOS in general  to trzeba si� b�dzie wzi�� za pozosta�e GPIO (opis). Teraz np. kompiluj� na C3 i w opcjach wyporu GPIO (np. dla przeka�nika) mam, �e GPIO-1 to TX, a domy�lnie dla tego uk�adu jest TX na GPIO21. 
Rozwi�zania s� dwa: albo do wszystkich uk�ad�w dajemy GPIO bez opis�w (TX, RX) albo szukamy rozpiski pinout i wstawiamy (C3 - RX20, TX21, C6 - RX17, TX16, S3- TX43, RX44, dla samego ESP32 - jest OK 1(TX) i 3(RX)).
- PZEM adresy jako parametry pod flaga SUPLA_PZEM_ADR
- default settings from cloud
- HC_SR04 https://github.com/SUPLA/supla-device/pull/122
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