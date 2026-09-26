$ErrorActionPreference = 'Stop'

$arduinoCli = Join-Path $PSScriptRoot 'arduino-cli.exe'
$buildProperty = 'compiler.cpp.extra_flags=-D FLAG_STRING=1 -DFLAG_STRING_TEXT=\"alpha\040beta\" -D FLAG_NUMERIC_COUNT=7 -D FLAG_NUMERIC_GPIO=12'

Push-Location $PSScriptRoot
try {
    & $arduinoCli compile --fqbn 'esp32:esp32:esp32' --build-property $buildProperty --upload --port 'COM3'
    $exitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

exit $exitCode