# RCS Thruster Optimisation

This solution explores attitude/translation control for reaction control systems (RCS) with multiple thrusters. It focuses on mapping a desired force/torque command to per‑thruster power levels bounded to `[0,1]`, and compares two solver implementations.

## Projects
- `RCS` – core domain types (vectors, thrusters, engine, command) plus the generic `RcsEngineOptimiser` that builds the coefficient matrix and desired vector from a fleet of thrusters.
- `RCS.Custom` – optimiser wired to a custom goal solver (`CustomGoalLinearSolver`) that searches bounded thrust levels without MSF.
- `RCS.MSF` – optimiser wired to the Microsoft Solver Foundation goal-based solver (`MsfGoalLinearSolver`) as the reference implementation.
- `LinearSolver` – shared solver interface and progress type.
- `LinearSolver.Custom` / `LinearSolver.MSF` – concrete solver implementations for the custom and MSF paths.
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
