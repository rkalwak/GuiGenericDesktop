using System.Collections.Generic;

namespace GuiGenericBuilderDesktop.Localization
{
    /// <summary>
    /// English translations
    /// </summary>
    internal static class EnglishTranslations
    {
        public static Dictionary<string, string> GetTranslations()
        {
            return new Dictionary<string, string>
            {
                // Main Window
                ["AppTitle"] = "GUI-Generic Builder",
                ["Board"] = "Board:",
                ["Port"] = "Port:",
                ["Flash"] = "Flash:",
                ["None"] = "None",
                ["ManageConfigs"] = "Manage Configs...",
                ["UpdateGuiGeneric"] = "1. Update Gui-Generic",
                ["CheckDevice"] = "2. Check Device",
                ["Compile"] = "3. Compile",
                ["StopCompilation"] = "3. Stop compilation",
                ["Deploy"] = "Deploy",
                ["Backup"] = "Backup",
                ["EraseFlash"] = "Erase Flash",
                ["EraseFlashTooltip"] = "Erase flash memory before deploying firmware (recommended for clean installation)",
                ["BackupTooltip"] = "Create backup before deploying firmware",
                
                // Column Headers
                ["Enabled"] = "Enabled",
                ["Key"] = "Key",
                ["Name"] = "Name",
                ["Description"] = "Description",
                ["Parameters"] = "Parameters",
                ["ParamsButton"] = "Params...",
                
                // Status Messages
                ["DownloadingRepository"] = "? Downloading GUI-Generic repository...",
                ["RepositoryUpdatedSuccess"] = "? Repository updated successfully!",
                ["RepositoryUpdateFailed"] = "? Repository update failed",
                ["DetectingDevice"] = "? Detecting device...",
                ["DeviceDetected"] = "? Device detected: {0} on {1}",
                ["NoDeviceDetected"] = "? No device detected",
                ["DeviceDetectionError"] = "? Device detection error",
                ["CompilingFirmware"] = "? Compiling firmware... Elapsed: {0:F1}s",
                ["CompilationSuccessful"] = "? Compilation successful! Time: {0:F1}s",
                ["CompilationFailed"] = "? Compilation failed! Time: {0:F1}s",
                ["CompilationError"] = "? Compilation error! Time: {0:F1}s",
                ["StoppingCompilation"] = "? Stopping compilation...",
                ["CompilationStopped"] = "? Compilation stopped. Time: {0:F1}s",
                
                // Message Box Titles
                ["UpdateComplete"] = "Update Complete",
                ["UpdateFailed"] = "Update Failed",
                ["DeviceNotFound"] = "Device Not Found",
                ["DetectionError"] = "Detection Error",
                ["Error"] = "Error",
                ["Warning"] = "Warning",
                ["Information"] = "Information",
                ["Success"] = "Success",
                
                // Message Box Messages - Main Window
                ["RepositoryUpdatedMessage"] = "Repository updated successfully!",
                ["ErrorUpdatingRepository"] = "Error updating repository: {0}",
                ["NoDeviceDetectedMessage"] = @"No ESP device detected.

Please ensure:
• Device is connected via USB
• USB drivers are installed
• Device is powered on",
                
                ["DeviceDetectionErrorMessage"] = @"Device detection error: {0}

Check the logs for more details.",
                
                ["RepositoryNotFound"] = @"GUI-Generic repository not found!

Please click '1. Update Gui-Generic' button first to download the repository.

The repository is required for firmware compilation.",
                
                ["RepositoryNotFoundTitle"] = "Repository Not Found",
                
                ["EmptyRepository"] = @"GUI-Generic repository directory is empty!

Repository path: {0}

Please click '1. Update Gui-Generic' button to download the repository.",
                
                ["EmptyRepositoryTitle"] = "Empty Repository",
                
                ["IncompleteRepository"] = @"GUI-Generic repository appears to be incomplete or corrupted.

Missing file: platformio.ini
Repository path: {0}

Please click '1. Update Gui-Generic' button to re-download the repository.",
                
                ["IncompleteRepositoryTitle"] = "Incomplete Repository",
                ["NoFlagsSelected"] = "No flags selected. Enable some flags before compiling.",
                ["NoFlags"] = "No Flags",
                
                ["PlatformRequired"] = @"Please select a target platform (board) before compiling.

Use '2. Check Device' to auto-detect, or manually select a platform from the Board dropdown.

Available platforms:
• ESP32 (default)
• ESP32-C3
• ESP32-C6
• ESP32-S3",
                
                ["PlatformRequiredTitle"] = "Platform Required",
                
                ["PlatformCompatibilityError"] = @"The following flags are not compatible with the selected platform ({0}):

{1}

Please disable these flags or select a different platform before compiling.",
                
                ["PlatformCompatibilityErrorTitle"] = "Platform Compatibility Error",
                ["I2CConfigurationError"] = "{0}",
                ["I2CConfigurationErrorTitle"] = "I2C Configuration Error",
                
                ["COMPortRequired"] = @"Please select a COM port before compiling with deployment.

The firmware needs to be uploaded to a device connected via COM port.
Use '2. Check Device' to auto-detect, or manually select a COM port.

Alternatively, uncheck 'Deploy' to compile only without uploading.",
                
                ["COMPortRequiredTitle"] = "COM Port Required",
                
                ["CompilationStoppedMessage"] = @"Compilation stopped by user.

Elapsed time: {0:F1}s",
                
                ["CompilationStoppedTitle"] = "Compilation Stopped",
                ["CompilationErrorMessage"] = "Compilation error: {0}",
                ["ErrorTitle"] = "Error",
                
                ["PlatformCompatibility"] = @"The following flags are not compatible with the selected platform ({0}) and have been disabled:

{1}",
                
                ["PlatformCompatibilityTitle"] = "Platform Compatibility",
                
                ["PlatformIncompatibility"] = @"The flag '{0}' is not compatible with the selected platform ({1}).

This flag is disabled on: {2}",
                
                ["PlatformIncompatibilityTitle"] = "Platform Incompatibility",
                ["BlockingDependencies"] = "Blocking Dependencies",
                ["NoParameters"] = "Selected flag has no parameters.",
                ["NoParametersTitle"] = "No Parameters",
                ["NoSelection"] = "No flag selected. Select a flag in the grid and try again.",
                ["NoSelectionTitle"] = "No Selection",
                ["ThisFlagHasNoParameters"] = "This flag has no parameters.",
                
                ["ConfigurationLoaded"] = @"Configuration '{0}' loaded successfully!

Platform: {1}
COM Port: {2}
Enabled flags: {3}",
                
                ["ConfigurationLoadedTitle"] = "Configuration Loaded",
                
                ["ManualConfigurationRequired"] = @"The configuration '{0}' has no saved flags.

Please manually select the build flags.",
                
                ["ManualConfigurationRequiredTitle"] = "Manual Configuration Required",
                ["ErrorOpeningConfigManager"] = "Error opening configuration manager: {0}",
                ["FileNotFound"] = "File Not Found",
                ["BuilderJsonNotFound"] = "builder.json not found at: {0}",
                ["InvalidSections"] = "builder.json does not contain valid Sections",
                ["ErrorLoadingBuilderJson"] = "Error loading builder.json: {0}",
                
                // Language
                ["Language"] = "Language:",
                ["LanguageTooltip"] = "Select application language",
                ["LanguageChanged"] = "Language changed to: {0}",
                
                // Compilation Results Window
                ["CompilationSuccessTitle"] = "Compilation Successful",
                ["CompilationFailedTitle"] = "Compilation Failed",
                ["BuildConfiguration"] = "Build Configuration:",
                ["CopyConfig"] = "Copy Configuration",
                ["BackupFile"] = "Backup File:",
                ["FirmwareFile"] = "Firmware File:",
                ["BuildOutput"] = "Build Output:",
                ["ErrorLogs"] = "Error Logs:",
                ["CopyOutput"] = "Copy Output",
                ["CopyLogs"] = "Copy Logs",
                ["SaveLogs"] = "Save Logs",
                ["CopyPath"] = "Copy Path",
                ["OpenFolder"] = "Open Folder",
                ["Close"] = "Close",
                ["ConfigCopied"] = "Configuration string copied to clipboard successfully!",
                ["CopySuccess"] = "Copy Success",
                ["CopyError"] = "Copy Error",
                ["FailedToCopy"] = "Failed to copy to clipboard: {0}",
                ["LogsSaved"] = "Content saved successfully to:\n{0}",
                ["SaveSuccess"] = "Save Success",
                ["SaveError"] = "Save Error",
                ["FailedToSave"] = "Failed to save: {0}",
                ["PathCopied"] = "File path copied to clipboard!\n\n{0}",
                ["FileNotFoundError"] = "File not found.",
                ["DirectoryNotFound"] = "Directory not found:\n{0}",
                ["OpenFolderError"] = "Failed to open folder: {0}",
                ["NoConfigurationAvailable"] = "No configuration available.",
                ["NoLogsAvailable"] = "No logs available.",
                ["OutputCopied"] = "Output copied to clipboard successfully!",
                ["LogsCopied"] = "Logs copied to clipboard successfully!",
                ["FailedToCopyConfig"] = "Failed to copy configuration string to clipboard: {0}",
                ["LogFilesFilter"] = "Log files (*.log)|*.log|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                ["SaveCompilationOutput"] = "Save Compilation Output",
                ["SaveCompilationLogs"] = "Save Compilation Logs",
                ["BuildConfigurationString"] = "Build Configuration String",
                ["BackupPathCopied"] = "Backup file path copied to clipboard!\n\n{0}",
                ["FailedToCopyBackupPath"] = "Failed to copy backup path to clipboard: {0}",
                ["BackupDirectoryNotFound"] = "Backup directory not found:\n{0}",
                ["BackupFileNotFound"] = "Backup file not found.",
                ["FirmwarePathCopied"] = "Firmware file path copied to clipboard!\n\n{0}",
                ["FailedToCopyFirmwarePath"] = "Failed to copy firmware path to clipboard: {0}",
                ["FirmwareFileNotFoundError"] = "Firmware file not found.",
                ["DirectoryNotFoundTitle"] = "Directory Not Found",
                ["OpenFolderErrorTitle"] = "Open Folder Error",
                
                // Configuration Manager Window
                ["ConfigurationManager"] = "Configuration Manager",
                ["SavedConfigurations"] = "Saved Configurations",
                ["LoadFromEncoded"] = "Load from Encoded String",
                ["SaveCurrentConfig"] = "Save Current Configuration",
                ["Load"] = "Load",
                ["Delete"] = "Delete",
                ["ConfigurationDetails"] = "Configuration Details:",
                ["ConfigName"] = "Name:",
                ["Platform"] = "Platform:",
                ["COMPort"] = "COM Port:",
                ["SavedDate"] = "Saved Date:",
                ["EncodedConfig"] = "Encoded Configuration:",
                ["CopyEncoded"] = "Copy Encoded",
                ["EnabledFlags"] = "Enabled Flags ({0})",
                ["NoConfigurationsYet"] = "No configurations saved yet. Save your first configuration from the 'Save Current Configuration' tab.",
                ["SelectConfiguration"] = "Select a configuration to see details",
                ["ConfirmDelete"] = "Are you sure you want to delete configuration '{0}'?\n\n{1}",
                ["ConfirmDeleteTitle"] = "Confirm Delete",
                ["ConfigDeleted"] = "Configuration deleted successfully.",
                ["DeleteSuccess"] = "Delete Success",
                ["DeleteFailed"] = "Failed to delete configuration file.",
                ["DeleteFailedTitle"] = "Delete Failed",
                ["FailedToDelete"] = "Failed to delete configuration: {0}",
                ["DeleteError"] = "Delete Error",
                ["EncodedCopied"] = "Encoded configuration copied to clipboard!\n\nYou can now share this string or paste it in the 'Load from Encoded String' tab.",
                ["NoEncodedValue"] = "This configuration does not have an encoded value.\n\nIt may have been created with an older version.",
                ["NoEncodedValueTitle"] = "No Encoded Value",
                ["EnterEncodedString"] = "Enter an encoded configuration string:",
                ["PasteFromClipboard"] = "Paste from Clipboard",
                ["DecodeAndPreview"] = "Decode and Preview",
                ["NoInput"] = "Please enter an encoded configuration string.",
                ["NoInputTitle"] = "No Input",
                ["DecodingFailed"] = "Failed to decode the configuration.\n\nPlease check that the encoded string is valid and not corrupted.",
                ["DecodingFailedTitle"] = "Decoding Failed",
                ["DecodingError"] = "Decoding error: {0}",
                ["DecodingErrorTitle"] = "Decoding Error",
                ["FlagsDecoded"] = "{0} flags decoded",
                ["LoadDecoded"] = "Load Decoded Configuration",
                ["SaveDecoded"] = "Save Decoded Configuration",
                ["ClipboardEmpty"] = "Clipboard is empty.",
                ["NoContent"] = "No Content",
                ["ClipboardInvalid"] = "Clipboard does not contain text.",
                ["InvalidContent"] = "Invalid Content",
                ["ContentPasted"] = "Content pasted successfully!\n\nClick 'Decode and Preview' to view the configuration.",
                ["PasteSuccess"] = "Paste Success",
                ["FailedToPaste"] = "Failed to paste from clipboard: {0}",
                ["PasteError"] = "Paste Error",
                ["DecodeConfigFirst"] = "Please decode a configuration first.",
                ["NoConfiguration"] = "No Configuration",
                ["SaveCurrentConfigButton"] = "Save Current Configuration",
                ["NoFlagsEnabled"] = "No flags are currently enabled.\n\nPlease enable some flags before saving the configuration.",
                ["NoFlagsSelectedTitle"] = "No Flags Selected",
                ["ConfigurationSaved"] = "Configuration '{0}' saved successfully!\n\nIt will now appear in the 'Saved Configurations' tab.",
                ["SaveSuccessTitle"] = "Save Success",
                ["ErrorSavingConfig"] = "Error saving configuration: {0}",
                ["SaveErrorTitle"] = "Save Error",
                ["ErrorLoadingConfig"] = "Error loading configuration: {0}",
                ["LoadError"] = "Load Error",
                
                // Additional ConfigurationManager keys
                ["FirmwareFileNotFound"] = "Firmware file not found:\n\n{0}\n\nThe firmware file may have been deleted or moved.",
                ["EncodedConfigCopied"] = "Encoded configuration copied to clipboard!",
                ["NotSpecified"] = "Not specified",
                ["FlagsCount"] = "{0} flags",
                
                // Configuration Name Input Window
                ["EnterConfigurationName"] = "Enter Configuration Name",
                ["ConfigurationName"] = "Configuration Name:",
                ["Save"] = "Save",
                ["Cancel"] = "Cancel",
                ["NameRequired"] = "Please enter a configuration name.",
                ["NameRequiredTitle"] = "Name Required",
                ["InvalidCharacters"] = "Configuration name contains invalid characters.\n\nPlease avoid using: \\ / : * ? \" < > |",
                ["InvalidCharactersTitle"] = "Invalid Characters",
                ["InvalidName"] = "Configuration name cannot be empty.",
                ["InvalidNameTitle"] = "Invalid Name",
            };
        }
    }
}
