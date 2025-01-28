@echo off
echo Building WPF Application...
:: Restore dependencies
dotnet restore WpfApp4.sln

:: Build the project
dotnet build WpfApp4.sln --configuration Release

if %ERRORLEVEL% EQU 0 (
    echo Build completed successfully.
) else (
    echo Build failed with error code %ERRORLEVEL%
    exit /b %ERRORLEVEL%
) 