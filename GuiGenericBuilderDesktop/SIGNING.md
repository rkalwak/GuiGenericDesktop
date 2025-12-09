# Code Signing Setup for GUI Generic Builder Desktop

## Overview
This application supports two types of signing:
1. **Strong-Name Signing** - For .NET assembly identity
2. **Authenticode Signing** - For Windows executable code signing

## Setup Instructions

### 1. Strong-Name Signing (Required for Build)

Generate the strong-name key file:

```powershell
# Navigate to the GuiGenericBuilderDesktop project directory
cd GuiGenericBuilderDesktop

# Generate the strong-name key file
sn -k GuiGenericBuilder.snk
```

This file (`GuiGenericBuilder.snk`) should be committed to source control (it's for assembly identity, not security).

### 2. Authenticode Code Signing (Optional - For Distribution)

#### Option A: Self-Signed Certificate (Development/Testing)

Create a self-signed certificate for testing:

```powershell
# Create self-signed certificate
$cert = New-SelfSignedCertificate -Type CodeSigningCert `
    -Subject "CN=GUI Generic Builder Development" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears(5)

# Export to PFX with password
$password = ConvertTo-SecureString -String "YourPasswordHere" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "GuiGenericBuilder.pfx" -Password $password

# Move to project directory
Move-Item GuiGenericBuilder.pfx .\GuiGenericBuilderDesktop\
```

**Important**: Add `GuiGenericBuilder.pfx` to `.gitignore` - never commit certificates to source control!

#### Option B: Commercial Certificate (Production)

1. Purchase a code signing certificate from a trusted CA:
   - DigiCert
   - Sectigo (formerly Comodo)
   - GlobalSign

2. Receive your certificate as a `.pfx` file

3. Place the `.pfx` file in the `GuiGenericBuilderDesktop` directory

4. Name it `GuiGenericBuilder.pfx` (or update the project file)

### 3. Build Configuration

#### Setting Certificate Password

The certificate password can be provided in three ways:

**Option 1: Environment Variable (Recommended for CI/CD)**
```powershell
$env:CertificatePassword = "YourPassword"
dotnet build -c Release
```

**Option 2: MSBuild Property**
```powershell
dotnet build -c Release /p:CertificatePassword=YourPassword
```

**Option 3: Project File (Not Recommended - Security Risk)**
Edit `GuiGenericBuilderDesktop.csproj` and add (use only for testing):
```xml
<PropertyGroup>
  <CertificatePassword>YourPassword</CertificatePassword>
</PropertyGroup>
```

### 4. Build the Application

```powershell
# Debug build (no code signing)
dotnet build

# Release build (with code signing if certificate exists)
dotnet build -c Release /p:CertificatePassword=YourPassword
```

## Verification

### Verify Strong-Name Signing
```powershell
sn -vf .\bin\Release\net10.0-windows\GuiGenericBuilderDesktop.exe
```

### Verify Authenticode Signing
```powershell
# View signature
Get-AuthenticodeSignature .\bin\Release\net10.0-windows\GuiGenericBuilderDesktop.exe | Format-List

# Or use signtool
signtool verify /pa .\bin\Release\net10.0-windows\GuiGenericBuilderDesktop.exe
```

## .gitignore Additions

Add these lines to your `.gitignore`:

```
# Code signing certificates (NEVER commit these!)
*.pfx
*.p12

# Strong-name key can be committed (it's public)
# *.snk
```

## CI/CD Integration

### GitHub Actions Example

```yaml
- name: Sign Application
  env:
    CERTIFICATE_PASSWORD: ${{ secrets.CERTIFICATE_PASSWORD }}
  run: |
    # Decode certificate from secrets
    echo "${{ secrets.CERTIFICATE_BASE64 }}" | base64 -d > GuiGenericBuilderDesktop/GuiGenericBuilder.pfx
    
    # Build with signing
    dotnet build -c Release /p:CertificatePassword=$env:CERTIFICATE_PASSWORD
    
    # Clean up certificate
    Remove-Item GuiGenericBuilderDesktop/GuiGenericBuilder.pfx
```

## Troubleshooting

### Strong-Name Signing Fails
- Ensure `GuiGenericBuilder.snk` exists in the project directory
- Run: `sn -k GuiGenericBuilder.snk`

### Code Signing Fails
- Verify certificate file exists: `GuiGenericBuilder.pfx`
- Check certificate password is correct
- Ensure you're building in Release configuration
- Install Windows SDK for signtool.exe

### signtool.exe Not Found
Install Windows SDK from: https://developer.microsoft.com/windows/downloads/windows-sdk/

Or update the SignToolPath in the project file to match your SDK version.

## Security Best Practices

1. **Never commit `.pfx` files** to source control
2. Store certificate passwords in secure secret management (Azure Key Vault, GitHub Secrets, etc.)
3. Use different certificates for development and production
4. Rotate certificates before expiration
5. Use hardware security modules (HSM) for production certificates when possible

## Notes

- Strong-name signing is automatic on every build
- Authenticode signing only occurs on Release builds when certificate is present
- The build will succeed even if code signing fails (ContinueOnError=true)
- Signing is skipped if `GuiGenericBuilder.pfx` doesn't exist
