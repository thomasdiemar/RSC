# Script to update AssemblyVersion, AssemblyFileVersion, and csproj <Version> from branch name
# Usage: .\Update-AssemblyVersion.ps1 -BranchName "version1.2.3"
# Or: .\Update-AssemblyVersion.ps1 (will use current git branch)
#
# This script updates:
# 1. AssemblyVersion and AssemblyFileVersion in AssemblyInfo.cs files (4-part version)
# 2. <Version> property in all 5 core project csproj files (3-part version)
# 3. Validates version consistency across all updated files

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
$assemblyVersion = $parts[0..3] -join '.'

# For csproj, use 3-part version (major.minor.patch)
$csprojVersion = $parts[0..2] -join '.'

Write-Host "Extracted AssemblyVersion (4-part): $assemblyVersion" -ForegroundColor Green
Write-Host "Extracted csproj Version (3-part): $csprojVersion" -ForegroundColor Green

# ============================================================================
# PHASE 3A: Update AssemblyInfo.cs files
# ============================================================================

# Find all AssemblyInfo.cs files (excluding obj and bin directories)
$assemblyInfoFiles = Get-ChildItem -Path $ProjectRoot -Recurse -Filter "AssemblyInfo.cs" | Where-Object { $_.FullName -notmatch '(obj|bin)' }

if ($assemblyInfoFiles.Count -eq 0) {
    Write-Host "No AssemblyInfo.cs files found!" -ForegroundColor Red
    exit 1
}

Write-Host "`nUpdating $($assemblyInfoFiles.Count) AssemblyInfo.cs file(s):" -ForegroundColor Yellow

$assemblyInfoUpdatedCount = 0
foreach ($file in $assemblyInfoFiles) {
    Write-Host "  - $($file.FullName)" -ForegroundColor Gray
    
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    
    # Replace AssemblyVersion
    $content = $content -replace '\[assembly: AssemblyVersion\(".*?"\)\]', "[assembly: AssemblyVersion(`"$assemblyVersion`")]"
    
    # Replace AssemblyFileVersion
    $content = $content -replace '\[assembly: AssemblyFileVersion\(".*?"\)\]', "[assembly: AssemblyFileVersion(`"$assemblyVersion`")]"
    
    # Only write if content changed
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8
        Write-Host "    ✓ Updated" -ForegroundColor Green
        $assemblyInfoUpdatedCount++
    } else {
        Write-Host "    ℹ No changes needed" -ForegroundColor Yellow
    }
}

# ============================================================================
# PHASE 3B: Update csproj <Version> properties for 5 core projects
# ============================================================================

$csprojFiles = @(
    "LinearSolver\LinearSolver.csproj",
    "LinearSolver.Custom.GoalProgramming\LinearSolver.Custom.GoalProgramming.csproj",
    "RCS\RCS.csproj"
)

Write-Host "`nUpdating $($csprojFiles.Count) csproj <Version> property/properties:" -ForegroundColor Yellow

$csprojUpdatedCount = 0
$csprojVersions = @()

foreach ($relPath in $csprojFiles) {
    $csprojPath = Join-Path $ProjectRoot $relPath
    
    if (-not (Test-Path $csprojPath)) {
        Write-Host "  - $relPath - NOT FOUND" -ForegroundColor Red
        continue
    }
    
    Write-Host "  - $relPath" -ForegroundColor Gray
    
    try {
        # Load the XML
        [xml]$csproj = Get-Content $csprojPath -Encoding UTF8
        $updated = $false
        
        # Check if <Version> element exists in PropertyGroup
        $propertyGroups = $csproj.Project.PropertyGroup
        $versionElements = @()
        
        foreach ($propGroup in $propertyGroups) {
            if ($propGroup.Version) {
                $versionElements += $propGroup
            }
        }
        
        # Update or create <Version> element in first PropertyGroup
        if ($versionElements.Count -gt 0) {
            # Update existing Version elements
            foreach ($propGroup in $versionElements) {
                if ($propGroup.Version -ne $csprojVersion) {
                    $propGroup.Version = $csprojVersion
                    $updated = $true
                }
            }
        } else {
            # Add new Version element to first PropertyGroup
            if ($propertyGroups.Count -gt 0) {
                $firstPropGroup = $propertyGroups[0]
                if (-not $firstPropGroup.Version) {
                    $versionNode = $csproj.CreateElement("Version")
                    $versionNode.InnerText = $csprojVersion
                    $firstPropGroup.AppendChild($versionNode) | Out-Null
                    $updated = $true
                }
            }
        }
        
        # Save if updated
        if ($updated) {
            $csproj.Save($csprojPath)
            Write-Host "    ✓ Updated to $csprojVersion" -ForegroundColor Green
            $csprojUpdatedCount++
            $csprojVersions += @{ Path = $relPath; Version = $csprojVersion }
        } else {
            Write-Host "    ℹ Already set to $csprojVersion" -ForegroundColor Yellow
            $csprojVersions += @{ Path = $relPath; Version = $csprojVersion }
        }
    } catch {
        Write-Host "    ✗ Error: $_" -ForegroundColor Red
    }
}

# ============================================================================
# PHASE 3C: Update nuspec <version> property
# ============================================================================

$nuspecPath = Join-Path $ProjectRoot "RCS\RCS.nuspec"

if (Test-Path $nuspecPath) {
    Write-Host "`nUpdating RCS.nuspec version:" -ForegroundColor Yellow
    
    try {
        [xml]$nuspec = Get-Content $nuspecPath -Encoding UTF8
        $currentVersion = $nuspec.package.metadata.version
        
        if ($currentVersion -ne $csprojVersion) {
            $nuspec.package.metadata.version = $csprojVersion
            $nuspec.Save($nuspecPath)
            Write-Host "  - RCS.nuspec" -ForegroundColor Gray
            Write-Host "    ✓ Updated from $currentVersion to $csprojVersion" -ForegroundColor Green
        } else {
            Write-Host "  - RCS.nuspec" -ForegroundColor Gray
            Write-Host "    ℹ Already set to $csprojVersion" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "  ✗ Error updating nuspec: $_" -ForegroundColor Red
    }
}

# ============================================================================
# PHASE 3D: Validate version consistency
# ============================================================================

Write-Host "`nValidating version consistency across projects:" -ForegroundColor Yellow

$consistencyErrors = 0
foreach ($proj in $csprojVersions) {
    if ($proj.Version -ne $csprojVersion) {
        Write-Host "  ✗ $($proj.Path): Expected $csprojVersion, but found $($proj.Version)" -ForegroundColor Red
        $consistencyErrors++
    } else {
        Write-Host "  ✓ $($proj.Path): $($proj.Version)" -ForegroundColor Green
    }
}

if ($consistencyErrors -gt 0) {
    Write-Host "`n✗ Version consistency check FAILED with $consistencyErrors error(s)" -ForegroundColor Red
    exit 1
}

# ============================================================================
# Summary
# ============================================================================

Write-Host "`n=== Update Summary ===" -ForegroundColor Cyan
Write-Host "AssemblyInfo.cs files updated: $assemblyInfoUpdatedCount/$($assemblyInfoFiles.Count)" -ForegroundColor Green
Write-Host "csproj files updated: $csprojUpdatedCount/$($csprojFiles.Count)" -ForegroundColor Green
Write-Host "Version consistency check: ✓ PASSED" -ForegroundColor Green
Write-Host "`nAssemblyVersion update complete!" -ForegroundColor Green
