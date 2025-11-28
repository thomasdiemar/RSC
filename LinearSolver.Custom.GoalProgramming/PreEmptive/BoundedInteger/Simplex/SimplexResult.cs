using System;
using System.Collections.Generic;
using System.Linq;
using LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Diagnostics;
using LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Model;
using LinearSolver;

namespace LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Simplex
{
    /// <summary>
    /// Captures the outcome of a simplex stage together with diagnostics and the solution vector.
    /// </summary>
    public sealed class SimplexResult
    {
        public SimplexResult(SimplexStatus status, Fraction objectiveValue, SimplexDiagnostics diagnostics, IEnumerable<BoundedIntegerVariable> solution)
        {
            Status = status;
            ObjectiveValue = objectiveValue;
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            Solution = solution?.Select(v => v.Clone()).ToList() ?? throw new ArgumentNullException(nameof(solution));
        }

        public SimplexStatus Status { get; }

        public Fraction ObjectiveValue { get; }

        public SimplexDiagnostics Diagnostics { get; }

        public IReadOnlyList<BoundedIntegerVariable> Solution { get; }
    }
}
