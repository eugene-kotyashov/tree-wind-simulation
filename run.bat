@echo off
echo Building and Running WPF Application...

:: Build the project first
call build.bat

if %ERRORLEVEL% NEQ 0 (
    echo Build failed, cannot run application.
    exit /b %ERRORLEVEL%
)

:: Run the application from the Release build
echo Starting application...
start "" "WpfApp4\bin\Release\net8.0-windows\WpfApp4.exe"

if %ERRORLEVEL% EQU 0 (
    echo Application started successfully.
) else (
    echo Failed to start application. Error code: %ERRORLEVEL%
    exit /b %ERRORLEVEL%
) 