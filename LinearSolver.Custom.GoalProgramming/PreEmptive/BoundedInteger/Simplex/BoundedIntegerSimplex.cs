using System;
using System.Collections.Generic;
using System.Linq;
using LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Diagnostics;
using LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Model;
using LinearSolver.Custom.GoalProgramming.Mathematics;

namespace LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Simplex
{
    /// <summary>
    /// Performs bounded simplex evaluations for a single priority level.
    /// This is a scaffold for the full bounded preemptive integer algorithm; it currently validates feasibility
    /// and checks whether the current tableau satisfies the target goals.
    /// </summary>
    public sealed class BoundedIntegerSimplex
    {
        private static readonly Fraction Zero = new Fraction(0);
        private readonly IRatioTest ratioTest;

        public BoundedIntegerSimplex()
            : this(new BoundedAugmentedRatioTest())
        {
        }

        public BoundedIntegerSimplex(IRatioTest ratioTest)
        {
            this.ratioTest = ratioTest ?? new BoundedAugmentedRatioTest();
        }
        public int MaxIterations { get; set; } = 1000;

        public SimplexResult SolvePriority(PreEmptiveIntegerTableau tableau, int priorityLevel)
        {
            if (tableau == null)
            {
                throw new ArgumentNullException(nameof(tableau));
            }

            if (priorityLevel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(priorityLevel));
            }

            tableau.CurrentPriority = priorityLevel;
            if (!HasRowsForPriority(tableau, priorityLevel))
            {
                throw new ArgumentException("No goal rows exist for the requested priority.", nameof(priorityLevel));
            }
            var diagnostics = new SimplexDiagnostics(priorityLevel);
            ValidateVariables(tableau);

            var iterations = 0;
            while (iterations < MaxIterations)
            {
                var evaluation = EvaluateGoals(tableau, diagnostics);
                if (evaluation.AllSatisfied)
                {
                    return CreateResult(SimplexStatus.Optimal, evaluation.ObjectiveValue, diagnostics, tableau);
                }

                var enteringColumn = SelectEnteringColumn(tableau, evaluation);
                if (enteringColumn < 0)
                {
                    return CreateResult(SimplexStatus.GoalViolation, evaluation.ObjectiveValue, diagnostics, tableau);
                }

                tableau.EnteringColumnIndex = enteringColumn;
                var pivot = ratioTest.Evaluate(tableau);
                diagnostics.RecordPivot();
                diagnostics.RecordPivotDetail(tableau.EnteringColumnIndex, tableau.KeyRow, tableau.Delta);

                if (pivot == PivotType.DegeneratePivot)
                {
                    return CreateResult(SimplexStatus.GoalViolation, evaluation.ObjectiveValue, diagnostics, tableau);
                }

                if (pivot == PivotType.PreEmptiveBoundHit)
                {
                    ApplyBoundHit(tableau);
                }
                else
                {
                    ApplyRowPivot(tableau);
                }

                iterations++;
            }

            return CreateResult(SimplexStatus.GoalViolation, Zero, diagnostics, tableau);
        }

        internal Fraction EvaluateRow(PreEmptiveIntegerTableau tableau, int rowIndex)
        {
            tableau = tableau ?? throw new ArgumentNullException(nameof(tableau));
            if (rowIndex < 0 || rowIndex >= tableau.RowCount)
            {
                throw new ArgumentOutOfRangeException(nameof(rowIndex));
            }

            var total = Zero;
            for (int c = 0; c < tableau.ColumnCount; c++)
            {
                var coefficient = tableau.GetCoefficient(rowIndex, c);
                if (coefficient == Zero)
                {
                    continue;
                }

                total += coefficient * tableau.ColumnHeaders[c].Value;
            }

            return total;
        }

        private GoalEvaluationResult EvaluateGoals(PreEmptiveIntegerTableau tableau, SimplexDiagnostics diagnostics)
        {
            Fraction cumulativeObjective = Zero;
            var violatingRow = -1;
            var adjustment = GoalAdjustment.None;
            foreach (var row in EnumerateRows(tableau, tableau.CurrentPriority))
            {
                diagnostics.RecordRowEvaluation();
                var actualValue = EvaluateRow(tableau, row.RowIndex);
                cumulativeObjective += actualValue;

                if (violatingRow < 0)
                {
                    var neededAdjustment = DetermineAdjustment(row.Goal, actualValue);
                    if (neededAdjustment != GoalAdjustment.None)
                    {
                        violatingRow = row.RowIndex;
                        adjustment = neededAdjustment;
                    }
                }
            }

            var allSatisfied = violatingRow < 0;
            return new GoalEvaluationResult(allSatisfied, cumulativeObjective, violatingRow, adjustment);
        }

        private static GoalAdjustment DetermineAdjustment(GoalDefinition goal, Fraction actualValue)
        {
            var tolerance = goal.Tolerance;
            switch (goal.Sense)
            {
                case GoalSense.Maximize:
                    return actualValue + tolerance >= goal.RightHandSide
                        ? GoalAdjustment.None
                        : GoalAdjustment.Increase;
                case GoalSense.Minimize:
                    return actualValue - tolerance <= goal.RightHandSide
                        ? GoalAdjustment.None
                        : GoalAdjustment.Decrease;
                case GoalSense.Equal:
                    if (Fraction.Abs(actualValue - goal.RightHandSide) <= tolerance)
                    {
                        return GoalAdjustment.None;
                    }

                    return actualValue < goal.RightHandSide ? GoalAdjustment.Increase : GoalAdjustment.Decrease;
                default:
                    return GoalAdjustment.None;
            }
        }

