namespace LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Simplex
{
    /// <summary>
    /// Represents streaming progress emitted by the lexicographic solver.
    /// </summary>
    public class Progress
    {
        public string Info { get; set; }

        public LexicographicGoalResult Result { get; set; }

        public bool Done { get; set; }
    }
}
