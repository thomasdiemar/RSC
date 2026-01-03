# RCS Thruster Optimisation

This solution explores attitude/translation control for reaction control systems (RCS) with multiple thrusters. It focuses on mapping a desired force/torque command to per‑thruster power levels bounded to `[0,1]`, and compares two solver implementations.

## NuGet Packages

### Latest: Unified Package (v2.0.0+)
As of v2.0.0, all core projects are consolidated into a **single `RCS` NuGet package**:

```xml
<PackageReference Include="RCS" Version="2.0.0" />
```

This single package includes:
- `LinearSolver` – solver interfaces and utilities
- `LinearSolver.Custom` – custom goal programming solver
- `LinearSolver.Custom.GoalProgramming` – goal programming implementation
- `RCS` – core domain types and engine optimizer
- `RCS.Custom` – custom solver wired optimization

**Breaking Change:** Previous separate packages (`LinearSolver` v1.0.0, `LinearSolver.Custom` v1.0.0) are **no longer published**. See [Migration Guide](#migration-guide) below.

### Solver-Specific Packages (Optional)
- `RCS.MSF` – Uses Microsoft Solver Foundation (separate package, requires additional setup)

## Projects
- `RCS` – core domain types (vectors, thrusters, engine, command) plus the generic `RcsEngineOptimiser` that builds the coefficient matrix and desired vector from a fleet of thrusters.
- `RCS.Custom` – optimiser wired to a custom linear solver that searches bounded thrust levels without MSF.
- `RCS.MSF` – optimiser wired to the Microsoft Solver Foundation goal-based solver (`MsfGoalLinearSolver`) as the reference implementation.
- `LinearSolver` – shared solver interface and progress type.
- `LinearSolver.Custom` / `LinearSolver.Custom.GoalProgramming` – concrete custom solver implementations (now bundled in RCS v2.0.0+).
- `LinearSolver.MSF` – MSF-based solver implementation (optional dependency).
- `RCS.Test` – MSTest suite that validates solver behaviour across several thruster layouts (12 thrusters, 3Fx, 3Opposite, 4Fx) and all force/torque directions, including soft-goal scenarios.

## How it works
1. `RcsEngineOptimiser` orders thrusters, builds a 6×N coefficient matrix (Fx/Fy/Fz/Tx/Ty/Tz rows), and derives desired row targets from an `RcsCommand`.
2. `RcsCommand` can request forces/torques and optionally allow non-commanded axes to be treated as soft goals (encoded as `double.NaN` in the desired vector).
3. The linear solver streams progress snapshots of per‑thruster outputs; the optimiser maps each snapshot to resultant force/torque.
4. Tests assert that the custom and MSF optimisers agree on outputs and resultant vectors for each scenario, across multiple thruster layouts.

## Running tests
```pwsh
dotnet test RCS.Test/RCS.Test.csproj
```

## Key scenarios in tests
- Max/Min force and torque in all axes for the 12‑thruster rig.
- Degenerate layouts (3Fx, 4Fx) to probe feasibility and soft-goal handling.
- Soft non-commanded axes (`AllowNonCommandedForces` / `AllowNonCommandedTorques`) to allow the solver to prefer, but not require, zero on unrequested axes.

## Migration Guide

### Upgrading from v1.0.0 to v2.0.0

**What Changed:**
- `LinearSolver` and `LinearSolver.Custom` are **no longer published as separate packages**
- All solver implementations consolidated into single `RCS` package
- Framework updated to .NET Framework 4.7.1
- MSF dependency removed from core RCS package

**For Consumers Using `RCS` v1.x:**
1. **No Action Required**: Simply update your package reference:
   ```xml
   <!-- Before -->
   <PackageReference Include="RCS" Version="1.0.0" />
   
   <!-- After -->
   <PackageReference Include="RCS" Version="2.0.0" />
   ```
2. All functionality remains the same; the v2.0.0 package is a drop-in replacement

**For Consumers Using `LinearSolver` or `LinearSolver.Custom` Separately:**
1. **Remove old references**:
   ```xml
   <!-- Remove these -->
   <PackageReference Include="LinearSolver" Version="1.0.0" />
   <PackageReference Include="LinearSolver.Custom" Version="1.0.0" />
   ```
2. **Replace with unified package**:
   ```xml
   <!-- Add this -->
   <PackageReference Include="RCS" Version="2.0.0" />
   ```
3. **Update usings** (if required): Most code should work unchanged since namespaces are preserved:
   - `LinearSolver` namespace still available
   - `LinearSolver.Custom` namespace still available
   - `RCS` namespace still available

**Breaking Changes:**
- If you directly referenced `LinearSolver` or `LinearSolver.Custom` NuGet packages, you must switch to the unified `RCS` package
- .NET Framework requirement: v4.7.1 or higher (updated from v4.7.2, but still compatible)

**No API Changes:**
- Class/interface signatures unchanged
- Namespace structure preserved
- All public APIs remain backward compatible
- Just bundled into a single package
