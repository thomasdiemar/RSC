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