        private static IEnumerable<(GoalDefinition Goal, int RowIndex)> EnumerateRows(PreEmptiveIntegerTableau tableau, int priorityLevel)
        {
            for (int i = 0; i < tableau.RowCount; i++)
            {
                var goal = tableau.RowGoals[i];
                if (goal.Priority == priorityLevel)
                {
                    yield return (goal, i);
                }
            }
        }

        private static bool HasRowsForPriority(PreEmptiveIntegerTableau tableau, int priorityLevel)
        {
            foreach (var goal in tableau.RowGoals)
            {
                if (goal.Priority == priorityLevel)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateVariables(PreEmptiveIntegerTableau tableau)
        {
            foreach (var variable in tableau.ColumnHeaders)
            {
                if (variable.Value < variable.LowerBound || variable.Value > variable.UpperBound)
                {
                    throw new InvalidOperationException($"Variable {variable.Name} is outside of its bounds.");
                }
            }
        }

        private int SelectEnteringColumn(PreEmptiveIntegerTableau tableau, GoalEvaluationResult evaluation)
        {
            if (evaluation.ViolatingRowIndex < 0 || evaluation.RequiredAdjustment == GoalAdjustment.None)
            {
                return -1;
            }

            var bestColumn = -1;
            var bestMagnitude = Zero;
            for (int column = 0; column < tableau.ColumnCount; column++)
            {
                var variable = tableau.ColumnHeaders[column];
                var coefficient = tableau.GetCoefficient(evaluation.ViolatingRowIndex, column);
                if (coefficient == Zero)
                {
                    continue;
                }

                if (!CanImproveVariable(variable, coefficient, evaluation.RequiredAdjustment))
                {
                    continue;
                }

                var magnitude = Fraction.Abs(coefficient);
                if (bestColumn < 0 || magnitude > bestMagnitude)
                {
                    bestColumn = column;
                    bestMagnitude = magnitude;
                }
            }

            return bestColumn;
        }

        private static void ApplyBoundHit(PreEmptiveIntegerTableau tableau)
        {
            var variable = tableau.ColumnHeaders[tableau.EnteringColumnIndex];
            if (variable.BoundState == SolverBoundState.Lower)
            {
                variable.SetValue(variable.UpperBound);
            }
            else if (variable.BoundState == SolverBoundState.Upper)
            {
                variable.SetValue(variable.LowerBound);
            }
        }

        private static void ApplyRowPivot(PreEmptiveIntegerTableau tableau)
        {
            var entering = tableau.ColumnHeaders[tableau.EnteringColumnIndex];
            var delta = tableau.Delta;
            var direction = entering.BoundState == SolverBoundState.Lower ? new Fraction(1) : new Fraction(-1);
            entering.SetValue(entering.Value + direction * delta);

            var rowState = tableau.RowStates[tableau.KeyRow];
            var target = rowState.PendingPivotState;
            if (rowState.Bound != null)
            {
                var newValue = target == SolverBoundState.Lower ? rowState.Bound.Lower : rowState.Bound.Upper;
                rowState.Value = newValue;
                rowState.BoundState = target;
                tableau.SetRightHandSide(tableau.KeyRow, newValue);
                rowState.SetPendingPivotState(SolverBoundState.Basic);
            }
        }

        private static bool CanImproveVariable(BoundedIntegerVariable variable, Fraction coefficient, GoalAdjustment adjustment)
        {
            if (variable == null)
            {
                return false;
            }

            if (variable.BoundState == SolverBoundState.Basic)
            {
                return false;
            }

            switch (adjustment)
            {
                case GoalAdjustment.Increase:
                    if (coefficient > Zero)
                    {
                        return variable.BoundState == SolverBoundState.Lower && variable.Value < variable.UpperBound;
                    }

                    if (coefficient < Zero)
                    {
                        return variable.BoundState == SolverBoundState.Upper && variable.Value > variable.LowerBound;
                    }

                    break;
                case GoalAdjustment.Decrease:
                    if (coefficient > Zero)
                    {
                        return variable.BoundState == SolverBoundState.Upper && variable.Value > variable.LowerBound;
                    }

                    if (coefficient < Zero)
                    {
                        return variable.BoundState == SolverBoundState.Lower && variable.Value < variable.UpperBound;
                    }

                    break;
            }

            return false;
        }

        private static SimplexResult CreateResult(SimplexStatus status, Fraction objective, SimplexDiagnostics diagnostics, PreEmptiveIntegerTableau tableau)
        {
            diagnostics.SetFinalStates(tableau.ColumnHeaders);
            var solution = tableau.ColumnHeaders.Select(v => v.Clone()).ToList();
            return new SimplexResult(status, objective, diagnostics, solution);
        }

        private sealed class GoalEvaluationResult
        {
            public GoalEvaluationResult(bool allSatisfied, Fraction objectiveValue, int violatingRowIndex, GoalAdjustment requiredAdjustment)
            {
                AllSatisfied = allSatisfied;
                ObjectiveValue = objectiveValue;
                ViolatingRowIndex = violatingRowIndex;
                RequiredAdjustment = requiredAdjustment;
            }

            public bool AllSatisfied { get; }

            public Fraction ObjectiveValue { get; }

            public int ViolatingRowIndex { get; }

            public GoalAdjustment RequiredAdjustment { get; }
        }

        private enum GoalAdjustment
        {
            None,
            Increase,
            Decrease
        }
    }
}
