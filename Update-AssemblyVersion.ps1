# Script to update AssemblyVersion and AssemblyFileVersion from branch name
# Usage: .\Update-AssemblyVersion.ps1 -BranchName "version1.2.3"
# Or: .\Update-AssemblyVersion.ps1 (will use current git branch)

param(
    [string]$BranchName,
    [string]$ProjectRoot = (Get-Location).Path
)

# If no branch name provided, get from git
if (-not $BranchName) {
    $BranchName = git rev-parse --abbrev-ref HEAD
}

Write-Host "=== Assembly Version Updater ===" -ForegroundColor Cyan
Write-Host "Branch: $BranchName" -ForegroundColor Cyan

# Extract version from branch name (e.g., version1.2.3 -> 1.2.3.0)
$version = $BranchName -replace 'version', ''
$version = $version -replace '[^0-9.]', ''

# Ensure version has 4 parts (major.minor.build.revision)
$parts = $version.Split('.')
while ($parts.Count -lt 4) {
    $parts += "0"
}
$version = $parts[0..3] -join '.'

Write-Host "Extracted Version: $version" -ForegroundColor Green

# Find all AssemblyInfo.cs files (excluding obj and bin directories)
$assemblyInfoFiles = Get-ChildItem -Path $ProjectRoot -Recurse -Filter "AssemblyInfo.cs" | Where-Object { $_.FullName -notmatch '(obj|bin)' }

if ($assemblyInfoFiles.Count -eq 0) {
    Write-Host "No AssemblyInfo.cs files found!" -ForegroundColor Red
    exit 1
}

Write-Host "`nUpdating $($assemblyInfoFiles.Count) AssemblyInfo.cs file(s):" -ForegroundColor Yellow

foreach ($file in $assemblyInfoFiles) {
    Write-Host "  - $($file.FullName)" -ForegroundColor Gray
    
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    
    # Replace AssemblyVersion
    $content = $content -replace '\[assembly: AssemblyVersion\(".*?"\)\]', "[assembly: AssemblyVersion(`"$version`")]"
    
    # Replace AssemblyFileVersion
    $content = $content -replace '\[assembly: AssemblyFileVersion\(".*?"\)\]', "[assembly: AssemblyFileVersion(`"$version`")]"
    
    # Only write if content changed
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8
        Write-Host "    ✓ Updated" -ForegroundColor Green
    } else {
        Write-Host "    ℹ No changes needed" -ForegroundColor Yellow
    }
}

Write-Host "`nAssemblyVersion update complete!" -ForegroundColor Green
