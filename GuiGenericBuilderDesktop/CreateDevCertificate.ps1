# PowerShell script to create a self-signed code signing certificate for development

Write-Host "Creating Self-Signed Code Signing Certificate..." -ForegroundColor Cyan

$projectDir = $PSScriptRoot
$pfxFile = Join-Path $projectDir "GuiGenericBuilder.pfx"

# Check if certificate already exists
if (Test-Path $pfxFile) {
    $response = Read-Host "GuiGenericBuilder.pfx already exists. Overwrite? (y/N)"
    if ($response -ne 'y' -and $response -ne 'Y') {
        Write-Host "Skipping certificate creation." -ForegroundColor Yellow
        exit 0
    }
}

# Prompt for password
$password = Read-Host "Enter password for certificate (or press Enter for default)" -AsSecureString
if ([string]::IsNullOrEmpty((New-Object PSCredential "user", $password).GetNetworkCredential().Password)) {
    $password = ConvertTo-SecureString -String "GuiGenericBuilder2024!" -Force -AsPlainText
    Write-Host "Using default password: GuiGenericBuilder2024!" -ForegroundColor Yellow
}

try {
    # Create self-signed certificate
    Write-Host "Creating certificate..." -ForegroundColor Cyan
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject "CN=GUI Generic Builder Development, O=GUI Generic Builder, C=US" `
        -KeyUsage DigitalSignature `
        -FriendlyName "GUI Generic Builder Development Certificate" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3") `
        -NotAfter (Get-Date).AddYears(5)

    Write-Host "Certificate created with thumbprint: $($cert.Thumbprint)" -ForegroundColor Green

    # Export to PFX
    Write-Host "Exporting certificate to PFX..." -ForegroundColor Cyan
    Export-PfxCertificate -Cert $cert -FilePath $pfxFile -Password $password | Out-Null

    Write-Host "`nCertificate created successfully!" -ForegroundColor Green
    Write-Host "Location: $pfxFile" -ForegroundColor Cyan
    Write-Host "`nIMPORTANT SECURITY NOTES:" -ForegroundColor Yellow
    Write-Host "1. This is a DEVELOPMENT certificate only" -ForegroundColor Yellow
    Write-Host "2. Windows will show 'Unknown Publisher' warnings" -ForegroundColor Yellow
    Write-Host "3. DO NOT commit the .pfx file to source control" -ForegroundColor Yellow
    Write-Host "4. For production, purchase a certificate from a trusted CA" -ForegroundColor Yellow
    
    Write-Host "`nTo build with signing:" -ForegroundColor Cyan
    Write-Host '  dotnet build -c Release /p:CertificatePassword="<your-password>"' -ForegroundColor White
    
    # Optional: Install certificate to Trusted Root (for local testing)
    $installRoot = Read-Host "`nInstall to Trusted Root Certification Authorities for local testing? (y/N)"
    if ($installRoot -eq 'y' -or $installRoot -eq 'Y') {
        Write-Host "Installing to Trusted Root (requires elevation)..." -ForegroundColor Cyan
        
        # Export public key
        $cerFile = Join-Path $projectDir "GuiGenericBuilder.cer"
        Export-Certificate -Cert $cert -FilePath $cerFile | Out-Null
        
        # Import to Trusted Root
        Import-Certificate -FilePath $cerFile -CertStoreLocation Cert:\CurrentUser\Root
        
        Remove-Item $cerFile
        Write-Host "Certificate installed to Trusted Root" -ForegroundColor Green
        Write-Host "WARNING: This should only be done on development machines!" -ForegroundColor Red
    }

} catch {
    Write-Host "Error creating certificate: $_" -ForegroundColor Red
    exit 1
}
