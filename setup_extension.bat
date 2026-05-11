@echo off
setlocal enabledelayedexpansion
title Cortex Speed — Fix Extension ID
cd /d "%~dp0"

echo.
echo ╔══════════════════════════════════════════════╗
echo ║   Cortex Speed — Extension Setup Helper     ║
echo ╚══════════════════════════════════════════════╝
echo.
echo STEP 1: Open Chrome and go to: chrome://extensions/
echo STEP 2: Make sure "Developer mode" is ON (top right)
echo STEP 3: Find "Cortex Speed Interceptor"
echo STEP 4: Copy the ID shown below the extension name
echo.
set /p EXT_ID="Paste the Extension ID here and press Enter: "

if "%EXT_ID%"=="" (
    echo ERROR: No ID entered!
    pause
    exit /b 1
)

echo.
echo Updating allowed_origins with ID: %EXT_ID%

:: Write the updated JSON file
(
echo {
echo   "name": "com.cortexspeed.host",
echo   "description": "Cortex Speed Native Messaging Host Bridge",
echo   "path": "%~dp0src\Infrastructure\CortexSpeed.Bridge\bin\Debug\net10.0-windows\CortexSpeed.Bridge.exe",
echo   "type": "stdio",
echo   "allowed_origins": [
echo     "chrome-extension://%EXT_ID%/"
echo   ]
echo }
) > "%~dp0src\Extension\com.cortexspeed.host.json"

echo.
echo Registering Chrome Native Messaging Host...
REG ADD "HKCU\Software\Google\Chrome\NativeMessagingHosts\com.cortexspeed.host" /ve /t REG_SZ /d "%~dp0src\Extension\com.cortexspeed.host.json" /f

echo.
echo Registering Edge Native Messaging Host...
REG ADD "HKCU\Software\Microsoft\Edge\NativeMessagingHosts\com.cortexspeed.host" /ve /t REG_SZ /d "%~dp0src\Extension\com.cortexspeed.host.json" /f

echo.
echo ╔══════════════════════════════════════════════╗
echo ║   DONE! Now:                                ║
echo ║   1. Go to chrome://extensions/             ║
echo ║   2. Click Reload on Cortex Speed extension ║
echo ║   3. Start Cortex Speed app (run.bat)       ║
echo ║   4. Try downloading a file!                ║
echo ╚══════════════════════════════════════════════╝
echo.
pause
