using System;
using System.Collections.Generic;
using System.Linq;
using LinearSolver.Custom.GoalProgramming.Mathematics;

namespace LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Model
{
    /// <summary>
    /// Represents the tableau consumed by the bounded preemptive integer simplex.
    /// Holds the current coefficients, RHS values, and metadata such as column headers and bound states.
    /// </summary>
    public sealed class PreEmptiveIntegerTableau
    {
        private readonly List<BoundedIntegerVariable> columnHeaders;
        private readonly List<GoalDefinition> rowGoals;
        private readonly List<TableauRowState> rowStates;
        private readonly Fraction[,] coefficients;
        private readonly Fraction[] rightHandSide;

        public PreEmptiveIntegerTableau(
            IList<BoundedIntegerVariable> variables,
            IList<GoalDefinition> goals,
            IList<TableauRowState> states)
        {
            if (variables == null || variables.Count == 0)
            {
                throw new ArgumentException("At least one variable is required to build a tableau.", nameof(variables));
            }

            if (goals == null || goals.Count == 0)
            {
                throw new ArgumentException("At least one goal row is required.", nameof(goals));
            }

            if (states == null || states.Count != goals.Count)
            {
                throw new ArgumentException("Row states must align with goal rows.", nameof(states));
            }

            columnHeaders = variables.Select(v => v.Clone()).ToList();
            rowGoals = goals.Select(g => g.Clone()).ToList();
            rowStates = states.Select(s => s.Clone()).ToList();
            coefficients = new Fraction[rowGoals.Count, columnHeaders.Count];
            rightHandSide = new Fraction[rowGoals.Count];
        }

        public IReadOnlyList<BoundedIntegerVariable> ColumnHeaders => columnHeaders;

        public IReadOnlyList<GoalDefinition> RowGoals => rowGoals;

        public IReadOnlyList<TableauRowState> RowStates => rowStates;

        public int RowCount => rowGoals.Count;

        public int ColumnCount => columnHeaders.Count;

        public int EnteringColumnIndex { get; set; } = -1;

        public int KeyRow { get; set; } = -1;

        public Fraction Delta { get; set; } = new Fraction(0);

        public int ObjectiveRowIndex { get; set; }

        public int CurrentPriority { get; set; }

        public int RightHandSideColumnIndex => -1;

        public void ApplyBoundOverride(int columnIndex, Fraction lowerBound, Fraction upperBound)
        {
            ValidateColumn(columnIndex);
            columnHeaders[columnIndex] = columnHeaders[columnIndex].WithBounds(lowerBound, upperBound);
        }

        public void ApplySolution(IReadOnlyList<BoundedIntegerVariable> solution)
        {
            if (solution == null || solution.Count != columnHeaders.Count)
            {
                throw new ArgumentException("Solution vector does not match column count.", nameof(solution));
            }

            for (int i = 0; i < columnHeaders.Count; i++)
            {
                columnHeaders[i].SetValue(solution[i].Value);
            }
        }

        public Fraction GetCoefficient(int row, int column)
        {
            ValidateRow(row);
            ValidateColumn(column);
            return coefficients[row, column];
        }

        public void SetCoefficient(int row, int column, Fraction value)
        {
            ValidateRow(row);
            ValidateColumn(column);
            coefficients[row, column] = value;
        }

        public Fraction GetRightHandSide(int row)
        {
            ValidateRow(row);
            return rightHandSide[row];
        }

        public void SetRightHandSide(int row, Fraction value)
        {
            ValidateRow(row);
            rightHandSide[row] = value;
        }

        /// <summary>
        /// Locks the specified goal so that lower priorities respect the achieved optimum (Agoritm_Rcs.tex §5).
        /// </summary>
        public void LockGoalValue(int goalIndex, Fraction achievedValue)
        {
            ValidateRow(goalIndex);
            rightHandSide[goalIndex] = achievedValue;
            rowGoals[goalIndex] = rowGoals[goalIndex].CreateLock(achievedValue);
        }

        public PreEmptiveIntegerTableau Clone()
        {
            var clone = new PreEmptiveIntegerTableau(
                columnHeaders.Select(v => v.Clone()).ToList(),
                rowGoals.Select(g => g.Clone()).ToList(),
                rowStates.Select(s => s.Clone()).ToList());

            clone.EnteringColumnIndex = EnteringColumnIndex;
            clone.KeyRow = KeyRow;
            clone.Delta = Delta;
            clone.ObjectiveRowIndex = ObjectiveRowIndex;
            clone.CurrentPriority = CurrentPriority;

            for (int r = 0; r < RowCount; r++)
            {
                for (int c = 0; c < ColumnCount; c++)
                {
                    clone.SetCoefficient(r, c, coefficients[r, c]);
                }

                clone.SetRightHandSide(r, rightHandSide[r]);
                clone.rowStates[r].Value = rowStates[r].Value;
                clone.rowStates[r].BoundState = rowStates[r].BoundState;
            }

            return clone;
        }

        private void ValidateRow(int row)
        {
            if (row < 0 || row >= RowCount)
            {
                throw new ArgumentOutOfRangeException(nameof(row));
            }
        }

        private void ValidateColumn(int column)
        {
            if (column < 0 || column >= ColumnCount)
            {
                throw new ArgumentOutOfRangeException(nameof(column));
            }
        }
    }
}
