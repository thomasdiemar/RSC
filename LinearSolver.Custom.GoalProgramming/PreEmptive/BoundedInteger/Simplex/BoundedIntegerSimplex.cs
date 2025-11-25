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

            var constraintRows = new List<int>();
            var constraintValues = new List<double>();
            var targetRows = new List<int>();
            var allRightHandSides = new double[tableau.RowCount];
            var allTolerances = new double[tableau.RowCount];

            for (int r = 0; r < tableau.RowCount; r++)
            {
                allRightHandSides[r] = (double)tableau.GetRightHandSide(r);
                allTolerances[r] = (double)tableau.RowGoals[r].Tolerance;
            }

            for (int r = 0; r < tableau.RowCount; r++)
            {
                var goal = tableau.RowGoals[r];
                if (goal.Priority != priorityLevel)
                {
                    continue;
                }

                bool isSoft = goal.Tolerance >= Fraction.MaxValue - Fraction.Epsilon;
                if (isSoft)
                {
                    continue;
                }

                var rhs = (double)goal.RightHandSide;
                if (goal.Sense == GoalSense.Equal)
                {
                    constraintRows.Add(r);
                    constraintValues.Add(rhs);
                    continue;
                }

                if (Math.Abs(rhs) < 1e-9)
                {
                    constraintRows.Add(r);
                    constraintValues.Add(0);
                }
                else
                {
                    targetRows.Add(r);
                }
            }

            var coefficients = ExtractCoefficients(tableau);
            var solution = new double[tableau.ColumnCount];
            var objectiveValue = new Fraction(0);

            if (targetRows.Count == 0 && constraintRows.Count == 0)
            {
                diagnostics.SetFinalStates(tableau.ColumnHeaders);
                return new SimplexResult(SimplexStatus.Optimal, objectiveValue, diagnostics, tableau.ColumnHeaders.Select(v => v.Clone()).ToList());
            }

            if (targetRows.Count > 0)
            {
                foreach (var targetRow in targetRows)
                {
                    constraintRows.Add(targetRow);
                    constraintValues.Add(allRightHandSides[targetRow]);
                }
            }

            if (constraintRows.Count > 0)
            {
                var feasible = SolveEnumerating(coefficients, constraintRows, constraintValues, targetRow: -1, targetSign: 0, allRightHandSides, allTolerances);
                if (feasible == null)
                {
                    return CreateResult(SimplexStatus.GoalViolation, objectiveValue, diagnostics, tableau);
                }

                solution = feasible;
                objectiveValue = new Fraction(0);
            }

            for (int i = 0; i < tableau.ColumnCount; i++)
            {
                tableau.ColumnHeaders[i].SetValue(ToFraction(solution[i]));
            }

            diagnostics.SetFinalStates(tableau.ColumnHeaders);
            var cloned = tableau.ColumnHeaders.Select(v => v.Clone()).ToList();
            return new SimplexResult(SimplexStatus.Optimal, objectiveValue, diagnostics, cloned);
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

        private static double[,] ExtractCoefficients(PreEmptiveIntegerTableau tableau)
        {
            var rows = tableau.RowCount;
            var cols = tableau.ColumnCount;
            var result = new double[rows, cols];

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    result[r, c] = (double)tableau.GetCoefficient(r, c);
                }
            }

            return result;
        }

        private static double[] SolveEnumerating(
            double[,] coefficients,
            IList<int> constraintRows,
            IList<double> constraintValues,
            int targetRow,
            int targetSign,
            double[] allRightHandSides,
            double[] allTolerances)
        {
            int cols = coefficients.GetLength(1);
            var independentRows = GetIndependentRows(coefficients, constraintRows);
            int constraintCount = independentRows.Count;
            var constraintValueMap = new Dictionary<int, double>();
            for (int i = 0; i < constraintRows.Count; i++)
            {
                constraintValueMap[constraintRows[i]] = constraintValues[i];
            }
            var independentValues = independentRows.Select(row => constraintValueMap[row]).ToArray();
            double bestObjective = double.NegativeInfinity;
            double bestPenalty = double.PositiveInfinity;
            double bestThrust = double.PositiveInfinity;
            double[] best = null;
            const double eps = 1e-6;

            if (constraintCount == 0)
            {
                foreach (var assignment in EnumerateBinary(cols))
                {
                    var candidate = assignment.Select(v => v ? 1.0 : 0.0).ToArray();
                    double objective = targetRow >= 0 ? EvaluateRow(coefficients, targetRow, candidate) * targetSign : 0;
                    double penalty = ComputePenalty(coefficients, candidate, targetRow, allRightHandSides, allTolerances);
                    double thrustSum = candidate.Sum();
                    if (objective > bestObjective + 1e-9 ||
                        (Math.Abs(objective - bestObjective) <= 1e-9 && penalty < bestPenalty - 1e-9) ||
                        (Math.Abs(objective - bestObjective) <= 1e-9 && Math.Abs(penalty - bestPenalty) <= 1e-9 && thrustSum < bestThrust - 1e-9))
                    {
                        bestObjective = objective;
                        bestPenalty = penalty;
                        bestThrust = thrustSum;
                        best = candidate;
                    }
                }

                return best;
            }

            var allIndices = Enumerable.Range(0, cols).ToArray();
            foreach (var basis in Choose(allIndices, constraintCount))
            {
                var basisSet = new HashSet<int>(basis);
                var nonBasis = allIndices.Where(i => !basisSet.Contains(i)).ToArray();

                foreach (var assignment in EnumerateBinary(nonBasis.Length))
                {
                    var rhs = new double[constraintCount];
                    for (int i = 0; i < constraintCount; i++)
                    {
                        double sum = 0;
                        for (int j = 0; j < nonBasis.Length; j++)
                        {
                            double value = assignment[j] ? 1.0 : 0.0;
                            sum += coefficients[independentRows[i], nonBasis[j]] * value;
                        }

                        rhs[i] = independentValues[i] - sum;
                    }

                    var matrix = new double[constraintCount, constraintCount];
                    for (int i = 0; i < constraintCount; i++)
                    {
                        for (int j = 0; j < constraintCount; j++)
                        {
                            matrix[i, j] = coefficients[independentRows[i], basis[j]];
                        }
                    }

                    var solved = SolveLinearSystem(matrix, rhs);
                    if (solved == null)
                    {
                        continue;
                    }

                    var candidate = new double[cols];
                    for (int i = 0; i < nonBasis.Length; i++)
                    {
                        candidate[nonBasis[i]] = assignment[i] ? 1.0 : 0.0;
                    }

                    bool inBounds = true;
                    for (int i = 0; i < constraintCount; i++)
                    {
                        double value = solved[i];
                        if (value < -eps || value > 1.0 + eps)
                        {
                            inBounds = false;
                            break;
                        }

                        candidate[basis[i]] = Math.Max(0, Math.Min(1.0, value));
                    }

                    if (!inBounds)
                    {
                        continue;
                    }

                    if (!ConstraintsSatisfied(coefficients, constraintRows, constraintValues, candidate, eps))
                    {
                        continue;
                    }

                    double objective = targetRow >= 0 ? EvaluateRow(coefficients, targetRow, candidate) * targetSign : 0;
                    double penalty = ComputePenalty(coefficients, candidate, targetRow, allRightHandSides, allTolerances);
                    double thrustSum = candidate.Sum();
                    if (objective > bestObjective + 1e-9 ||
                        (Math.Abs(objective - bestObjective) <= 1e-9 && penalty < bestPenalty - 1e-9) ||
                        (Math.Abs(objective - bestObjective) <= 1e-9 && Math.Abs(penalty - bestPenalty) <= 1e-9 && thrustSum < bestThrust - 1e-9))
                    {
                        bestObjective = objective;
                        bestPenalty = penalty;
                        bestThrust = thrustSum;
                        best = candidate;
                    }
                }
            }

            return best;
        }

        private static bool ConstraintsSatisfied(
            double[,] coefficients,
            IList<int> constraintRows,
            IList<double> constraintValues,
            double[] candidate,
            double eps)
        {
            for (int i = 0; i < constraintRows.Count; i++)
            {
                double value = EvaluateRow(coefficients, constraintRows[i], candidate);
                if (Math.Abs(value - constraintValues[i]) > eps)
                {
                    return false;
                }
            }

            return true;
        }

        private static IEnumerable<int[]> Choose(int[] source, int k)
        {
            var combo = new int[k];
            return ChooseInternal(source, k, 0, 0, combo);
        }

        private static List<int> GetIndependentRows(double[,] coefficients, IList<int> constraintRows)
        {
            var independent = new List<int>();
            if (constraintRows.Count == 0)
            {
                return independent;
            }

            int cols = coefficients.GetLength(1);
            var working = new double[constraintRows.Count, cols];
            var rowOrder = new int[constraintRows.Count];
            for (int i = 0; i < constraintRows.Count; i++)
            {
                rowOrder[i] = constraintRows[i];
                for (int c = 0; c < cols; c++)
                {
                    working[i, c] = coefficients[constraintRows[i], c];
                }
            }

            const double eps = 1e-9;
            int pivotRow = 0;
            for (int col = 0; col < cols && pivotRow < constraintRows.Count; col++)
            {
                int bestRow = pivotRow;
                double best = Math.Abs(working[bestRow, col]);
                for (int row = pivotRow + 1; row < constraintRows.Count; row++)
                {
                    double value = Math.Abs(working[row, col]);
                    if (value > best)
                    {
                        best = value;
                        bestRow = row;
                    }
                }

                if (best < eps)
                {
                    continue;
                }

                if (bestRow != pivotRow)
                {
                    SwapRows(working, bestRow, pivotRow);
                    int temp = rowOrder[bestRow];
                    rowOrder[bestRow] = rowOrder[pivotRow];
                    rowOrder[pivotRow] = temp;
                }

                independent.Add(rowOrder[pivotRow]);

                double pivot = working[pivotRow, col];
                for (int c = col; c < cols; c++)
                {
                    working[pivotRow, c] /= pivot;
                }

                for (int row = 0; row < constraintRows.Count; row++)
                {
                    if (row == pivotRow)
                    {
                        continue;
                    }

                    double factor = working[row, col];
                    if (Math.Abs(factor) < eps)
                    {
                        continue;
                    }

                    for (int c = col; c < cols; c++)
                    {
                        working[row, c] -= factor * working[pivotRow, c];
                    }
                }

                pivotRow++;
            }

            return independent;
        }

        private static IEnumerable<int[]> ChooseInternal(int[] source, int k, int start, int depth, int[] combo)
        {
            if (depth == k)
            {
                var snapshot = new int[k];
                Array.Copy(combo, snapshot, k);
                yield return snapshot;
                yield break;
            }

            for (int i = start; i <= source.Length - (k - depth); i++)
            {
                combo[depth] = source[i];
                foreach (var c in ChooseInternal(source, k, i + 1, depth + 1, combo))
                {
                    yield return c;
                }
            }
        }

        private static IEnumerable<bool[]> EnumerateBinary(int length)
        {
            var total = 1 << length;
            for (int mask = 0; mask < total; mask++)
            {
                var values = new bool[length];
                for (int i = 0; i < length; i++)
                {
                    values[i] = (mask & (1 << i)) != 0;
                }

                yield return values;
            }
        }

        private static double[] SolveLinearSystem(double[,] matrix, double[] rhs)
        {
            int n = rhs.Length;
            var augmented = new double[n, n + 1];
            var pivotColumn = new int[n];
            for (int i = 0; i < n; i++)
            {
                pivotColumn[i] = -1;
            }

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    augmented[i, j] = matrix[i, j];
                }
                augmented[i, n] = rhs[i];
            }

            const double eps = 1e-9;
            int rowPivotIndex = 0;
            for (int col = 0; col < n && rowPivotIndex < n; col++)
            {
                int pivot = rowPivotIndex;
                double best = Math.Abs(augmented[pivot, col]);
                for (int row = rowPivotIndex + 1; row < n; row++)
                {
                    double value = Math.Abs(augmented[row, col]);
                    if (value > best)
                    {
                        best = value;
                        pivot = row;
                    }
                }

                if (best < eps)
                {
                    continue;
                }

                if (pivot != rowPivotIndex)
                {
                    SwapRows(augmented, pivot, rowPivotIndex);
                }

                pivotColumn[rowPivotIndex] = col;

                double pivotValue = augmented[rowPivotIndex, col];
                for (int j = col; j <= n; j++)
                {
                    augmented[rowPivotIndex, j] /= pivotValue;
                }

                for (int row = 0; row < n; row++)
                {
                    if (row == rowPivotIndex)
                    {
                        continue;
                    }

                    double factor = augmented[row, col];
                    for (int j = col; j <= n; j++)
                    {
                        augmented[row, j] -= factor * augmented[rowPivotIndex, j];
                    }
                }

                rowPivotIndex++;
            }

            for (int row = 0; row < n; row++)
            {
                bool allZero = true;
                for (int col = 0; col < n; col++)
                {
                    if (Math.Abs(augmented[row, col]) > eps)
                    {
                        allZero = false;
                        break;
                    }
                }

                if (allZero && Math.Abs(augmented[row, n]) > eps)
                {
                    return null;
                }
            }

            var solution = new double[n];
            for (int row = 0; row < n; row++)
            {
                int col = pivotColumn[row];
                if (col >= 0)
                {
                    solution[col] = augmented[row, n];
                }
            }

            return solution;
        }

        private static void SwapRows(double[,] matrix, int rowA, int rowB)
        {
            if (rowA == rowB)
            {
                return;
            }

            int cols = matrix.GetLength(1);
            for (int c = 0; c < cols; c++)
            {
                double temp = matrix[rowA, c];
                matrix[rowA, c] = matrix[rowB, c];
                matrix[rowB, c] = temp;
            }
        }

        private static double EvaluateRow(double[,] coefficients, int row, double[] values)
        {
            double total = 0;
            int cols = coefficients.GetLength(1);
            for (int c = 0; c < cols; c++)
            {
                total += coefficients[row, c] * values[c];
            }

            return total;
        }

        private static double ComputePenalty(
            double[,] coefficients,
            double[] candidate,
            int targetRow,
            double[] allRightHandSides,
            double[] allTolerances)
        {
            double penalty = 0;
            int rows = coefficients.GetLength(0);
            const double softWeight = 0.01;
            const double hardWeight = 1000.0;
            for (int r = 0; r < rows; r++)
            {
                if (r == targetRow)
                {
                    continue;
                }

                double tolerance = r < allTolerances.Length ? allTolerances[r] : 0;
                bool isSoft = tolerance >= 1e6;
                double desired = r < allRightHandSides.Length ? allRightHandSides[r] : 0;
                double value = EvaluateRow(coefficients, r, candidate);
                double delta = Math.Abs(value - desired);
                if (isSoft)
                {
                    penalty += softWeight * delta;
                }
                else
                {
                    penalty += hardWeight * delta;
                }
            }

            return penalty;
        }

        private static Fraction ToFraction(double value)
        {
            const int scale = 1000000;
            return new Fraction((int)Math.Round(value * scale), scale);
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
