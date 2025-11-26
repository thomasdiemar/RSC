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
                var rhs = (double)goal.RightHandSide;

                if (isSoft)
                {
                    continue;
                }

                if (Math.Abs(rhs) < 1e-9)
                {
                    constraintRows.Add(r);
                    constraintValues.Add(0);
                    continue;
                }

                // Non-zero hard row becomes the current objective target.
                targetRows.Add(r);
            }

            var coefficients = ExtractCoefficients(tableau);
            var solution = new double[tableau.ColumnCount];
            var objectiveValue = new Fraction(0);
            var originalConstraintRows = new List<int>(constraintRows);
            var originalConstraintValues = new List<double>(constraintValues);

            if (targetRows.Count == 0 && constraintRows.Count == 0)
            {
                diagnostics.SetFinalStates(tableau.ColumnHeaders);
                return new SimplexResult(SimplexStatus.Optimal, objectiveValue, diagnostics, tableau.ColumnHeaders.Select(v => v.Clone()).ToList());
            }

            int targetRow = targetRows.Count > 0 ? targetRows[0] : -1;
            int targetSign = 1;
            if (targetRow >= 0 && allRightHandSides[targetRow] < 0)
            {
                targetSign = -1;
            }

            // Remove target row from constraint list if present.
            if (targetRow >= 0)
            {
                int idx = constraintRows.IndexOf(targetRow);
                if (idx >= 0)
                {
                    constraintRows.RemoveAt(idx);
                    constraintValues.RemoveAt(idx);
                }
            }

            var feasible = SolveEnumerating(coefficients, constraintRows, constraintValues, targetRow, targetSign, allRightHandSides, allTolerances);
            if (feasible == null)
            {
                return CreateResult(SimplexStatus.GoalViolation, objectiveValue, diagnostics, tableau);
            }

            solution = feasible;
            if (targetRow >= 0)
            {
                var achieved = EvaluateRow(coefficients, targetRow, solution);
                objectiveValue = ToFraction(targetSign * achieved);
            }

            // Phase 2: lock achieved target and minimise soft penalties/thrust.
            if (targetRow >= 0)
            {
                var lockedConstraints = new List<int>(originalConstraintRows) { targetRow };
                var lockedValues = new List<double>(originalConstraintValues) { (double)objectiveValue / targetSign };
                var refined = SolveEnumerating(coefficients, lockedConstraints, lockedValues, targetRow: -1, targetSign: 1, allRightHandSides: allRightHandSides, allTolerances: allTolerances);
                if (refined != null)
                {
                    solution = refined;
                }
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
            var softRows = GetSoftRows(allTolerances);
            var solvingRows = GetIndependentRows(coefficients, constraintRows, 1e-9);
            var solvingValues = new List<double>(solvingRows.Count);
            foreach (var row in solvingRows)
            {
                int originalIndex = constraintRows.IndexOf(row);
                solvingValues.Add(originalIndex >= 0 ? constraintValues[originalIndex] : 0);
            }

            int eqCount = solvingRows.Count;
            const double eps = 1e-9;

            if (eqCount > cols)
            {
                return null;
            }

            double bestObjective = double.NegativeInfinity;
            double bestPenalty = double.PositiveInfinity;
            double bestThrust = double.PositiveInfinity;
            double[] best = null;

            // No hard constraints: brute-force 0/1 corners.
            if (eqCount == 0)
            {
                int maxMask = 1 << cols;
                var candidate = new double[cols];
                for (int mask = 0; mask < maxMask; mask++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        candidate[c] = ((mask >> c) & 1) == 1 ? 1.0 : 0.0;
                    }

                    double objective = targetRow >= 0 ? EvaluateRow(coefficients, targetRow, candidate) * targetSign : 0;
                    double penalty = 0;
                    if (softRows != null && softRows.Count > 0)
                    {
                        foreach (var r in softRows)
                        {
                            penalty += Math.Abs(EvaluateRow(coefficients, r, candidate));
                        }
                    }

                    double thrust = candidate.Sum();
                    if (objective > bestObjective + eps ||
                        (Math.Abs(objective - bestObjective) <= eps && penalty < bestPenalty - eps) ||
                        (Math.Abs(objective - bestObjective) <= eps && Math.Abs(penalty - bestPenalty) <= eps && thrust < bestThrust - eps))
                    {
                        bestObjective = objective;
                        bestPenalty = penalty;
                        bestThrust = thrust;
                        best = (double[])candidate.Clone();
                    }
                }

                return best;
            }

            var allIndices = Enumerable.Range(0, cols).ToArray();
            foreach (var basis in Choose(allIndices, eqCount))
            {
                var basisSet = new HashSet<int>(basis);
                var nonBasis = allIndices.Where(i => !basisSet.Contains(i)).ToArray();
                int nonCount = nonBasis.Length;
                int maxMask = 1 << nonCount;

                for (int mask = 0; mask < maxMask; mask++)
                {
                    var candidate = new double[cols];
                    for (int i = 0; i < nonCount; i++)
                    {
                        candidate[nonBasis[i]] = ((mask >> i) & 1) == 1 ? 1.0 : 0.0;
                    }

                    var matrix = new double[eqCount, eqCount];
                    var rhs = new double[eqCount];
                    for (int r = 0; r < eqCount; r++)
                    {
                        double sum = 0;
                        for (int i = 0; i < nonCount; i++)
                        {
                            sum += coefficients[solvingRows[r], nonBasis[i]] * candidate[nonBasis[i]];
                        }

                        rhs[r] = solvingValues[r] - sum;
                        for (int c = 0; c < eqCount; c++)
                        {
                            matrix[r, c] = coefficients[solvingRows[r], basis[c]];
                        }
                    }

                    var solved = SolveLinearSystem(matrix, rhs, eps);
                    if (solved == null)
                    {
                        continue;
                    }

                    bool inBounds = true;
                    for (int i = 0; i < eqCount; i++)
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

                    bool constraintsOk = true;
                    for (int r = 0; r < constraintRows.Count; r++)
                    {
                        double actual = EvaluateRow(coefficients, constraintRows[r], candidate);
                        if (Math.Abs(actual - constraintValues[r]) > 1e-6)
                        {
                            constraintsOk = false;
                            break;
                        }
                    }

                    if (!constraintsOk)
                    {
                        continue;
                    }

                    double objective = targetRow >= 0 ? EvaluateRow(coefficients, targetRow, candidate) * targetSign : 0;
                    double penalty = 0;
                    if (softRows != null && softRows.Count > 0)
                    {
                        foreach (var r in softRows)
                        {
                            penalty += Math.Abs(EvaluateRow(coefficients, r, candidate));
                        }
                    }

                    double thrust = candidate.Sum();
                    if (objective > bestObjective + eps ||
                        (Math.Abs(objective - bestObjective) <= eps && penalty < bestPenalty - eps) ||
                        (Math.Abs(objective - bestObjective) <= eps && Math.Abs(penalty - bestPenalty) <= eps && thrust < bestThrust - eps))
                    {
                        bestObjective = objective;
                        bestPenalty = penalty;
                        bestThrust = thrust;
                        best = candidate;
                    }
                }
            }

            return best;
        }

        private static double[] SolveLinearSystem(double[,] matrix, double[] rhs, double eps)
        {
            int n = rhs.Length;
            var augmented = new double[n, n + 1];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    augmented[i, j] = matrix[i, j];
                }
                augmented[i, n] = rhs[i];
            }

            int pivotRow = 0;
            for (int col = 0; col < n && pivotRow < n; col++)
            {
                int bestRow = pivotRow;
                double best = Math.Abs(augmented[bestRow, col]);
                for (int r = pivotRow + 1; r < n; r++)
                {
                    double val = Math.Abs(augmented[r, col]);
                    if (val > best)
                    {
                        best = val;
                        bestRow = r;
                    }
                }

                if (best < eps)
                {
                    continue;
                }

                if (bestRow != pivotRow)
                {
                    SwapRows(augmented, bestRow, pivotRow);
                }

                double pivot = augmented[pivotRow, col];
                for (int c = col; c <= n; c++)
                {
                    augmented[pivotRow, c] /= pivot;
                }

                for (int r = 0; r < n; r++)
                {
                    if (r == pivotRow)
                    {
                        continue;
                    }

                    double factor = augmented[r, col];
                    if (Math.Abs(factor) < eps)
                    {
                        continue;
                    }

                    for (int c = col; c <= n; c++)
                    {
                        augmented[r, c] -= factor * augmented[pivotRow, c];
                    }
                }

                pivotRow++;
            }

            // Detect inconsistency.
            for (int r = 0; r < n; r++)
            {
                bool allZero = true;
                for (int c = 0; c < n; c++)
                {
                    if (Math.Abs(augmented[r, c]) > eps)
                    {
                        allZero = false;
                        break;
                    }
                }

                if (allZero && Math.Abs(augmented[r, n]) > eps)
                {
                    return null;
                }
            }

            var solution = new double[n];
            for (int r = 0; r < n; r++)
            {
                int pivotCol = -1;
                for (int c = 0; c < n; c++)
                {
                    if (Math.Abs(augmented[r, c]) > eps)
                    {
                        pivotCol = c;
                        break;
                    }
                }

                if (pivotCol >= 0)
                {
                    solution[pivotCol] = augmented[r, n];
                }
            }

            return solution;
        }

        private static IEnumerable<int[]> Choose(int[] source, int k)
        {
            var combo = new int[k];
            return ChooseInternal(source, k, 0, 0, combo);
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
                foreach (var result in ChooseInternal(source, k, i + 1, depth + 1, combo))
                {
                    yield return result;
                }
            }
        }

        private static List<int> GetIndependentRows(double[,] coefficients, IList<int> constraintRows, double eps)
        {
            var independent = new List<int>();
            if (constraintRows == null || constraintRows.Count == 0)
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

            int pivotRow = 0;
            for (int col = 0; col < cols && pivotRow < constraintRows.Count; col++)
            {
                int bestRow = pivotRow;
                double best = Math.Abs(working[bestRow, col]);
                for (int r = pivotRow + 1; r < constraintRows.Count; r++)
                {
                    double val = Math.Abs(working[r, col]);
                    if (val > best)
                    {
                        best = val;
                        bestRow = r;
                    }
                }

                if (best < eps)
                {
                    continue;
                }

                if (bestRow != pivotRow)
                {
                    SwapRows(working, bestRow, pivotRow);
                    int tmp = rowOrder[bestRow];
                    rowOrder[bestRow] = rowOrder[pivotRow];
                    rowOrder[pivotRow] = tmp;
                }

                independent.Add(rowOrder[pivotRow]);

                double pivot = working[pivotRow, col];
                for (int c = col; c < cols; c++)
                {
                    working[pivotRow, c] /= pivot;
                }

                for (int r = 0; r < constraintRows.Count; r++)
                {
                    if (r == pivotRow)
                    {
                        continue;
                    }

                    double factor = working[r, col];
                    if (Math.Abs(factor) < eps)
                    {
                        continue;
                    }

                    for (int c = col; c < cols; c++)
                    {
                        working[r, c] -= factor * working[pivotRow, c];
                    }
                }

                pivotRow++;
            }

            return independent;
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

        private static IReadOnlyCollection<int> GetSoftRows(double[] allTolerances)
        {
            if (allTolerances == null)
            {
                return Array.Empty<int>();
            }

            var rows = new List<int>();
            for (int i = 0; i < allTolerances.Length; i++)
            {
                if (allTolerances[i] >= 1e6)
                {
                    rows.Add(i);
                }
            }

            return rows;
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
