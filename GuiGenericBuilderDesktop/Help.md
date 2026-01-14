# GUI-Generic Builder - Help Guide

## Welcome to GUI-Generic Builder

This application helps you compile and deploy firmware for Supla smart home devices using the GUI-Generic framework.

---

## Getting Started

### 1. Update GUI-Generic Repository

Before compiling any firmware, you need to download the GUI-Generic repository:

1. Click **"1. Update Gui-Generic"** button
2. Wait for the download to complete (indicated by ? message)
3. The repository contains all necessary source files for compilation

**Note**: This step is required only once, or when you want to update to the latest version.

---

### 2. Device Detection

To automatically detect your connected ESP device:

1. Connect your ESP32/ESP8266 device via USB
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
- Red warning appears if you try to enable incompatible flag
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
- Compact: Uses GZip compression
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

## Deployment Options

### Deploy Checkbox
- **Checked**: Firmware is uploaded to device after compilation
- **Unchecked**: Only compile, no upload (firmware saved to file)
- Requires valid COM port selection

### Backup Checkbox
- **Checked** (default): Creates backup before deployment
- Backup stored in `backup/` directory
- Includes full flash dump (4-16MB)
- Two files created:
  - `*.backup`: Flash memory dump
  - `*.info`: Metadata (date, device info, size)

### Erase Flash Checkbox
- **Checked**: Erases flash memory before uploading firmware
- **Unchecked** (default): Flash not erased
- Recommended for:
  - Clean installation
  - Switching between different firmware versions
  - Troubleshooting issues

---

## Firmware Files

### File Locations

**Configurations Directory**: `configurations/`
- Configuration JSON files
- Firmware binaries (`*.bin`)
- Merged ZIP packages (`*_merged.zip`)

**Backup Directory**: `backup/`
- Flash backups (`*.backup`)
- Backup metadata (`*.info`)

### Merged ZIP Files

Automatically created after compilation, contains:
- `firmware.bin` - Main application firmware
- `bootloader.bin` - ESP32 bootloader
- `partitions.bin` - Partition table
- `firmware_merged.bin` - Complete merged binary (recommended)
- `README.txt` - Flashing instructions

**Manual Flashing**:
```bash
# Option 1: Flash complete binary (recommended)
esptool --chip esp32 --port COM3 write_flash 0x0 firmware_merged.bin

# Option 2: Flash individual files
esptool --chip esp32 --port COM3 write_flash \
  0x1000 bootloader.bin \
  0x8000 partitions.bin \
  0x10000 firmware.bin
```

---

## Troubleshooting

### Device Not Detected

**Problem**: "No device detected" message

**Solutions**:
1. Check USB cable (must support data transfer)
2. Install USB-to-UART drivers:
   - CH340/CH341: [Download from manufacturer](http://www.wch-ic.com/downloads/CH341SER_ZIP.html)
   - CP2102: [Silicon Labs drivers](https://www.silabs.com/developers/usb-to-uart-bridge-vcp-drivers)
   - FTDI: Usually included in Windows
3. Try different USB port
4. Put device in flash mode (hold BOOT button, press RESET)

### Compilation Failed

**Problem**: Compilation error message

**Solutions**:
1. Check error logs in compilation results window
2. Verify repository is updated: Click "1. Update Gui-Generic"
3. Disable conflicting flags
4. Check platform compatibility (some flags don't work on ESP32-C3/C6/S2/S3)
5. Ensure enough free disk space

### Incompatible Flags

**Problem**: Flag checkbox won't stay checked

**Solution**:
- Flag is incompatible with selected platform
- Check platform restrictions in flag description
- Use compatible platform (usually ESP32 default)

### COM Port Issues

**Problem**: Can't select COM port or upload fails

**Solutions**:
1. Close other applications using the port (Arduino IDE, PuTTY, etc.)
2. Disconnect and reconnect device
3. Try different COM port in dropdown
4. Check Windows Device Manager for port conflicts

### WebView2 Not Available

**Problem**: Changelog/Help window shows error

**Solution**:
- Install Microsoft Edge WebView2 Runtime
- Download from: https://go.microsoft.com/fwlink/p/?LinkId=2124703
- Usually pre-installed on Windows 10/11

---

## Language Support

Switch between languages using the **Language** dropdown:
- **Polski** (Polish) - Default
- **English**

Language affects:
- All UI elements
- Flag names and descriptions
- Error messages
- Help and documentation

---

## Tips and Best Practices

### 1. Start Simple
- Begin with basic configuration (SUPLA_CONFIG + required sensors)
- Test compilation before adding more features
- Add features incrementally

### 2. Save Configurations
- Save working configurations with descriptive names
- Create backups before major changes
- Share configurations with encoded strings

### 3. Use Auto-Detection
- Let the app detect device (more reliable)
- Auto-detection sets optimal settings
- Prevents platform compatibility issues

### 4. Enable Backup
- Always create backup before deployment (default: enabled)
- Backups allow recovery if firmware has issues
- Store backups safely for future reference

### 5. Check Compatibility
- Review incompatible flags when switching platforms
- ESP32 (default) has best compatibility
- ESP32-C3/C6/S2/S3 have hardware limitations

### 6. Monitor Compilation Time
- First compilation takes longer (downloads dependencies)
- Typical compilation: 30-90 seconds
- Very long times may indicate issues

### 7. Update Regularly
- Update GUI-Generic repository for latest features
- Check changelog for new features and fixes
- Update firmware on devices periodically

---

## Keyboard Shortcuts

- **ESC**: Close current window
- **Ctrl+C**: Copy (in text fields)
- **Ctrl+V**: Paste (in text fields)

---

## Additional Resources

### Documentation
- **GUI-Generic GitHub**: https://github.com/krycha88/GUI-Generic
- **Supla Website**: https://www.supla.org/
- **Supla Forum**: https://forum.supla.org/

### Support
- Check changelog for recent updates
- Report issues on GitHub
- Ask questions on Supla forum

---

## Credits

**GUI-Generic Builder Desktop** - Desktop application for building Supla firmware

**GUI-Generic Framework** by krycha88 - Web-based configuration for Supla devices

**Supla** - Open-source home automation system

---

*Last Updated: January 2025*
*Version: 2.0.6*
