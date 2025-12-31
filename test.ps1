#Requires -Version 5.0
<#
.SYNOPSIS
Test script for RCS solution that loads environment variables from .env file.

.DESCRIPTION
This script loads the .env file, sets environment variables, and runs all tests
for the RCS solution.

.EXAMPLE
.\test.ps1

.EXAMPLE
.\test.ps1 -Configuration Debug

.EXAMPLE
.\test.ps1 -Verbose

.PARAMETER Configuration
The build configuration (Debug or Release). Default: Debug

.PARAMETER Verbose
Show detailed output during test execution
#>

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    
    [switch]$Verbose
)

# Function to load .env file
function Load-EnvFile {
    param(
        [string]$EnvFilePath = '.env'
    )
    
    if (-not (Test-Path $EnvFilePath)) {
        Write-Warning ".env file not found at $EnvFilePath"
        return
    }
    
    Write-Host "Loading environment variables from $EnvFilePath..." -ForegroundColor Cyan
    
    $count = 0
    Get-Content $EnvFilePath | Where-Object { $_.Trim() -and -not $_.Trim().StartsWith('#') } | ForEach-Object {
        if ($_ -match '^\s*([^=]+)=(.*)$') {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()
            
            # Remove quotes if present
            $value = $value -replace '^"(.*)"$', '$1'
            $value = $value -replace "^'(.*)'`$", '$1'
            
            [Environment]::SetEnvironmentVariable($key, $value, 'Process')
            
            if ($Verbose) {
                Write-Host "  ✓ Loaded: $key" -ForegroundColor Green
            }
            $count++
        }
    }
    
    Write-Host "Loaded $count environment variables." -ForegroundColor Green
}

# Function to run tests and parse results
function Run-Tests {
    param(
        [string]$TestProject,
        [string]$Configuration
    )
    
    Write-Host "Running tests for $TestProject..." -ForegroundColor Cyan
    
    $testArgs = @(
        'test',
        $TestProject,
        '-c', $Configuration
    )
    
    # Run tests and capture output
    $output = & dotnet @testArgs 2>&1
    
    # Display output
    $output | ForEach-Object { Write-Host $_ }
    
    # Check exit code
    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed with exit code $LASTEXITCODE"
    }
    
    # Extract test summary
    $summary = $output | Select-String "Test summary"
    if ($summary) {
        Write-Host "`n$summary" -ForegroundColor Green
    }
}

# Main script
try {
    Write-Host "`n=== RCS Test Script ===" -ForegroundColor Cyan
    Write-Host "Configuration: $Configuration" -ForegroundColor Cyan
    
    # Load environment variables
    Load-EnvFile
    
    # Restore if not already done
    Write-Host "`nRestoring dependencies..." -ForegroundColor Cyan
    dotnet restore RCS.slnx | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Restore failed" }
    Write-Host "Dependencies restored." -ForegroundColor Green
    
    # Build solution
    Write-Host "`nBuilding solution..." -ForegroundColor Cyan
    dotnet build RCS.slnx -c $Configuration --no-restore | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
    Write-Host "Build completed." -ForegroundColor Green
    
    # Run tests
    Write-Host "`n" -ForegroundColor Cyan
    Run-Tests -TestProject "RCS.Test/RCS.Test.csproj" -Configuration $Configuration
    
    Write-Host "`n✅ All tests completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "`n❌ Test execution failed: $_" -ForegroundColor Red
    exit 1
}
