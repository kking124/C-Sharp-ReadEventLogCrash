@echo off
REM Publish script for EventLogCrash
REM Builds in Release mode and creates an installer

set PROJECT=EventLogCrash
set CONFIG=Release

REM Clean and publish in Release mode for .NET 9 Windows
dotnet clean %PROJECT% -c %CONFIG%
dotnet publish %PROJECT% -c %CONFIG% --self-contained false --no-restore
if %errorlevel% neq 0 exit /b %errorlevel%

REM Build WiX installer (requires WiX Toolset installed and PATH configured)
set WXS=EventLogCrash.wxs
set WIXOBJ=bin/Publish/EventLogCrash.wixobj
set MSI=bin/Publish/EventLogCrashInstaller.msi
set WIXPATH=C:\Program Files (x86)\WiX Toolset v3.14\bin

if not exist "%WIXPATH%\candle.exe" (
	echo WiX Toolset not found at "%WIXPATH%". Please install WiX Toolset v3.14 and ensure the path is correct.
	exit /b 1
)

REM Change to project directory where the WiX file and source files are located
cd %PROJECT%

if exist "%WXS%" (
	"%WIXPATH%\candle.exe" "%WXS%" -out "%WIXOBJ%"
	if %errorlevel% neq 0 exit /b %errorlevel%
	"%WIXPATH%\light.exe" "%WIXOBJ%" -ext WixUIExtension -out "%MSI%"
	if %errorlevel% neq 0 exit /b %errorlevel%
	echo WiX installer built: "%CD%\%MSI%"
) else (
	echo WiX source file not found: "%WXS%"
	exit /b 1
)

cd ..
echo Build complete. MSI installer created: "%CD%\%PROJECT%\%MSI%"