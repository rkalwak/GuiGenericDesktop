# GUI-Generic Builder - Przewodnik Pomocy

## Witamy w GUI-Generic Builder

Ta aplikacja pomaga kompilować i wgrywać oprogramowanie dla urządzeń Supla przy użyciu frameworka GUI-Generic.

---

## Pierwsze kroki

### 1. Aktualizacja repozytorium GUI-Generic

Przed kompilacją jakiegokolwiek oprogramowania musisz pobrać repozytorium GUI-Generic:

1. Kliknij przycisk **"1. Aktualizuj Gui-Generic"**
2. Poczekaj na zakończenie pobierania (sygnalizowane komunikatem)
3. Repozytorium zawiera wszystkie niezbędne pliki źródłowe do kompilacji

**Uwaga**: Ten krok jest wymagany tylko raz lub gdy chcesz zaktualizować do najnowszej wersji.

---

### 2. Wykrywanie urządzenia

Aby automatycznie wykryć podłączone urządzenie ESP:

1. Podłącz swoje urządzenie ESP32 przez USB
2. Kliknij przycisk **"2. Sprawdź urządzenie"**
3. Aplikacja:
   - Automatycznie wykryje port COM
   - Zidentyfikuje typ układu (ESP32, ESP32-C3, ESP32-C6, ESP32-S3)
   - Ustawi rozmiar pamięci flash
   - Skonfiguruje niekompatybilne flagi

**Obsługiwane urządzenia**:
- ESP32 (wszystkie warianty)
- ESP32-C3
- ESP32-C6
- ESP32-S2
- ESP32-S3

---

### 3. Konfiguracja flag kompilacji

Flagi kompilacji kontrolują, które funkcje są uwzględnione w Twoim oprogramowaniu:

#### Wybór flag
- Przeglądaj sekcje flag (zorganizowane według funkcjonalności)
- Zaznacz pola wyboru dla funkcji, które chcesz włączyć
- Kliknij przycisk **"Parametry..."**, aby skonfigurować parametry flag, jeśli istnieją

#### Pola wyboru sekcji
- Kliknij pole wyboru obok nazwy sekcji, aby włączyć/wyłączyć wszystkie flagi w tej sekcji
- Trzy stany:
  - ✓ Zaznaczone: Wszystkie flagi włączone
  - ☐ Odznaczone: Wszystkie flagi wyłączone
  - ☑ Nieokreślone: Niektóre flagi włączone

#### Zależności flag
Niektóre flagi mają zależności od innych flag:
- **Auto-włączanie**: Powiązane flagi są automatycznie włączane
- **Auto-wyłączanie**: Konfliktujące flagi są automatycznie wyłączane
- **Blokowanie**: Niektóre flagi wymagają wcześniejszego włączenia innych flag

#### Kompatybilność platformy
Nie wszystkie flagi działają na wszystkich platformach:
- Niekompatybilne flagi są automatycznie wyłączane po wybraniu płytki
- Walidacja przed kompilacją zapobiega nieprawidłowym konfiguracjom

---

### 4. Wybór platformy

Wybierz swoją platformę docelową z listy rozwijanej **Płytka**:

- **ESP32** (domyślnie) - pamięć flash 4MB, 8MB, 16MB
- **ESP32-C3** - pamięć flash 4MB, 8MB, 16MB
- **ESP32-C6** - pamięć flash 4MB, 8MB, 16MB
- **ESP32-S3** - pamięć flash 4MB, 8MB, 16MB, 32MB

**Wybór rozmiaru pamięci flash**:
- Wybierz spośród dostępnych rozmiarów dla Twojej platformy
- Większa pamięć flash = więcej miejsca na funkcje i aktualizacje OTA
- Automatycznie wykrywane przy użyciu "2. Sprawdź urządzenie"

---

### 5. Kompilacja

Po skonfigurowaniu flag:

1. Wybierz **Port COM** (jeśli wgrywasz na urządzenie)
2. Wybierz opcje kompilacji:
   - **Wgraj**: Wgraj oprogramowanie na urządzenie po kompilacji
   - **Kopia zapasowa**: Utwórz kopię zapasową aktualnego oprogramowania przed wgraniem
   - **Wymaż pamięć**: Wyczyść pamięć flash przed wgraniem (zalecane dla czystej instalacji)
3. Kliknij przycisk **"3. Kompiluj"**

#### Proces kompilacji
- Czas trwania jest wyświetlany podczas kompilacji
- Wskaźniki stanu:
  - ⏳ Kompilowanie oprogramowania... (czarny tekst)
  - ✓ Kompilacja zakończona pomyślnie! (zielony)
  - ✗ Kompilacja nieudana (czerwony)
