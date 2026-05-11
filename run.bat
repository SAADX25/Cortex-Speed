@echo off
title Cortex Speed
cd /d "%~dp0"
echo Starting Cortex Speed...
dotnet run --project "src\Presentation\CortexSpeed.Presentation.WPF\CortexSpeed.Presentation.WPF.csproj"
pause
