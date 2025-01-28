@echo off
echo Building WPF Application in Debug configuration...

:: Restore dependencies
dotnet restore WpfApp4.sln

:: Build the project in Debug mode
dotnet build WpfApp4.sln --configuration Debug

if %ERRORLEVEL% EQU 0 (
    echo Debug build completed successfully.
) else (
    echo Debug build failed with error code %ERRORLEVEL%
    exit /b %ERRORLEVEL%
) 