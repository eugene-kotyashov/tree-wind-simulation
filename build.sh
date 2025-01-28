#!/bin/bash

echo "Building WPF Application..."

# Restore dependencies
dotnet restore WpfApp4.sln

# Build the project
dotnet build WpfApp4.sln --configuration Release

if [ $? -eq 0 ]; then
    echo "Build completed successfully."
else
    echo "Build failed with error code $?"
    exit 1
fi 