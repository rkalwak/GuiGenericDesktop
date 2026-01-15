# GUI-Generic Builder - Help Guide

## Welcome to GUI-Generic Builder

This application helps you compile and deploy firmware for Supla devices using the GUI-Generic framework.

---

## Getting Started

### 1. Update GUI-Generic Repository

Before compiling any firmware, you need to download the GUI-Generic repository:

1. Click **"1. Update Gui-Generic"** button
2. Wait for the download to complete (indicated by message)
3. The repository contains all necessary source files for compilation

**Note**: This step is required only once, or when you want to update to the latest version.

---

### 2. Device Detection

To automatically detect your connected ESP device:

1. Connect your ESP32 device via USB
2. Click **"2. Check Device"** button
3. The application will:
   - Detect the COM port automatically
   - Identify the chip type (ESP32, ESP32-C3, ESP32-C6, ESP32-S3)
   - Set the flash size
   - Configure incompatible flags

**Supported Devices**:
- ESP32 (all variants)
- ESP32-C3
- ESP32-C6
- ESP32-S2
- ESP32-S3

---

### 3. Configure Build Flags

Build flags control which features are included in your firmware:

#### Selecting Flags
- Browse through flag sections (organized by functionality)
- Check the boxes for features you want to enable
- Click **"Params..."** button to configure flag parameters

#### Section Checkboxes
- Click the checkbox next to section name to enable/disable all flags in that section
- Three states:
  - ? Checked: All flags enabled
  - ? Unchecked: All flags disabled
  - ? Indeterminate: Some flags enabled

#### Flag Dependencies
Some flags have dependencies on other flags:
- **Auto-Enable**: Related flags are automatically enabled
- **Auto-Disable**: Conflicting flags are automatically disabled
- **Blocking**: Some flags require other flags to be enabled first

#### Platform Compatibility
Not all flags work on all platforms:
- Incompatible flags are automatically disabled when you select a board
- Pre-compilation validation prevents invalid configurations

---

### 4. Platform Selection

Choose your target platform from the **Board** dropdown:

- **ESP32** (default) - 4MB, 8MB, 16MB flash
- **ESP32-C3** - 4MB, 8MB, 16MB flash
- **ESP32-C6** - 4MB, 8MB, 16MB flash
- **ESP32-S3** - 4MB, 8MB, 16MB, 32MB flash

**Flash Size Selection**:
- Choose from available flash sizes for your platform
- Larger flash = more space for features and OTA updates
- Auto-detected if using "2. Check Device"

---

### 5. Compilation

Once you've configured your flags:

1. Select **COM Port** (if deploying to device)
2. Choose compilation options:
   - **Deploy**: Upload firmware to device after compilation
   - **Backup**: Create backup of current firmware before deployment
   - **Erase Flash**: Erase flash memory before uploading (recommended for clean install)
3. Click **"3. Compile"** button

#### Compilation Process
- Elapsed time is shown during compilation
- Status indicators:
  - ? Compiling firmware... (black text)
  - ? Compilation successful! (green)
  - ? Compilation failed (red)
- Click **"3. Stop Compilation"** to cancel if needed

#### After Compilation
Upon successful compilation:
- Configuration is automatically saved
- Firmware binary is stored in `configurations/` directory
- Backup is saved in `backup/` directory as `Config_YYYYMMDD_HHMMSS.bin` (with the same timestamp as the configuration)
- Compilation results window shows:
  - Encoded configuration string (for sharing)
  - Backup file location (if created)
  - Compiled firmware location
  - Buttons to open folders and copy paths

---

## Configuration Management

### Saving Configurations

**Automatic Save**:
- After each successful compilation
- Filename: `Config_YYYYMMDD_HHMMSS.json`
- Includes firmware binary and merged ZIP

**Manual Save**:
1. Open **"Manage Configurations..."**
2. Go to **"Save Current Configuration"** tab
3. Click **"Save Current Configuration"** button
4. Enter a custom name
5. Configuration is saved with all enabled flags

### Loading Configurations

1. Click **"Manage Configurations..."** button
2. Select a configuration from the list
3. Click **"Load"** or double-click the configuration
4. All flags and settings are restored

### Encoded Configuration Strings

Each configuration has an encoded string that contains all selected flags:

**Features**:
- Reversible: Can be decoded back to original flags
- Shareable: Copy and paste to share configurations
- URL-safe: Can be used in URLs

**To Use**:
1. Copy encoded string from configuration
2. Share with others
3. Recipient pastes in **"Load from Encoded String"** tab
4. Configuration is decoded and can be loaded

---

