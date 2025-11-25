namespace LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Simplex
{
    /// <summary>
    /// Represents the outcome of a simplex stage.
    /// </summary>
    public enum SimplexStatus
    {
        Optimal,
        GoalViolation,
        IntegerViolation,
        BoundViolation
    }
}
