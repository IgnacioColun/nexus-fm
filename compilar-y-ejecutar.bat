@echo off
chcp 65001 >nul
title Compilando e Iniciando NexusFM (KDE Dolphin Edition)...
echo ============================================================================
echo   NexusFM (Dolphin Edition) - Compilacion e Integracion con Windows
echo ============================================================================

set CSC="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set WPF_DIR=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF

if not exist %CSC% (
    echo [Error] No se encontro el compilador nativo de .NET en %CSC%
    pause
    exit /b 1
)

:: 1. Generar el icono multi-resolucion (256/64/48/32/16px) de NexusFM
echo Generando icono multi-resolucion de carpetas NexusFM...
%CSC% /nologo /target:exe /out:"%~dp0src-csharp\IconGen.exe" /r:System.Drawing.dll "%~dp0src-csharp\IconGen.cs"
"%~dp0src-csharp\IconGen.exe" "%~dp0assets\nexus-folder.ico"
del /q "%~dp0src-csharp\IconGen.exe" 2>nul

:: 2. Cerrar instancia previa si esta abierta para poder actualizar el .exe
taskkill /f /im NexusFM.exe >nul 2>&1

:: 3. Compilar NexusFM.exe con soporte de extraccion de iconos de juegos/softwares
echo Compilando NexusFM.exe ...
%CSC% /nologo /target:winexe /win32icon:"%~dp0assets\nexus-folder.ico" /out:"%~dp0NexusFM.exe" ^
  /r:"%WPF_DIR%\PresentationFramework.dll" ^
  /r:"%WPF_DIR%\PresentationCore.dll" ^
  /r:"%WPF_DIR%\WindowsBase.dll" ^
  /r:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Xaml.dll" ^
  /r:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Drawing.dll" ^
  "%~dp0src-csharp\NexusFM.cs"

if %ERRORLEVEL% NEQ 0 (
    echo [Error] Fallo la compilacion.
    pause
    exit /b %ERRORLEVEL%
)

echo [OK] NexusFM.exe compilado exitosamente!
start "" "%~dp0NexusFM.exe"
