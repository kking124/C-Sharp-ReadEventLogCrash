@echo off
REM Build script for EventLogCrash
REM Usage: build.bat

dotnet restore
if %errorlevel% neq 0 exit /b %errorlevel%
dotnet build
if %errorlevel% neq 0 exit /b %errorlevel%