## Features Overview

### Common Build Flags

#### Core Components
- **SUPLA_CONFIG**: Web configuration interface (usually required)
- **SUPLA_RELAY**: Relay control support
- **SUPLA_BUTTON**: Physical button support
- **SUPLA_LED**: LED indicators

#### Sensors
- **SUPLA_DS18B20**: Temperature sensors (Dallas)
- **SUPLA_DHT11/DHT22**: Temperature and humidity sensors
- **SUPLA_BME280**: Temperature, humidity, pressure sensor
- **SUPLA_BMP280**: Temperature and pressure sensor
- **SUPLA_SHT**: SHT3x/SHT4x sensors
- **SUPLA_SI7021**: Temperature and humidity sensor

#### Power Monitoring
- **SUPLA_HLW8012**: Power measurement (Sonoff Pow)
- **SUPLA_CSE7766**: Power measurement (Sonoff Pow R2)
- **SUPLA_ADE7953**: Dual channel power measurement (Shelly 2.5)
- **SUPLA_PZEM**: PZEM-004T power meter

#### Displays
- **SUPLA_OLED**: OLED display support (SSD1306, SH1106)
- **SUPLA_MAX7219**: LED matrix display

#### Connectivity
- **SUPLA_DIRECT_LINKS**: Device linking without cloud
- **SUPLA_DEEP_SLEEP**: Power saving mode
- **SUPLA_MQTT**: MQTT protocol support

#### Special Features
- **SUPLA_RGBW**: RGB/RGBW lighting control
- **SUPLA_DIMMER**: Dimmer control
- **SUPLA_ROLLERSHUTTER**: Roller shutter control
- **SUPLA_IMPULSE_COUNTER**: Pulse counting (electricity, water, gas meters)

---

## Troubleshooting

### Compilation Errors

**Problem**: Compilation fails with errors
**Solution**:
1. Check error logs in output panel
2. Ensure all required flags are enabled
3. Check flag compatibility with selected platform
4. Update GUI-Generic repository
5. Ensure PlatformIO Core is properly installed

### Device Detection Issues

**Problem**: Device not detected
**Solution**:
1. Check USB connection
2. Install USB drivers (CP210x or CH340)
3. Try different USB cable
4. Close other programs using serial port
5. Manually select COM port from dropdown

### Upload Errors

**Problem**: Upload fails
**Solution**:
1. Ensure correct COM port is selected
2. Hold BOOT button during upload (on some boards)
3. Try reducing baud rate
4. Enable "Erase Flash" option
5. Check USB connection

### Memory Issues

**Problem**: Firmware too large for selected flash size
**Solution**:
1. Disable some unused features
2. Select board with larger flash
3. Reduce debug level
4. Remove unused sensors or modules

---

## Advanced Features

### Backup and Restore

Always backup your working firmware:

1. Enable "Backup" option before uploading
2. Backup files are stored in configuration directory
3. To restore:
   - Use esptool.py
   - Or re-upload saved configuration

### Offline Mode

Application can work without internet connection:
- GUI-Generic repository is stored locally
- Compilations are performed locally
- Required only for initial download/updates

### Configuration Sharing

Share your configurations:
1. Export encoded string
2. Share via email, forums, or social media
3. Other users can import your configuration
4. Perfect for sharing working configurations

---

## Keyboard Shortcuts

- **F1**: Open this help
- **Ctrl+S**: Save current configuration
- **Ctrl+O**: Open configuration manager
- **Ctrl+B**: Start compilation
- **Esc**: Close current dialog

---

## Additional Resources

### Links

- **GUI-Generic Documentation**: [github.com/rkalwak/GUI-Generic](https://github.com/rkalwak/GUI-Generic)
- **SUPLA Forum**: [forum.supla.org](https://forum.supla.org)
- **PlatformIO Documentation**: [docs.platformio.org](https://docs.platformio.org)

### Support

If you encounter issues:
1. Check this help for solutions
2. Search SUPLA forum
3. Report bugs on GitHub
4. Contact the community

**Application Logs**:
- Application logs are stored in the `logs/` directory
- Attach log files to forum posts if requested by developer
- Logs contain detailed information about compilation process and errors

---

## About the Application

**GUI-Generic Builder** was created to simplify the SUPLA firmware compilation process.

**Features**:
- ? Automatic device detection
- ? Visual build flag management
- ? Dependency verification
- ? Platform compatibility checking
- ? Configuration management
- ? Backup and restore
- ? Configuration sharing
- ? Multi-language support

**License**: MIT

**Version**: 2.0

---

*Last Updated: 2026*
