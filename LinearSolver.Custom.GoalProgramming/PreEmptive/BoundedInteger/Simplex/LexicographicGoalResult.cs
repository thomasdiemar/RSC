using LinearSolver.Custom.GoalProgramming.Mathematics;
using System.Collections.Generic;
using System.Linq;

namespace LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Simplex
{
    public sealed class LexicographicGoalResult
    {
        public LexicographicGoalResult(SimplexStatus status, IEnumerable<SimplexResult> stageResults, IReadOnlyDictionary<int, Fraction> stageObjectives)
        {
            Status = status;
            StageResults = stageResults?.ToList() ?? new List<SimplexResult>();
            StageObjectives = stageObjectives ?? new Dictionary<int, Fraction>();
        }

        public SimplexStatus Status { get; }

        public IReadOnlyList<SimplexResult> StageResults { get; }

        public IReadOnlyDictionary<int, Fraction> StageObjectives { get; }
    }
}
