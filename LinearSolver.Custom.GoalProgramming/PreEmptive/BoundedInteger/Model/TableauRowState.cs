using LinearSolver.Custom.GoalProgramming.Mathematics;

namespace LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Model
{
    /// <summary>
    /// Captures the current state of a basic variable represented by a tableau row.
    /// </summary>
    public sealed class TableauRowState
    {
        public TableauRowState(string name, int priority, Fraction value, SolverBound bound)
        {
            Name = name;
            Priority = priority;
            Value = value;
            Bound = bound;
            BoundState = SolverBoundState.Basic;
            PendingPivotState = SolverBoundState.Basic;
        }

        public string Name { get; }

        public int Priority { get; }

        public Fraction Value { get; set; }

        public SolverBound Bound { get; }

        public SolverBoundState BoundState { get; set; }

        public SolverBoundState PendingPivotState { get; private set; }

        public TableauRowState Clone()
        {
            var clone = new TableauRowState(Name, Priority, Value, Bound?.Clone());
            clone.BoundState = BoundState;
            clone.PendingPivotState = PendingPivotState;
            return clone;
        }

        public void SetPendingPivotState(SolverBoundState state)
        {
            PendingPivotState = state;
        }
    }
}
