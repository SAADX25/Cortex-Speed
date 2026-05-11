@echo off
setlocal enabledelayedexpansion
title Cortex Speed — Extension Setup Helper
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
echo Extension ID registered: %EXT_ID%

echo.
echo ╔══════════════════════════════════════════════╗
echo ║   DONE! Now:                                ║
echo ║   1. Start the Cortex Speed app (run.bat)   ║
echo ║   2. Try downloading a file!                ║
echo ║                                              ║
echo ║   Communication flow:                       ║
echo ║   Browser → localhost:19256 (HTTP)          ║
echo ║                                              ║
echo ╚══════════════════════════════════════════════╝
echo.
pause
