# Repository Guidelines

## Project Structure & Module Organization
- RCS/ – core domain models (vectors, thrusters, engine, optimiser) and command handling.
- LinearSolver/ – shared solver interfaces and progress primitives.
- LinearSolver.Custom/ and LinearSolver.MSF/ – concrete solver implementations (custom search vs. MSF goals).
- RCS.Test/ – MSTest suite covering multiple thruster layouts.

## Build, Test, and Development Commands
- dotnet build RCS.slnx – build all projects.
- dotnet test RCS.Test/RCS.Test.csproj – run the full MSTest suite.
- Typical loop: edit → dotnet test → review TestContext output for diagnostics.

## Coding Style & Naming Conventions
- C# with 4-space indentation; keep files ASCII.
- Public methods should have concise <summary> docs; prefer clear, descriptive names.
- Thruster factory helpers live in RCS.Test/ThrusterTestData.cs (name new ones CreateThrustersX).

## Testing Guidelines
- Framework: MSTest ([TestMethod]).
- Test naming: <Layout>_<Action>_MatchesMsf.
- Always run dotnet test RCS.Test/RCS.Test.csproj before PRs; outputs log thruster levels on failure.

## Commit & Pull Request Guidelines
- Use short, imperative commit messages (e.g., "Add granular solver progress").
- PRs should describe scope, link issues if any, and note test results (command above).

## Security & Configuration Tips
- Target .NET Framework 4.7.2; avoid adding new dependencies.
- Custom solver must not depend on MSF; MSF solver should guard against reading unsolved decisions for progress snapshots.

## Project Restrictions 
Following projects are restricted:
- LinearSolver
- LinearSolver.Custom
- LinearSolver.Custom.GoalProgramming

Restricted to:
System (basic types, math, text, collections)
System.Collections.Generic (lists, dictionaries, etc.)
System.Text (for StringBuilder, etc.)
System.Linq (for LINQ operations)

## Project Rules
Following projects 
- LinearSolver
- LinearSolver.Custom
- LinearSolver.Custom.GoalProgramming
must not use MFS or LinearSolver.MSF project

## LinearSolver.Custom.GoalProgramming
Must be implemented with Premptive Bounded Goal programming using Premptive Bounded Goal simplex and tablau.
Do not use the weights approach. Use the Preemptive approach:

Preemptive Approach
While the weights method balances goals using weights in the objective function, the preemptive method gives hierarchical priority to goals through iterative optimizations.
Here are the steps to the preemptive approach:
1. Run a regular linear programming optimization on your first goal — e.g., maximize profit
2. Save the objective value from that run
3. Run another regular linear programming on the next most important goal — e.g., add the objective value from the last run as a constraint
4. Repeat the process until you have gone through all goal metrics
Two important features of the preemptive method are (1) it prioritizes goals by rank and (2) the objective value of a higher importance goal cannot be decreased (because of the hard constraint) when optimizing lower priority goals. Let’s go over an example to build intuition.

see https://towardsdatascience.com/linear-programming-managing-multiple-targets-with-goal-programming/ 

## RcsCommand
From RcsCommand the goals and priorites can be extracted:
Positive DesiredForce (Fx,Fy,Fz) implies a maximize goal of that force, with higher priority.
Negative DesiredForce (Fx,Fy,Fz) implies a minimize goal of that force, with higher priority.
Positive DesiredTorque (Tx,Ty,Tz) implies a maximize goal of that torque, with higher priority.
Negative DesiredTorque (Tx,Ty,Tz) implies a minimize goal of that torque, with higher priority.
Neutral DesiredForce element (Fx=0,Fy=0,Fz=0) implies lowest priority and:
- If AllowNonCommandedForces == false, fixed constraint of 0 for that force element
- If AllowNonCommandedForces == true, no constraint for that force element, but a soft goal of minimizing that force element to zero
Neutral DesiredTorque element (Tx=0,Ty=0,Tz=0) implies lowest priority and:
- If AllowNonCommandedTorques == false, fixed constraint of 0 for that torque element 
- If AllowNonCommandedTorques == true, no constraint for that torque element, but a soft goal of minimizing that torque element to zero