- Kliknij **"3. Zatrzymaj kompilację"**, aby anulować w razie potrzeby

#### Po kompilacji
Po udanej kompilacji:
- Konfiguracja jest automatycznie zapisywana
- Plik binarny oprogramowania jest przechowywany w katalogu `configurations/`
- Kopia zapasowa jest zapisana w katalogu `backup` pod nazwą `Config_YYYYMMDD_HHMMSS.bin` (z tym samym znacznikiem czasu co konfiguracja)
- Okno wyników kompilacji pokazuje:
  - Zakodowaną konfigurajcę (do udostępniania)
  - Lokalizację pliku kopii zapasowej (jeśli utworzono)
  - Lokalizację skompilowanego oprogramowania
  - Przyciski do otwierania folderów i kopiowania ścieżek

---

## Zarządzanie konfiguracjami

### Zapisywanie konfiguracji

**Automatyczny zapis**:
- Po każdej udanej kompilacji
- Nazwa pliku: `Config_YYYYMMDD_HHMMSS.json`
- Zawiera plik binarny oprogramowania i połączony plik ZIP

**Ręczny zapis**:
1. Otwórz **"Zarządzaj konfiguracjami..."**
2. Przejdź do zakładki **"Zapisz bieżącą konfigurację"**
3. Kliknij przycisk **"Zapisz bieżącą konfigurację"**
4. Wprowadź niestandardową nazwę
5. Konfiguracja jest zapisywana ze wszystkimi włączonymi flagami

### Wczytywanie konfiguracji

1. Kliknij przycisk **"Zarządzaj konfiguracjami..."**
2. Wybierz konfigurację z listy
3. Kliknij **"Wczytaj"** lub kliknij dwukrotnie konfigurację
4. Wszystkie flagi i ustawienia są przywracane

### Zakodowana konfiguracja

Każda konfiguracja ma zakodowany ciąg zawierający wszystkie wybrane flagi:

**Funkcje**:
- Odwracalny: Może być zdekodowany z powrotem do oryginalnych flag
- Możliwy do udostępniania: Kopiuj i wklej, aby udostępnić konfiguracje
- Bezpieczny dla URL: Może być używany w adresach URL

**Jak używać**:
1. Skopiuj zakodowany ciąg z konfiguracji
2. Udostępnij innym
3. Odbiorca wkleja w zakładce **"Wczytaj z zakodowanego ciągu"**
4. Konfiguracja jest dekodowana i może być wczytana

---

## Przegląd funkcji

### Wspólne flagi kompilacji

#### Komponenty podstawowe
- **SUPLA_CONFIG**: Interfejs konfiguracji webowej (zwykle wymagany)
- **SUPLA_RELAY**: Obsługa sterowania przekaźnikami
- **SUPLA_BUTTON**: Obsługa przycisków fizycznych
- **SUPLA_LED**: Wskaźniki LED

#### Czujniki
- **SUPLA_DS18B20**: Czujniki temperatury (Dallas)
- **SUPLA_DHT11/DHT22**: Czujniki temperatury i wilgotności
- **SUPLA_BME280**: Czujnik temperatury, wilgotności i ciśnienia
- **SUPLA_BMP280**: Czujnik temperatury i ciśnienia
- **SUPLA_SHT**: Czujniki SHT3x/SHT4x
- **SUPLA_SI7021**: Czujnik temperatury i wilgotności

#### Monitorowanie mocy
- **SUPLA_HLW8012**: Pomiar mocy (Sonoff Pow)
- **SUPLA_CSE7766**: Pomiar mocy (Sonoff Pow R2)
- **SUPLA_ADE7953**: Dwukanałowy pomiar mocy (Shelly 2.5)
- **SUPLA_PZEM**: Miernik mocy PZEM-004T

#### Wyświetlacze
- **SUPLA_OLED**: Obsługa wyświetlaczy OLED (SSD1306, SH1106)
- **SUPLA_MAX7219**: Wyświetlacz matrycowy LED

#### Łączność
- **SUPLA_DIRECT_LINKS**: Łączenie urządzeń bez chmury
- **SUPLA_DEEP_SLEEP**: Tryb oszczędzania energii
- **SUPLA_MQTT**: Obsługa protokołu MQTT

#### Funkcje specjalne
- **SUPLA_RGBW**: Sterowanie oświetleniem RGB/RGBW
- **SUPLA_DIMMER**: Sterowanie ściemniaczem
- **SUPLA_ROLLERSHUTTER**: Sterowanie roletami
- **SUPLA_IMPULSE_COUNTER**: Liczenie impulsów (liczniki energii, wody, gazu)

---

