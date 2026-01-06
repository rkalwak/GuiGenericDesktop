using System.Collections.Generic;

namespace GuiGenericBuilderDesktop.Localization
{
    /// <summary>
    /// Polish (Polski) translations - Default language
    /// </summary>
    internal static class PolishTranslations
    {
        public static Dictionary<string, string> GetTranslations()
        {
            return new Dictionary<string, string>
            {
                // Main Window
                ["AppTitle"] = "GUI-Generic Builder",
                ["Board"] = "Płytka:",
                ["Port"] = "Port:",
                ["Flash"] = "Pamięć:",
                ["None"] = "Brak",
                ["ManageConfigs"] = "Zarządzaj Konfiguracjami...",
                ["UpdateGuiGeneric"] = "1. Aktualizuj Gui-Generic",
                ["CheckDevice"] = "2. Sprawdź Urządzenie",
                ["Compile"] = "3. Kompiluj",
                ["StopCompilation"] = "3. Zatrzymaj kompilację",
                ["Deploy"] = "Wgraj",
                ["Backup"] = "Kopia zapasowa",
                ["EraseFlash"] = "Wymaż pamięć",
                ["EraseFlashTooltip"] = "Wymaż pamięć flash przed wgraniem firmware (zalecane dla czystej instalacji)",
                ["BackupTooltip"] = "Utwórz kopię zapasową przed wgraniem firmware",
                
                // Column Headers
                ["Enabled"] = "Włączone",
                ["Key"] = "Klucz",
                ["Name"] = "Nazwa",
                ["Description"] = "Opis",
                ["Parameters"] = "Parametry",
                ["ParamsButton"] = "Param...",
                
                // Status Messages
                ["DownloadingRepository"] = "⏳ Pobieranie repozytorium GUI-Generic...",
                ["RepositoryUpdatedSuccess"] = "✓ Repozytorium zaktualizowane pomyślnie!",
                ["RepositoryUpdateFailed"] = "✗ Aktualizacja repozytorium nie powiodła się",
                ["DetectingDevice"] = "⏳ Wykrywanie urządzenia...",
                ["DeviceDetected"] = "✓ Wykryto urządzenie: {0} na {1}",
                ["NoDeviceDetected"] = "✗ Nie wykryto urządzenia",
                ["DeviceDetectionError"] = "✗ Błąd wykrywania urządzenia",
                ["CompilingFirmware"] = "⏳ Kompilowanie firmware... Upłynęło: {0:F1}s",
                ["CompilationSuccessful"] = "✓ Kompilacja zakończona sukcesem! Czas: {0:F1}s",
                ["CompilationFailed"] = "✗ Kompilacja nie powiodła się! Czas: {0:F1}s",
                ["CompilationError"] = "✗ Błąd kompilacji! Czas: {0:F1}s",
                ["StoppingCompilation"] = "⏳ Zatrzymywanie kompilacji...",
                ["CompilationStopped"] = "⏹ Kompilacja zatrzymana. Czas: {0:F1}s",
                
                // Message Box Titles
                ["UpdateComplete"] = "Aktualizacja Zakończona",
                ["UpdateFailed"] = "Aktualizacja Nie Powiodła Się",
                ["DeviceNotFound"] = "Nie Znaleziono Urządzenia",
                ["DetectionError"] = "Błąd Wykrywania",
                ["Error"] = "Błąd",
                ["Warning"] = "Ostrzeżenie",
                ["Information"] = "Informacja",
                ["Success"] = "Sukces",
                
                // Message Box Messages - Main Window
                ["RepositoryUpdatedMessage"] = "Repozytorium zaktualizowane pomyślnie!",
                ["ErrorUpdatingRepository"] = "Błąd aktualizacji repozytorium: {0}",
                ["NoDeviceDetectedMessage"] = @"Nie wykryto urządzenia ESP.

Upewnij się, że:
• Urządzenie jest podłączone przez USB
• Sterowniki USB są zainstalowane
• Urządzenie jest włączone",
                
                ["DeviceDetectionErrorMessage"] = @"Błąd wykrywania urządzenia: {0}

Sprawdź logi, aby uzyskać więcej szczegółów.",
                
                ["RepositoryNotFound"] = @"Nie znaleziono repozytorium GUI-Generic!

Najpierw kliknij przycisk '1. Aktualizuj Gui-Generic', aby pobrać repozytorium.

Repozytorium jest wymagane do kompilacji firmware.",
                
                ["RepositoryNotFoundTitle"] = "Nie Znaleziono Repozytorium",
                
                ["EmptyRepository"] = @"Katalog repozytorium GUI-Generic jest pusty!

Ścieżka repozytorium: {0}

Kliknij przycisk '1. Aktualizuj Gui-Generic', aby pobrać repozytorium.",
                
                ["EmptyRepositoryTitle"] = "Puste Repozytorium",
                
                ["IncompleteRepository"] = @"Repozytorium GUI-Generic wydaje się być niekompletne lub uszkodzone.

Brakujące plik: platformio.ini
Ścieżka repozytorium: {0}

Kliknij przycisk '1. Aktualizuj Gui-Generic', aby ponownie pobrać repozytorium.",
                
                ["IncompleteRepositoryTitle"] = "Niekompletne Repozytorium",
                ["NoFlagsSelected"] = "Nie wybrano flag. Włącz niektóre flagi przed kompilacją.",
                ["NoFlags"] = "Brak Flag",
                
                ["PlatformRequired"] = @"Wybierz platformę docelową (płytkę) przed kompilacją.

Użyj '2. Sprawdź Urządzenie' do automatycznego wykrycia lub ręcznie wybierz platformę z listy rozwijanej Płytka.

Dostępne platformy:
• ESP32 (domyślna)
• ESP32-C3
• ESP32-C6
• ESP32-S3",
                
                ["PlatformRequiredTitle"] = "Wymagana Platforma",
                
                ["PlatformCompatibilityError"] = @"Następujące flagi nie są kompatybilne z wybraną platformą ({0}):

{1}

Wyłącz te flagi lub wybierz inną platformę przed kompilacją.",
                
                ["PlatformCompatibilityErrorTitle"] = "Błąd Kompatybilności Platformy",
                ["I2CConfigurationError"] = "{0}",
                ["I2CConfigurationErrorTitle"] = "Błąd Konfiguracji I2C",
                
                ["COMPortRequired"] = @"Wybierz port COM przed kompilacją z wgrywaniem.

Firmware musi zostać przesłany do urządzenia podłączonego przez port COM.
Użyj '2. Sprawdź Urządzenie' do automatycznego wykrycia lub ręcznie wybierz port COM.

Alternatywnie odznacz 'Wgraj', aby tylko skompilować bez wgrywania.",
                
                ["COMPortRequiredTitle"] = "Wymagany Port COM",
                
                ["CompilationStoppedMessage"] = @"Kompilacja zatrzymana przez użytkownika.

Upłynął czas: {0:F1}s",
                
                ["CompilationStoppedTitle"] = "Kompilacja Zatrzymana",
                ["CompilationErrorMessage"] = "Błąd kompilacji: {0}",
                ["ErrorTitle"] = "Błąd",
                
                ["PlatformCompatibility"] = @"Następujące flagi nie są kompatybilne z wybraną platformą ({0}) i zostały wyłączone:

{1}",
                
                ["PlatformCompatibilityTitle"] = "Kompatybilność Platformy",
                
                ["PlatformIncompatibility"] = @"Flaga '{0}' nie jest kompatybilna z wybraną platformą ({1}).

Ta flaga jest wyłączona na: {2}",
                
                ["PlatformIncompatibilityTitle"] = "Niekompatybilność Platformy",
                ["BlockingDependencies"] = "Blokujące Zależności",
                ["NoParameters"] = "Wybrana flaga nie ma parametrów.",
                ["NoParametersTitle"] = "Brak Parametrów",
                ["NoSelection"] = "Nie wybrano flagi. Wybierz flagę w siatce i spróbuj ponownie.",
                ["NoSelectionTitle"] = "Brak Wyboru",
                ["ThisFlagHasNoParameters"] = "Ta flaga nie ma parametrów.",
                
                ["ConfigurationLoaded"] = @"Konfiguracja '{0}' załadowana pomyślnie!

Platforma: {1}
Port COM: {2}
Włączone flagi: {3}",
                
                ["ConfigurationLoadedTitle"] = "Konfiguracja Załadowana",
                
                ["ManualConfigurationRequired"] = @"Konfiguracja '{0}' nie ma zapisanych flag.

Wybierz flagi kompilacji ręcznie.",
                
                ["ManualConfigurationRequiredTitle"] = "Wymagana Ręczna Konfiguracja",
                ["ErrorOpeningConfigManager"] = "Błąd otwierania menedżera konfiguracji: {0}",
                ["FileNotFound"] = "Nie Znaleziono Pliku",
                ["BuilderJsonNotFound"] = "Nie znaleziono builder.json w: {0}",
                ["InvalidSections"] = "builder.json nie zawiera prawidłowych Sekcji",
                ["ErrorLoadingBuilderJson"] = "Błąd ładowania builder.json: {0}",
                
                // Language
                ["Language"] = "Język:",
                ["LanguageTooltip"] = "Wybierz język aplikacji",
                ["LanguageChanged"] = "Język zmieniony na: {0}",
                
                // Compilation Results Window
                ["CompilationSuccessTitle"] = "Kompilacja Zakończona Sukcesem",
                ["CompilationFailedTitle"] = "Kompilacja Nie Powiodła Się",
                ["BuildConfiguration"] = "Konfiguracja Kompilacji:",
                ["CopyConfig"] = "Kopiuj Konfigurację",
                ["BackupFile"] = "Plik Kopii Zapasowej:",
                ["FirmwareFile"] = "Plik Firmware:",
                ["BuildOutput"] = "Wynik Kompilacji:",
                ["ErrorLogs"] = "Logi Błędów:",
                ["CopyOutput"] = "Kopiuj Wynik",
                ["CopyLogs"] = "Kopiuj Logi",
                ["SaveLogs"] = "Zapisz Logi",
                ["CopyPath"] = "Kopiuj Ścieżkę",
                ["OpenFolder"] = "Otwórz Folder",
                ["Close"] = "Zamknij",
                ["ConfigCopied"] = "Ciąg konfiguracji skopiowany do schowka pomyślnie!",
                ["CopySuccess"] = "Skopiowano Pomyślnie",
                ["CopyError"] = "Błąd Kopiowania",
                ["FailedToCopy"] = "Nie udało się skopiować do schowka: {0}",
                ["LogsSaved"] = "Zawartość zapisana pomyślnie do:\n{0}",
                ["SaveSuccess"] = "Zapisano Pomyślnie",
                ["SaveError"] = "Błąd Zapisywania",
                ["FailedToSave"] = "Nie udało się zapisać: {0}",
                ["PathCopied"] = "Ścieżka do pliku skopiowana do schowka!\n\n{0}",
                ["FileNotFoundError"] = "Nie znaleziono pliku.",
                ["DirectoryNotFound"] = "Nie znaleziono katalogu:\n{0}",
                ["OpenFolderError"] = "Nie udało się otworzyć folderu: {0}",
                ["NoConfigurationAvailable"] = "Brak dostępnej konfiguracji.",
                ["NoLogsAvailable"] = "Brak dostępnych logów.",
                ["OutputCopied"] = "Wynik skopiowany do schowka pomyślnie!",
                ["LogsCopied"] = "Logi skopiowane do schowka pomyślnie!",
                ["FailedToCopyConfig"] = "Nie udało się skopiować ciągu konfiguracji do schowka: {0}",
                ["LogFilesFilter"] = "Pliki logów (*.log)|*.log|Pliki tekstowe (*.txt)|*.txt|Wszystkie pliki (*.*)|*.*",
                ["SaveCompilationOutput"] = "Zapisz Wynik Kompilacji",
                ["SaveCompilationLogs"] = "Zapisz Logi Kompilacji",
                ["BuildConfigurationString"] = "Ciąg Konfiguracji Kompilacji",
                ["BackupPathCopied"] = "Ścieżka do pliku kopii zapasowej skopiowana do schowka!\n\n{0}",
                ["FailedToCopyBackupPath"] = "Nie udało się skopiować ścieżki kopii zapasowej do schowka: {0}",
                ["BackupDirectoryNotFound"] = "Nie znaleziono katalogu kopii zapasowej:\n{0}",
                ["BackupFileNotFound"] = "Nie znaleziono pliku kopii zapasowej.",
                ["FirmwarePathCopied"] = "Ścieżka do pliku firmware skopiowana do schowka!\n\n{0}",
                ["FailedToCopyFirmwarePath"] = "Nie udało się skopiować ścieżki firmware do schowka: {0}",
                ["FirmwareFileNotFoundError"] = "Nie znaleziono pliku firmware.",
                ["DirectoryNotFoundTitle"] = "Nie Znaleziono Katalogu",
                ["OpenFolderErrorTitle"] = "Błąd Otwierania Folderu",
                
                // Help
                ["Help"] = "Pomoc",
                ["SelectConfiguration"] = "Wybierz konfigurację, aby zobaczyć szczegóły",
                ["DecodeAndPreview"] = "Dekoduj i Podgląd",
                ["SaveCurrentConfigButton"] = "Zapisz Bieżącą Konfigurację",
                ["LoadDecoded"] = "Załaduj Zdekodowaną Konfigurację",
                ["SaveDecoded"] = "Zapisz Zdekodowaną Konfigurację",
                
                // Configuration Manager Window
                ["ConfigurationManager"] = "Menedżer Konfiguracji",
                ["SavedConfigurations"] = "Zapisane Konfiguracje",
                ["LoadFromEncoded"] = "Załaduj z Zakodowanego Ciągu",
                ["SaveCurrentConfig"] = "Zapisz Bieżącą Konfigurację",
                ["Load"] = "Załaduj",
                ["Delete"] = "Usuń",
                ["ConfigurationDetails"] = "Szczegóły Konfiguracji:",
                ["ConfigName"] = "Nazwa:",
                ["Platform"] = "Platforma:",
                ["COMPort"] = "Port COM:",
                ["SavedDate"] = "Data Zapisu:",
                ["EncodedConfig"] = "Zakodowana Konfiguracja:",
                ["CopyEncoded"] = "Kopiuj Zakodowane",
                ["EnabledFlags"] = "Włączone Flagi ({0})",
                ["NoConfigurationsYet"] = "Nie zapisano jeszcze żadnych konfiguracji. Zapisz swoją pierwszą konfigurację z zakładki 'Zapisz Bieżącą Konfigurację'.",
                ["ConfirmDelete"] = "Czy na pewno chcesz usunąć konfigurację '{0}'?\n\n{1}",
                ["ConfirmDeleteTitle"] = "Potwierdź Usunięcie",
                ["ConfigDeleted"] = "Konfiguracja usunięta pomyślnie.",
                ["DeleteSuccess"] = "Usunięto Pomyślnie",
                ["DeleteFailed"] = "Nie udało się usunąć pliku konfiguracji.",
                ["DeleteFailedTitle"] = "Usunięcie Nie Powiodło Się",
                ["FailedToDelete"] = "Nie udało się usunąć konfiguracji: {0}",
                ["DeleteError"] = "Błąd Usuwania",
                ["EncodedCopied"] = "Zakodowana konfiguracja skopiowana do schowka!\n\nMożesz teraz udostępnić ten ciąg lub wkleić go w zakładce 'Załaduj z Zakodowanego Ciągu'.",
                ["NoEncodedValue"] = "Ta konfiguracja nie ma wartości zakodowanej.\n\nMogła zostać utworzona w starszej wersji.",
                ["NoEncodedValueTitle"] = "Brak Wartości Zakodowanej",
                ["EnterEncodedString"] = "Wprowadź zakodowany ciąg konfiguracji:",
                ["PasteFromClipboard"] = "Wklej ze Schowka",
                ["NoInput"] = "Wprowadź zakodowany ciąg konfiguracji.",
                ["NoInputTitle"] = "Brak Danych",
                ["DecodingFailed"] = "Nie udało się zdekodować konfiguracji.\n\nSprawdź, czy zakodowany ciąg jest prawidłowy i nie jest uszkodzony.",
                ["DecodingFailedTitle"] = "Dekodowanie Nie Powiodło Się",
                ["DecodingError"] = "Błąd dekodowania: {0}",
                ["DecodingErrorTitle"] = "Błąd Dekodowania",
                ["FlagsDecoded"] = "Zdekodowano {0} flag",
                ["ClipboardEmpty"] = "Schowek jest pusty.",
                ["NoContent"] = "Brak Zawartości",
                ["ClipboardInvalid"] = "Schowek nie zawiera tekstu.",
                ["InvalidContent"] = "Nieprawidłowa Zawartość",
                ["ContentPasted"] = "Zawartość wklejona pomyślnie!\n\nKliknij 'Dekoduj i Podgląd', aby wyświetlić konfigurację.",
                ["PasteSuccess"] = "Wklejono Pomyślnie",
                ["FailedToPaste"] = "Nie udało się wkleić ze schowka: {0}",
                ["PasteError"] = "Błąd Wklejania",
                ["DecodeConfigFirst"] = "Najpierw zdekoduj konfigurację.",
                ["NoConfiguration"] = "Brak Konfiguracji",
                ["NoFlagsEnabled"] = "Obecnie nie ma włączonych flag.\n\nWłącz niektóre flagi przed zapisaniem konfiguracji.",
                ["NoFlagsSelectedTitle"] = "Nie Wybrano Flag",
                ["ConfigurationSaved"] = "Konfiguracja '{0}' zapisana pomyślnie!\n\nBędzie teraz widoczna w zakładce 'Zapisane Konfiguracje'.",
                ["SaveSuccessTitle"] = "Zapisano Pomyślnie",
                ["ErrorSavingConfig"] = "Błąd zapisywania konfiguracji: {0}",
                ["SaveErrorTitle"] = "Błąd Zapisywania",
                ["ErrorLoadingConfig"] = "Błąd ładowania konfiguracji: {0}",
                ["LoadError"] = "Błąd Ładowania",
                ["FirmwareFileNotFound"] = "Nie znaleziono pliku firmware:\n\n{0}\n\nPlik firmware mógł zostać usunięty lub przeniesiony.",
                ["EncodedConfigCopied"] = "Zakodowana konfiguracja skopiowana do schowka!",
                ["NotSpecified"] = "Nie określono",
                ["FlagsCount"] = "{0} flag",
                
                // Additional ConfigurationManager labels
                ["LoadFromEncodedTitle"] = "Załaduj ustawienia z zakodowanej konfiguracji",
                ["LoadFromEncodedDescription"] = "Wklej zakodowaną konfigurację, aby go zdekodować i załadować.",
                ["EncodedConfigString"] = "Zakodowana konfiguracja:",
                ["DecodedConfiguration"] = "Zdekodowana konfiguracja",
                ["FlagsLabel"] = "Flagi:",
                ["SaveCurrentConfigDescription"] = "Zapisz aktualnie wybrane flagi kompilacji do biblioteki konfiguracji.",
                ["ConfigurationSavedTitle"] = "Konfiguracja Zapisana",
                ["EncodedStringForSharing"] = "Zakodowana konfiguracja (do udostępnienia):",
                
                // Compilation Results Window
                ["BuildConfigurationDescription"] = "Ten zakodowany ciąg zawiera Twoją pełną konfigurację kompilacji i może być udostępniony lub ponownie użyty.",
                ["BackupCreatedMessage"] = "✓ Pamięć flash urządzenia została zabezpieczona przed wdrożeniem.",
                ["FirmwareReadyMessage"] = "✓ Binarny plik firmware gotowy do wdrożenia lub ręcznego flashowania.",
                
                // Configuration Name Input Window
            };
        }
    }
}
