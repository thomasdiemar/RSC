using LinearSolver;

namespace LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Model
{
    /// <summary>
    /// Represents lower and upper bounds for simplex variables/rows.
    /// </summary>
    public sealed class SolverBound
    {
        public SolverBound(Fraction lower, Fraction upper)
        {
            Lower = lower;
            Upper = upper;
        }

        public Fraction Lower { get; }

        public Fraction Upper { get; }

        public SolverBound Clone() => new SolverBound(Lower, Upper);
    }
}
