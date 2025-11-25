using LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Model;

namespace LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Simplex
{
    public interface IRatioTest
    {
        PivotType Evaluate(PreEmptiveIntegerTableau tableau);
    }
}
