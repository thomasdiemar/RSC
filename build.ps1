#Requires -Version 5.0
<#
.SYNOPSIS
Build script for RCS solution that loads environment variables from .env file.

.DESCRIPTION
This script loads the .env file, sets environment variables, and builds the RCS solution
in Release configuration.

.EXAMPLE
.\build.ps1

.EXAMPLE
.\build.ps1 -Configuration Debug

.PARAMETER Configuration
The build configuration (Debug or Release). Default: Release
#>

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
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
            Write-Host "  ✓ Loaded: $key" -ForegroundColor Green
            $count++
        }
    }
    
    Write-Host "Loaded $count environment variables." -ForegroundColor Green
}

# Main script
try {
    Write-Host "`n=== RCS Build Script ===" -ForegroundColor Cyan
    
    # Load environment variables
    Load-EnvFile
    
    # Restore dependencies
    Write-Host "`n[1/3] Restoring dependencies..." -ForegroundColor Cyan
    dotnet restore RCS.slnx
    if ($LASTEXITCODE -ne 0) { throw "Restore failed" }
    
    # Build solution
    Write-Host "`n[2/3] Building solution ($Configuration)..." -ForegroundColor Cyan
    dotnet build RCS.slnx -c $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
    
    # Run tests
    Write-Host "`n[3/3] Running tests..." -ForegroundColor Cyan
    dotnet test RCS.Test/RCS.Test.csproj -c $Configuration --no-build
    if ($LASTEXITCODE -ne 0) { throw "Tests failed" }
    
    Write-Host "`n✅ Build completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "`n❌ Build failed: $_" -ForegroundColor Red
    exit 1
}
