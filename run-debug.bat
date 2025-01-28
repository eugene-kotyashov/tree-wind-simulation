@echo off
echo Building and Running WPF Application in Debug mode...

:: Build the project first in Debug mode
call build-debug.bat

if %ERRORLEVEL% NEQ 0 (
    echo Debug build failed, cannot run application.
    exit /b %ERRORLEVEL%
)

:: Run the application from the Debug build
echo Starting application in Debug mode...
Start-Process -WorkingDirectory "WpfApp4\bin\Debug\net8.0-windows" -FilePath "WpfApp4.exe"

if %ERRORLEVEL% EQU 0 (
    echo Application started successfully in Debug mode.
) else (
    echo Failed to start application. Error code: %ERRORLEVEL%
    exit /b %ERRORLEVEL%
) 