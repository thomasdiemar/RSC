# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2026-01-03

### 🎯 Major Release: Unified NuGet Package

This release consolidates five projects into a **single `RCS` NuGet package**, simplifying dependency management while maintaining full API compatibility.

### 📦 Changed

#### Package Structure
- **BREAKING**: Separate `LinearSolver` v1.0.0 and `LinearSolver.Custom` v1.0.0 NuGet packages **no longer published**
- **NEW**: Single `RCS` v2.0.0 package now includes all five core projects:
  - `LinearSolver.dll` (interfaces and utilities)
  - `LinearSolver.Custom.dll` (custom goal solver)
  - `LinearSolver.Custom.GoalProgramming.dll` (goal programming implementation)
  - `RCS.dll` (core domain and engine optimizer)
  - `RCS.Custom.dll` (custom solver wired optimizer)
- All assemblies packaged to `lib/net471` framework directory
- No external dependencies (MSF remains optional via `RCS.MSF` package)

#### Framework
- Updated from .NET Framework 4.7.2 to 4.7.1
- All projects target single framework version for consistency

#### Dependencies
- Removed unused `Microsoft.Solver.Foundation` reference from core `RCS` project
- `RCS.MSF` remains optional for advanced solver functionality
- No new runtime dependencies introduced

### 🔧 Infrastructure

#### Build & Versioning
- Enhanced `Update-AssemblyVersion.ps1` script to:
  - Update `<Version>` property in all 5 core project csproj files
  - Generate 4-part assembly versions (e.g., 2.0.0.0)
  - Generate 3-part semantic versions for NuGet (e.g., 2.0.0)
  - Validate version consistency across all projects
  - Extract versions from branch names (e.g., `version2.0.1`)

#### CI/CD Workflow
- Updated `publish-nuget.yml` GitHub Actions workflow to:
  - Use enhanced versioning script for consistent updates
  - Build all 5 core projects for unified package
  - Create single `RCS.*.nupkg` instead of 3 separate packages
  - Validate package contents (must contain 5 DLLs)
  - Push unified package to Azure DevOps feed

### 📝 Migration Guide

**For consumers upgrading from v1.x:**

1. Remove old package references:
   ```xml
   <!-- Remove -->
   <PackageReference Include="LinearSolver" Version="1.0.0" />
   <PackageReference Include="LinearSolver.Custom" Version="1.0.0" />
   ```

2. Add unified package reference:
   ```xml
   <!-- Add -->
   <PackageReference Include="RCS" Version="2.0.0" />
   ```

3. **No API changes required** - all namespaces and public APIs remain unchanged

### ✅ What Remains Unchanged

- All public APIs, classes, and interfaces (backward compatible)
- Namespace structure (`LinearSolver`, `LinearSolver.Custom`, `RCS`, `RCS.Custom`)
- Test coverage and validation suite
- Thruster layout scenarios and solver comparison
- Soft-goal handling and constraint management

### 🧪 Testing

All phases of consolidation tested and verified:

#### Phase 1: Project Configuration
- ✅ All 5 projects updated to framework v4.7.1
- ✅ All 5 projects set to PackageId "RCS"
- ✅ All 5 projects set to Version "2.0.0"
- ✅ MSF reference removed from core RCS
- ✅ All projects build successfully

#### Phase 2: NuSpec Consolidation
- ✅ RCS.nuspec updated with all 5 project assemblies
- ✅ 10 binaries packaged (5 DLLs + 5 PDBs) to lib/net471
- ✅ External dependencies removed
- ✅ Separate LinearSolver.nuspec and LinearSolver.Custom.nuspec deleted

#### Phase 3: Versioning Script Enhancement
- ✅ Update-AssemblyVersion.ps1 now updates csproj `<Version>` properties
- ✅ Converts 4-part assembly versions to 3-part semantic versions
- ✅ Validates version consistency across all 5 projects
- ✅ Tested with version2.0.1 branch name

#### Phase 4: GitHub Actions Workflow
- ✅ Workflow updated to use enhanced versioning script
- ✅ Build step includes all 5 core projects
- ✅ Pack step creates single unified package
- ✅ Validation confirms exactly 1 RCS*.nupkg with 5 DLLs

#### Phase 5: Local Testing
- ✅ All 5 core projects compile in Release configuration
- ✅ All 10 binaries verified present
- ✅ RCS.nuspec structure validated
- ✅ Version update script tested and reverted successfully

#### Phase 6: Documentation
- ✅ README.md updated with unified package info
- ✅ Migration guide provided for v1.x → v2.0.0 upgrade
- ✅ Breaking changes clearly documented

### 🔗 Related Issues

- Issue #24: Update TargetFrameworkVersion to v4.7.1
- Issue #25: Set PackageId to RCS across all projects
- Issue #26: Remove MSF dependency from core RCS
- Issue #27: Consolidate NuSpec files
- Issue #28: Delete separate LinearSolver*.nuspec files
- Issue #29: Enhance Update-AssemblyVersion.ps1 for csproj updates
- Issue #30: Update GitHub Actions workflow for unified package
- Issue #31-32: Testing verification
- Issue #33: Documentation and migration guide

### 📋 Deployment Instructions

**To create a release:**

1. Create a version branch:
   ```powershell
   git checkout -b version2.0.1
   git push origin version2.0.1
   ```

2. GitHub Actions workflow automatically:
   - Extracts version from branch name
   - Runs Update-AssemblyVersion.ps1
   - Builds all 5 core projects
   - Creates single RCS.2.0.1.nupkg
   - Publishes to Azure DevOps feed

**Notes:**
- Branch naming convention: `version<major>.<minor>.<patch>`
- Example: `version2.0.0`, `version2.0.1`, `version2.1.0`
- Workflow only triggers on branches matching `version*` pattern

### ⚠️ Known Issues

- ThrusterVisualizer (net9.0 SDK project) has unrelated assembly attribute duplication issues
- RCS.Test skipped in CI/CD (depends on RCS.MSF with MSF requirements)

## [1.0.0] - Earlier

See git history for v1.0.0 and earlier releases.
