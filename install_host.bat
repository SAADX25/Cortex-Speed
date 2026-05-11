@echo off
setlocal

:: Define the host name and the absolute path to the JSON manifest
set "HOST_NAME=com.cortexspeed.host"
set "MANIFEST_PATH=%~dp0src\Extension\com.cortexspeed.host.json"

echo ========================================================
echo  Cortex Speed - Native Messaging Host Installer v2.0
echo ========================================================
echo.
echo  Manifest: %MANIFEST_PATH%
echo.

echo [1/2] Registering Chrome Native Messaging Host...
REG ADD "HKCU\Software\Google\Chrome\NativeMessagingHosts\%HOST_NAME%" /ve /t REG_SZ /d "%MANIFEST_PATH%" /f

echo.
echo [2/2] Registering Edge Native Messaging Host...
REG ADD "HKCU\Software\Microsoft\Edge\NativeMessagingHosts\%HOST_NAME%" /ve /t REG_SZ /d "%MANIFEST_PATH%" /f

echo.
echo ========================================================
echo  Installation Complete!
echo  
echo  NEXT STEPS:
echo  1. Build the Bridge:  dotnet build src\Infrastructure\CortexSpeed.Bridge
echo  2. Reload the extension in chrome://extensions
echo  3. Start Cortex Speed WPF app
echo  4. Try downloading a file!
echo ========================================================
pause
