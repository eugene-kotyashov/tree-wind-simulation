Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

Write-Host "Building and Running WPF Application in Debug mode..."

# Build the project first in Debug mode
& .\build-debug.bat

if ($LASTEXITCODE -ne 0) {
    Write-Host "Debug build failed, cannot run application."
    exit $LASTEXITCODE
}

# Run the application from the Debug build
Write-Host "Starting application in Debug mode..."
$exePath = "WpfApp4\bin\Debug\net8.0-windows\WpfApp4.exe"

if (Test-Path $exePath) {
    Start-Process -FilePath $exePath -WorkingDirectory (Split-Path $exePath)
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Application started successfully in Debug mode."
    } else {
        Write-Host "Failed to start application. Error code: $LASTEXITCODE"
        exit $LASTEXITCODE
    }
} else {
    Write-Host "Error: Application executable not found at: $exePath"
    exit 1
} 