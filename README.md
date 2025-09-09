# EventLogCrash

A simple .NET 9.0 WinForms application that checks the Windows Event Log to determine if the application crashed or exited successfully during its last run.

## Features
- Reads Windows Event Log for Event IDs 1000/1001 to detect crashes.
- Displays last exit status and error stack trace (if any).
- Two buttons: "Exit Successfully" and "Crash App".

## Prerequisites
- Windows 10/11
- .NET 9.0 SDK

## Build Instructions

1. Open a terminal in the project root directory.
2. Run the following command to restore dependencies and build:
   ```powershell
   dotnet restore ; dotnet build
   ```
3. To run the application:
   ```powershell
   dotnet run --project EventLogCrash
   ```

## Usage
- On launch, the app displays the last exit status.
- Click "Exit Successfully" to exit and log a successful run.
- Click "Crash App" to trigger an unhandled exception (Windows will log Event IDs 1000/1001).

## Notes
- You may need administrator privileges to read the Windows Event Log.
- The app uses best practices for WinForms and event log access.

## Release History

### 1.1.0
- move crash check out of UI thread
- add icon

### 1.0.1
- update UI
- add .NET Error Display

### 1.0.0
- initial release
- 2 buttons for successfull and fail fast crash
- display error if last exit was error