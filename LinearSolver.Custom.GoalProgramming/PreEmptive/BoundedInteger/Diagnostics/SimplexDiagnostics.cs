using System;
using System.Collections.Generic;
using LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Model;
using LinearSolver;

namespace LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Diagnostics
{
    /// <summary>
    /// Collects lightweight counters for each simplex stage so regressions can be diagnosed.
    /// </summary>
    /// <remarks>
    /// Mirrors the instrumentation described in Agoritm_Rcs.tex Appendix B to aid debugger parity with the domain work.
    /// </remarks>
    public sealed class SimplexDiagnostics
    {
        public SimplexDiagnostics(int priorityLevel)
        {
            if (priorityLevel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(priorityLevel));
            }

            PriorityLevel = priorityLevel;
        }

        public int PriorityLevel { get; }

        public int EvaluatedRows { get; private set; }

        public int PivotCount { get; private set; }

        public int RecordedBranches { get; private set; }

        public int MaxBranchDepth { get; private set; }

        public int LastBranchVariable { get; private set; } = -1;

        public int FirstBranchVariable { get; private set; } = -1;

        public Fraction LastUpperBound { get; private set; } = Fraction.MinValue;

        private readonly List<BranchTrace> branchDetails = new List<BranchTrace>();
        private readonly List<PivotTrace> pivotDetails = new List<PivotTrace>();
        private readonly List<Fraction> incumbentHistory = new List<Fraction>();
        private readonly Dictionary<string, SolverBoundState> finalStates = new Dictionary<string, SolverBoundState>();

        public IReadOnlyList<BranchTrace> BranchDetails => branchDetails;

        public IReadOnlyList<PivotTrace> PivotDetails => pivotDetails;

        public IReadOnlyDictionary<string, SolverBoundState> FinalVariableStates => finalStates;

        public IReadOnlyList<Fraction> IncumbentHistory => incumbentHistory;

        public void RecordRowEvaluation() => EvaluatedRows++;

        public void RecordPivot() => PivotCount++;

        public void RecordBranch(int variableIndex, int depth)
        {
            RecordedBranches++;
            if (FirstBranchVariable < 0)
            {
                FirstBranchVariable = variableIndex;
            }
            LastBranchVariable = variableIndex;
            if (depth > MaxBranchDepth)
            {
                MaxBranchDepth = depth;
            }
        }

        public void RecordBranchDetail(string variableName, Fraction lowerBound, Fraction upperBound, int depth, Fraction delta)
        {
            branchDetails.Add(new BranchTrace(variableName, lowerBound, upperBound, depth, delta));
        }

        public void RecordPivotDetail(int enteringColumn, int leavingRow, Fraction stepSize)
        {
            pivotDetails.Add(new PivotTrace(enteringColumn, leavingRow, stepSize));
        }

        public void RecordUpperBound(Fraction bound)
        {
            if (bound > LastUpperBound)
            {
                LastUpperBound = bound;
            }
        }

        public void SetFinalStates(IReadOnlyList<BoundedIntegerVariable> solution)
        {
            finalStates.Clear();
            if (solution == null)
            {
                return;
            }

            foreach (var variable in solution)
            {
                finalStates[variable.Name] = variable.BoundState;
            }
        }

        public void RecordIncumbent(Fraction objective)
        {
            incumbentHistory.Add(objective);
        }

        public void Merge(SimplexDiagnostics other)
        {
            if (other == null)
            {
                return;
            }

            EvaluatedRows += other.EvaluatedRows;
            PivotCount += other.PivotCount;
            RecordedBranches += other.RecordedBranches;
            if (other.MaxBranchDepth > MaxBranchDepth)
            {
                MaxBranchDepth = other.MaxBranchDepth;
            }

            if (FirstBranchVariable < 0 && other.FirstBranchVariable >= 0)
            {
                FirstBranchVariable = other.FirstBranchVariable;
            }

            if (other.LastBranchVariable >= 0)
            {
                LastBranchVariable = other.LastBranchVariable;
            }

            if (other.LastUpperBound > LastUpperBound)
            {
                LastUpperBound = other.LastUpperBound;
            }

            branchDetails.AddRange(other.BranchDetails);
            pivotDetails.AddRange(other.PivotDetails);
            incumbentHistory.AddRange(other.IncumbentHistory);
        }
    }

    public sealed class BranchTrace
    {
        public BranchTrace(string variable, Fraction lower, Fraction upper, int depth, Fraction delta)
        {
            Variable = variable;
            LowerBound = lower;
            UpperBound = upper;
            Depth = depth;
            Delta = delta;
        }

        public string Variable { get; }

        public Fraction LowerBound { get; }

        public Fraction UpperBound { get; }

        public int Depth { get; }

        public Fraction Delta { get; }
    }

    public sealed class PivotTrace
    {
        public PivotTrace(int enteringColumn, int leavingRow, Fraction stepSize)
        {
            EnteringColumn = enteringColumn;
            LeavingRow = leavingRow;
            StepSize = stepSize;
        }

        public int EnteringColumn { get; }

        public int LeavingRow { get; }

        public Fraction StepSize { get; }
    }
}
