@echo off
setlocal
pushd "%~dp0"
"arduino-cli.exe" compile --fqbn esp32:esp32:esp32 --build-property "compiler.cpp.extra_flags=-D FLAG_STRING=1 -DFLAG_STRING_TEXT=\"alpha\040beta\" -D FLAG_NUMERIC_COUNT=7 -D FLAG_NUMERIC_GPIO=12"
set "BUILD_EXIT_CODE=%ERRORLEVEL%"
popd
exit /b %BUILD_EXIT_CODE%