## Rozwiązywanie problemów

### Błędy kompilacji

**Problem**: Kompilacja kończy się niepowodzeniem z błędami
**Rozwiązanie**:
1. Sprawdź dziennik błędów w panelu wyjściowym
2. Upewnij się, że wszystkie wymagane flagi są włączone
3. Sprawdź kompatybilność flag z wybraną platformą
4. Zaktualizuj repozytorium GUI-Generic
5. Upewnij się, że PlatformIO Core jest poprawnie zainstalowane

### Problemy z wykrywaniem urządzenia

**Problem**: Urządzenie nie zostało wykryte
**Rozwiązanie**:
1. Sprawdź połączenie USB
2. Zainstaluj sterowniki USB (CP210x lub CH340)
3. Spróbuj innego kabla USB
4. Zamknij inne programy korzystające z portu szeregowego
5. Ręcznie wybierz port COM z listy rozwijanej

### Błędy wgrywania

**Problem**: Wgrywanie kończy się niepowodzeniem
**Rozwiązanie**:
1. Upewnij się, że wybrany jest prawidłowy port COM
2. Przytrzymaj przycisk BOOT podczas wgrywania (na niektórych płytkach)
3. Spróbuj zmniejszyć prędkość transmisji
4. Włącz opcję "Wyczyść pamięć"
5. Sprawdź połączenie USB

### Problemy z pamięcią

**Problem**: Oprogramowanie jest za duże dla wybranego rozmiaru pamięci flash
**Rozwiązanie**:
1. Wyłącz niektóre nieużywane funkcje
2. Wybierz płytkę z większą pamięcią flash
3. Zmniejsz poziom debugowania
4. Usuń nieużywane czujniki lub moduły

---

## Zaawansowane funkcje

### Kopia zapasowa i przywracanie

Zawsze twórz kopię zapasową swojego działającego oprogramowania:

1. Włącz opcję "Kopia zapasowa" przed wgrywaniem
2. Pliki kopii zapasowych są przechowywane w katalogu konfiguracji
3. Aby przywrócić:
   - Użyj narzędzia esptool.py
   - Lub wgraj zapisaną konfigurację ponownie

### Tryb offline

Aplikacja może działać bez połączenia z internetem:
- Repozytorium GUI-Generic jest przechowywane lokalnie
- Kompilacje są wykonywane lokalnie
- Wymagane tylko dla wstępnego pobierania/aktualizacji

### Udostępnianie konfiguracji

Udostępniaj swoje konfiguracje:
1. Eksportuj zakodowany ciąg
2. Udostępnij przez e-mail, forum lub media społecznościowe
3. Inni użytkownicy mogą zaimportować Twoją konfigurację
4. Doskonałe do udostępniania działających konfiguracji

---

## Skróty klawiszowe

- **F1**: Otwórz tę pomoc
- **Ctrl+S**: Zapisz bieżącą konfigurację
- **Ctrl+O**: Otwórz menedżer konfiguracji
- **Ctrl+B**: Rozpocznij kompilację
- **Esc**: Zamknij aktualne okno dialogowe

---

## Dodatkowe zasoby

### Linki

- **Dokumentacja GUI-Generic**: [github.com/rkalwak/GUI-Generic](https://github.com/rkalwak/GUI-Generic)
- **Forum SUPLA**: [forum.supla.org](https://forum.supla.org)
- **Dokumentacja PlatformIO**: [docs.platformio.org](https://docs.platformio.org)

### Wsparcie

Jeśli napotkasz problemy:
1. Sprawdź tę pomoc pod kątem rozwiązań
2. Wyszukaj na forum SUPLA
3. Zgłoś błędy na GitHub
4. Skontaktuj się ze społecznością

**Logi aplikacji**:
- Logi aplikacji są przechowywane w katalogu `logs/`
- Dołącz pliki logów do postów na forum, jeśli zostaniesz o to poproszony przez dewelopera
- Logi zawierają szczegółowe informacje o procesie kompilacji i błędach

---

## O aplikacji

**GUI-Generic Builder** został stworzony, aby uprościć proces kompilacji oprogramowania SUPLA.

**Funkcje**:
- ✓ Automatyczne wykrywanie urządzeń
- ✓ Wizualne zarządzanie flagami kompilacji
- ✓ Weryfikacja zależności
- ✓ Sprawdzanie kompatybilności platformy
- ✓ Zarządzanie konfiguracjami
- ✓ Kopie zapasowe i przywracanie
- ✓ Udostępnianie konfiguracji
- ✓ Obsługa wielu języków

**Licencja**: MIT

**Wersja**: 2.0

---

*Ostatnia aktualizacja: 2026*
