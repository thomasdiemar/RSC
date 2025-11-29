using System;
using System.Collections.Generic;
using System.Linq;
using LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Diagnostics;
using LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Model;
using LinearSolver;

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
            var softRows = new List<int>();

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
                    softRows.Add(r);
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
                // Neutral axes become soft when non-commanded forces/torques are allowed (signaled via NaN in BuildDesiredVector).
                for (int i = 0; i < originalConstraintRows.Count; i++)
                {
                    var row = originalConstraintRows[i];
                    var rhs = originalConstraintValues[i];
                    if (double.IsNaN(rhs))
                    {
                        softRows.Add(row);
                    }
                }
            }

            var feasible = SolveEnumerating(coefficients, constraintRows, constraintValues, targetRow, targetSign, allRightHandSides, allTolerances, softRows, minimizeThrust: false);
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
                // Secondary: minimize unintended axes + thrust (no target objective).
                var refined = SolveEnumerating(coefficients, lockedConstraints, lockedValues, targetRow: -1, targetSign: 1, allRightHandSides: allRightHandSides, allTolerances: allTolerances, softRows, minimizeThrust: true);
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
            double[] allTolerances,
            IList<int> softRows,
            bool minimizeThrust)
        {
            var workingRows = new List<int>(constraintRows ?? Array.Empty<int>());
            var workingValues = new List<double>(constraintValues ?? Array.Empty<double>());

            int cols = coefficients.GetLength(1);
            const double eps = 1e-9;

            if (workingRows.Count != workingValues.Count)
            {
                return null;
            }

            var augmentedRows = new List<int>(workingRows);
            var augmentedValues = new List<double>(workingValues);
            return SearchFeasible(coefficients, augmentedRows, augmentedValues, targetRow, targetSign, softRows, allRightHandSides, minimizeThrust, eps);
        }

        private static double[] SearchFeasible(
            double[,] coefficients,
            IList<int> constraintRows,
            IList<double> constraintValues,
            int targetRow,
            int targetSign,
            IList<int> softRows,
            double[] rightHandSides,
            bool minimizeThrust,
            double eps)
        {
            int cols = coefficients.GetLength(1);
            int rows = coefficients.GetLength(0);
            int constraintCount = constraintRows.Count;
            var softSet = softRows != null ? new HashSet<int>(softRows) : new HashSet<int>();
            var constraintSet = new HashSet<int>(constraintRows);

            double bestScore = double.NegativeInfinity;
            double[] best = null;

            if (cols <= 16)
            {
                int totalMasks = 1 << cols;
                for (int mask = 0; mask < totalMasks; mask++)
                {
                    var candidate = new double[cols];
                    for (int c = 0; c < cols; c++)
                    {
                        candidate[c] = ((mask >> c) & 1) == 1 ? 1.0 : 0.0;
                    }

                    var score = ScoreCandidate(coefficients, constraintRows, constraintValues, targetRow, targetSign, softSet, constraintSet, rows, minimizeThrust, candidate);
                    if (score.Score > bestScore + 1e-9)
                    {
                        bestScore = score.Score;
                        best = score.Solution;
                    }
                }

                // Check if we have a good solution with non-zero objective
                bool hasGoodObjective = true;
                if (best != null && targetRow >= 0)
                {
                    double obj = 0;
                    for (int j = 0; j < cols; j++)
                    {
                        obj += coefficients[targetRow, j] * best[j];
                    }
                    obj *= targetSign;
                    hasGoodObjective = Math.Abs(obj) > 1e-9;
                }

                if (best != null && bestScore > double.NegativeInfinity / 2 && hasGoodObjective)
                {
                    return best;
                }
            }

            if (constraintCount > 0)
            {
                var exact = SolveEqualitySystem(coefficients, constraintRows, constraintValues, cols, eps);
                if (exact != null)
                {
                    var exactScore = ScoreCandidate(coefficients, constraintRows, constraintValues, targetRow, targetSign, softSet, constraintSet, rows, minimizeThrust, exact);
                    if (exactScore.Score > bestScore + 1e-9)
                    {
                        bestScore = exactScore.Score;
                        best = exactScore.Solution;
                    }
                }

                var lsCandidate = SolveLeastSquares(coefficients, constraintRows, constraintValues, cols, eps);
                if (lsCandidate != null)
                {
                    var score = ScoreCandidate(coefficients, constraintRows, constraintValues, targetRow, targetSign, softSet, constraintSet, rows, minimizeThrust, lsCandidate);

                    // Only update if better, unless we're in minimizeThrust mode and need any feasible solution
                    if (score.Score > bestScore + 1e-9 || (minimizeThrust && score.Score > double.NegativeInfinity))
                    {
                        bestScore = score.Score;
                        best = score.Solution;
                    }

                    if (minimizeThrust && score.Score > double.NegativeInfinity)
                    {
                        return best;
                    }
                }
            }

            if (constraintCount == 0)
            {
                var solution = new double[cols];
                if (targetRow >= 0 && !minimizeThrust)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        double coeff = coefficients[targetRow, c] * targetSign;
                        if (coeff > eps)
                        {
                            solution[c] = 1.0;
                        }
                        else if (coeff < -eps)
                        {
                            solution[c] = 0.0;
                        }
                    }
                }
                return solution;
            }

            var valueLookup = new Dictionary<int, double>();
            for (int i = 0; i < constraintRows.Count; i++)
            {
                valueLookup[constraintRows[i]] = constraintValues[i];
            }

            var variables = Enumerable.Range(0, cols).ToArray();
            var freeCombos = Choose(variables, constraintCount);

            foreach (var free in freeCombos)
            {
                var freeSet = new HashSet<int>(free);
                var fixedVars = variables.Where(v => !freeSet.Contains(v)).ToArray();
                int assignmentCount = 1 << fixedVars.Length;

                var freeList = free.ToArray();
                var constraintMatrix = new double[constraintCount, constraintCount];
                for (int r = 0; r < constraintCount; r++)
                {
                    for (int c = 0; c < constraintCount; c++)
                    {
                        constraintMatrix[r, c] = coefficients[constraintRows[r], freeList[c]];
                    }
                }

                for (int mask = 0; mask < assignmentCount; mask++)
                {
                    var rhs = new double[constraintCount];
                    for (int r = 0; r < constraintCount; r++)
                    {
                        rhs[r] = valueLookup[constraintRows[r]];
                    }

                    for (int j = 0; j < fixedVars.Length; j++)
                    {
                        double val = ((mask >> j) & 1) == 1 ? 1.0 : 0.0;
                        int varIndex = fixedVars[j];
                        for (int r = 0; r < constraintCount; r++)
                        {
                            rhs[r] -= coefficients[constraintRows[r], varIndex] * val;
                        }
                    }

                    var freeSolution = SolveLinearSystem(constraintMatrix, rhs, eps);
                    if (freeSolution == null)
                    {
                        continue;
                    }

                    var candidate = new double[cols];
                    bool withinBounds = true;

                    for (int i = 0; i < fixedVars.Length; i++)
                    {
                        candidate[fixedVars[i]] = ((mask >> i) & 1) == 1 ? 1.0 : 0.0;
                    }

                    for (int i = 0; i < freeList.Length; i++)
                    {
                        double val = freeSolution[i];
                        if (val < -eps || val > 1.0 + eps)
                        {
                            withinBounds = false;
                            break;
                        }

                        candidate[freeList[i]] = Math.Max(0, Math.Min(1.0, val));
                    }

                    if (!withinBounds)
                    {
                        continue;
                    }

                    double penalty = 0;
                    for (int r = 0; r < constraintRows.Count; r++)
                    {
                        double actual = EvaluateRow(coefficients, constraintRows[r], candidate);
                        penalty += Math.Abs(actual - valueLookup[constraintRows[r]]);
                    }

                    double objective = 0;
                    if (targetRow >= 0)
                    {
                        objective = targetSign * EvaluateRow(coefficients, targetRow, candidate);
                    }

                    double unintendedPenalty = 0;
                    for (int r = 0; r < rows; r++)
                    {
                        if (r == targetRow || constraintSet.Contains(r))
                        {
                            continue;
                        }

                        double deviation = EvaluateRow(coefficients, r, candidate);
                        double weight = softSet.Contains(r) ? 1_000_000.0 : 1_000_000.0;
                        unintendedPenalty += weight * Math.Abs(deviation);
                    }

                    double thrust = candidate.Sum();
                    double score = minimizeThrust
                        ? -1_000_000.0 * penalty - 1_000.0 * unintendedPenalty - thrust
                        : objective - 1_000_000.0 * penalty - 1_000.0 * unintendedPenalty - 1e-3 * thrust;

                    if (score > bestScore + 1e-9)
                    {
                        bestScore = score;
                        best = candidate;
                    }
                }
            }

            // If we have an overdetermined system (more constraints than variables) and haven't found a good solution,
            // try to solve for the constraints and then maximize the objective
            // Check if current solution has zero or near-zero objective value
            double currentObjective = 0;
            if (best != null && targetRow >= 0)
            {
                for (int j = 0; j < cols; j++)
                {
                    currentObjective += coefficients[targetRow, j] * best[j];
                }
                currentObjective *= targetSign;
            }



            // Handle overdetermined systems (more constraints than variables)
            if (constraintCount > cols && (best == null || Math.Abs(currentObjective) < 1e-9))
            {
                var overdeterminedSolution = SolveOverdeterminedSystem(
                    coefficients, constraintRows, constraintValues, targetRow, targetSign,
                    softSet, constraintSet, rows, minimizeThrust, eps);
                if (overdeterminedSolution != null)
                {
                    var score = ScoreCandidate(coefficients, constraintRows, constraintValues,
                        targetRow, targetSign, softSet, constraintSet, rows, minimizeThrust, overdeterminedSolution);
                    if (score.Score > bestScore + 1e-9)
                    {
                        bestScore = score.Score;
                        best = score.Solution;
                    }
                }
            }

            // Handle underdetermined systems (more variables than constraints)
            // Need to maximize objective among infinitely many feasible solutions
            if (cols > constraintCount && targetRow >= 0 && (best == null || Math.Abs(currentObjective) < 1e-9))
            {
                var underdeterminedSolution = SolveUnderdeterminedSystem(
                    coefficients, constraintRows, constraintValues, targetRow, targetSign,
                    cols, constraintCount, eps);
                if (underdeterminedSolution != null)
                {
                    var score = ScoreCandidate(coefficients, constraintRows, constraintValues,
                        targetRow, targetSign, softSet, constraintSet, rows, minimizeThrust, underdeterminedSolution);
                    if (score.Score > bestScore + 1e-9)
                    {
                        bestScore = score.Score;
                        best = score.Solution;
                    }
                }
            }

            return best;
        }

        /// <summary>
        /// Specialized solver for overdetermined systems where we need to satisfy constraints and optimize an objective.
        /// Uses QR decomposition to find the minimum-norm solution, then tries to improve it along the null space.
        /// </summary>
        private static double[] SolveOverdeterminedSystem(
            double[,] coefficients,
            IList<int> constraintRows,
            IList<double> constraintValues,
            int targetRow,
            int targetSign,
            HashSet<int> softSet,
            HashSet<int> constraintSet,
            int rows,
            bool minimizeThrust,
            double eps)
        {
            int cols = coefficients.GetLength(1);
            int m = constraintRows.Count;

            if (m == 0 || cols == 0) return null;

            // Build the constraint matrix A and RHS vector b
            var A = new double[m, cols];
            var b = new double[m];
            for (int i = 0; i < m; i++)
            {
                int row = constraintRows[i];
                b[i] = constraintValues[i];
                for (int j = 0; j < cols; j++)
                {
                    A[i, j] = coefficients[row, j];
                }
            }

            // Simple approach: Try different combinations of which variables to set to 0 or 1,
            // then solve the resulting system
            double[] bestSolution = null;
            double bestObjective = double.NegativeInfinity;

            // Try all combinations of setting variables to their bounds
            if (cols <= 10)
            {
                int maxCombos = 1 << (cols * 2);  // 2^(2*cols) combinations (each variable can be free, =0, or =1)
                for (int combo = 0; combo < Math.Min(maxCombos, 10000); combo++)
                {
                    var fixedMask = new int[cols];  // 0=free, 1=fix to 0, 2=fix to 1
                    int temp = combo;
                    for (int j = 0; j < cols; j++)
                    {
                        fixedMask[j] = temp & 3;
                        temp >>= 2;
                    }

                    // Try to solve with these fixed values
                    var candidate = TrySolveWithFixedVariables(A, b, fixedMask, cols, m, eps);
                    if (candidate == null) continue;

                    // Evaluate objective
                    double obj = 0;
                    if (targetRow >= 0)
                    {
                        for (int j = 0; j < cols; j++)
                        {
                            obj += coefficients[targetRow, j] * candidate[j];
                        }
                        obj *= targetSign;
                    }

                    if (obj > bestObjective + eps)
                    {
                        bestObjective = obj;
                        bestSolution = candidate;
                    }
                }
            }

            return bestSolution;
        }

        /// <summary>
        /// Solve an underdetermined system (more variables than constraints) by maximizing the objective.
        /// Strategy: Fix some variables at their bounds and solve for the rest.
        /// </summary>
        private static double[] SolveUnderdeterminedSystem(
            double[,] coefficients,
            IList<int> constraintRows,
            IList<double> constraintValues,
            int targetRow,
            int targetSign,
            int cols,
            int m,
            double eps)
        {

            // Build constraint matrix A and RHS vector b
            var A = new double[m, cols];
            var b = new double[m];
            for (int i = 0; i < m; i++)
            {
                int row = constraintRows[i];
                b[i] = constraintValues[i];
                for (int j = 0; j < cols; j++)
                {
                    A[i, j] = coefficients[row, j];
                }
            }

            double[] bestSolution = null;
            double bestObjective = double.NegativeInfinity;

            // Get objective coefficients
            var objCoeffs = new double[cols];
            for (int j = 0; j < cols; j++)
            {
                objCoeffs[j] = coefficients[targetRow, j] * targetSign;
            }

            // Strategy: Try fixing different subsets of variables to maximize the objective
            // Since we have more degrees of freedom, prioritize variables with positive objective coefficients

            // First, try a greedy approach: set variables with positive objective coefficients to 1
            var greedy = new double[cols];
            for (int j = 0; j < cols; j++)
            {
                greedy[j] = objCoeffs[j] > 0 ? 1.0 : 0.0;
            }

            // Project onto constraint manifold
            double bestThrust = double.PositiveInfinity;
            var greedySolution = ProjectOntoConstraints(A, b, greedy, cols, m, eps);
            if (greedySolution != null)
            {
                double obj = 0;
                for (int j = 0; j < cols; j++)
                {
                    obj += objCoeffs[j] * greedySolution[j];
                }
                double thrust = greedySolution.Sum();
                if (obj > bestObjective)
                {
                    bestObjective = obj;
                    bestThrust = thrust;
                    bestSolution = greedySolution;
                }
            }

            // Try different combinations of fixed variables (limit search to avoid explosion)
            // For 6 variables, 4^6 = 4096 combinations
            int maxCombos = Math.Min(1 << (cols * 2), 10000);
            for (int combo = 0; combo < maxCombos; combo++)
            {
                var fixedMask = new int[cols];
                int temp = combo;
                for (int j = 0; j < cols; j++)
                {
                    fixedMask[j] = temp & 3;  // 0=free, 1=fix to 0, 2=fix to 1
                    temp >>= 2;
                }

                var candidate = TrySolveWithFixedVariables(A, b, fixedMask, cols, m, eps);
                if (candidate == null) continue;

                double obj = 0;
                for (int j = 0; j < cols; j++)
                {
                    obj += objCoeffs[j] * candidate[j];
                }

                double thrust = candidate.Sum();

                // Use objective as primary, thrust as tie-breaker
                bool isBetter = obj > bestObjective + eps ||
                                (Math.Abs(obj - bestObjective) <= eps && thrust < bestThrust - eps);

                if (isBetter)
                {
                    bestObjective = obj;
                    bestThrust = thrust;
                    bestSolution = candidate;
                }
            }

            return bestSolution;
        }

        /// <summary>
        /// Project a point onto the constraint manifold A*x = b.
        /// </summary>
        private static double[] ProjectOntoConstraints(double[,] A, double[] b, double[] initial, int n, int m, double eps)
        {
            // Use least squares: minimize ||x - initial||^2 subject to A*x = b
            // Lagrangian: L = ||x - initial||^2 + lambda^T (A*x - b)
            // Optimality: 2(x - initial) + A^T*lambda = 0, A*x = b
            // => x = initial - 0.5*A^T*lambda, A*x = b
            // => A*initial - 0.5*A*A^T*lambda = b
            // => lambda = 2*(A*A^T)^{-1}*(A*initial - b)

            var aat = new double[m, m];
            var rhs = new double[m];

            // Compute A*A^T and A*initial - b
            for (int i = 0; i < m; i++)
            {
                rhs[i] = -b[i];
                for (int j = 0; j < n; j++)
                {
                    rhs[i] += A[i, j] * initial[j];
                }

                for (int k = 0; k < m; k++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        aat[i, k] += A[i, j] * A[k, j];
                    }
                }
            }

            // Add regularization
            for (int i = 0; i < m; i++)
            {
                aat[i, i] += eps;
            }

            var lambda = SolveLinearSystem(aat, rhs, eps);
            if (lambda == null) return null;

            // Compute x = initial - 0.5*A^T*lambda
            var result = new double[n];
            for (int j = 0; j < n; j++)
            {
                result[j] = initial[j];
                for (int i = 0; i < m; i++)
                {
                    result[j] -= 0.5 * A[i, j] * lambda[i];
                }
                result[j] = Math.Max(0, Math.Min(1.0, result[j]));
            }

            // Verify constraints
            for (int i = 0; i < m; i++)
            {
                double val = 0;
                for (int j = 0; j < n; j++)
                {
                    val += A[i, j] * result[j];
                }
                if (Math.Abs(val - b[i]) > 1e-4)
                {
                    return null;
                }
            }

            return result;
        }

        private static double[] TrySolveWithFixedVariables(double[,] A, double[] b, int[] fixedMask, int n, int m, double eps)
        {
            // fixedMask[j]: 0=free, 1=fix to 0, 2=fix to 1, 3=try both
            var freeVars = new List<int>();
            var result = new double[n];

            for (int j = 0; j < n; j++)
            {
                if (fixedMask[j] == 0 || fixedMask[j] == 3)
                {
                    freeVars.Add(j);
                }
                else if (fixedMask[j] == 1)
                {
                    result[j] = 0;
                }
                else if (fixedMask[j] == 2)
                {
                    result[j] = 1;
                }
            }

            // If we have free variables, we need to solve for them
            if (freeVars.Count > 0)
            {
                // Build reduced system for free variables
                var reducedA = new double[m, freeVars.Count];
                var reducedB = new double[m];

                for (int i = 0; i < m; i++)
                {
                    reducedB[i] = b[i];
                    // Subtract contribution from fixed variables
                    for (int j = 0; j < n; j++)
                    {
                        if (fixedMask[j] != 0 && fixedMask[j] != 3)
                        {
                            reducedB[i] -= A[i, j] * result[j];
                        }
                    }

                    // Copy columns for free variables
                    for (int k = 0; k < freeVars.Count; k++)
                    {
                        reducedA[i, k] = A[i, freeVars[k]];
                    }
                }

                // Solve reduced system
                var freeSolution = SolveLeastSquaresWithBounds(reducedA, reducedB, freeVars.Count, eps);
                if (freeSolution == null) return null;

                for (int k = 0; k < freeVars.Count; k++)
                {
                    result[freeVars[k]] = freeSolution[k];
                }
            }

            // Verify all constraints are satisfied
            for (int i = 0; i < m; i++)
            {
                double val = 0;
                for (int j = 0; j < n; j++)
                {
                    val += A[i, j] * result[j];
                }
                if (Math.Abs(val - b[i]) > 1e-4)
                {
                    return null;
                }
            }

            return result;
        }

        private static double[] FindFeasibleNear(double[,] A, double[] b, double[] initial, int n, int m, double eps)
        {
            // Use least squares to project the initial point onto the constraint manifold
            // Solve: minimize ||x - initial||^2 subject to A*x = b

            // This is equivalent to solving: (A^T*A + I)*x = A^T*b + initial
            var ata = new double[n, n];
            var rhs = new double[n];

            // Compute A^T*A and A^T*b
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    double aij = A[i, j];
                    rhs[j] += aij * b[i];
                    for (int k = 0; k < n; k++)
                    {
                        ata[j, k] += aij * A[i, k];
                    }
                }
            }

            // Add identity term (regularization) and initial point bias
            for (int i = 0; i < n; i++)
            {
                ata[i, i] += 1.0;
                rhs[i] += initial[i];
            }

            var solution = SolveLinearSystem(ata, rhs, eps);
            if (solution == null) return null;

            // Clamp to [0, 1]
            for (int i = 0; i < n; i++)
            {
                solution[i] = Math.Max(0, Math.Min(1.0, solution[i]));
            }

            // Verify constraints are satisfied
            for (int i = 0; i < m; i++)
            {
                double val = 0;
                for (int j = 0; j < n; j++)
                {
                    val += A[i, j] * solution[j];
                }
                if (Math.Abs(val - b[i]) > 1e-4)  // Relax tolerance slightly
                {
                    return null;  // Not feasible
                }
            }

            return solution;
        }

        private static double EvaluateObjective(double[,] coefficients, int targetRow, int targetSign, double[] solution)
        {
            if (targetRow < 0) return 0;
            double val = 0;
            for (int j = 0; j < solution.Length; j++)
            {
                val += coefficients[targetRow, j] * solution[j];
            }
            return val * targetSign;
        }

        private static double[] SolveLeastSquaresWithBounds(double[,] A, double[] b, int n, double eps)
        {
            int m = A.GetLength(0);

            // Compute A^T * A and A^T * b
            var ata = new double[n, n];
            var atb = new double[n];

            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    double aij = A[i, j];
                    atb[j] += aij * b[i];
                    for (int k = 0; k < n; k++)
                    {
                        ata[j, k] += aij * A[i, k];
                    }
                }
            }

            // Add regularization for stability
            for (int i = 0; i < n; i++)
            {
                ata[i, i] += eps;
            }

            // Solve the system
            var solution = SolveLinearSystem(ata, atb, eps);
            if (solution == null) return null;

            // Clamp to [0, 1]
            for (int i = 0; i < n; i++)
            {
                solution[i] = Math.Max(0, Math.Min(1.0, solution[i]));
            }

            return solution;
        }

        private static void SetObjective(double[,] tableau, int[] basis, double[] cost)
        {
            int rowCount = tableau.GetLength(0) - 1;
            int colCount = tableau.GetLength(1) - 1;
            int objRow = rowCount;

            for (int c = 0; c < colCount; c++)
            {
                tableau[objRow, c] = -cost[c];
            }
            tableau[objRow, colCount] = 0;

            for (int r = 0; r < rowCount; r++)
            {
                int basic = basis[r];
                double weight = cost[basic];
                if (Math.Abs(weight) < 1e-12)
                {
                    continue;
                }

                for (int c = 0; c <= colCount; c++)
                {
                    tableau[objRow, c] += weight * tableau[r, c];
                }
            }
        }

        private static bool RunSimplex(double[,] tableau, int[] basis, double eps)
        {
            int rowCount = tableau.GetLength(0) - 1;
            int colCount = tableau.GetLength(1) - 1;
            int objRow = rowCount;

            while (true)
            {
                int entering = -1;
                double mostNeg = -eps;
                for (int c = 0; c < colCount; c++)
                {
                    double val = tableau[objRow, c];
                    if (val < mostNeg)
                    {
                        mostNeg = val;
                        entering = c;
                    }
                }

                if (entering < 0)
                {
                    return true;
                }

                int pivotRow = -1;
                double bestRatio = double.PositiveInfinity;
                for (int r = 0; r < rowCount; r++)
                {
                    double coeff = tableau[r, entering];
                    if (coeff <= eps)
                    {
                        continue;
                    }

                    double ratio = tableau[r, colCount] / coeff;
                    if (ratio < bestRatio - eps || (Math.Abs(ratio - bestRatio) <= eps && basis[pivotRow < 0 ? r : pivotRow] > basis[r]))
                    {
                        bestRatio = ratio;
                        pivotRow = r;
                    }
                }

                if (pivotRow < 0)
                {
                    return false;
                }

                Pivot(tableau, basis, pivotRow, entering, eps);
            }
        }

        private static void Pivot(double[,] tableau, int[] basis, int pivotRow, int entering, double eps)
        {
            int rowCount = tableau.GetLength(0);
            int colCount = tableau.GetLength(1);
            double pivot = tableau[pivotRow, entering];
            for (int c = 0; c < colCount; c++)
            {
                tableau[pivotRow, c] /= pivot;
            }

            for (int r = 0; r < rowCount; r++)
            {
                if (r == pivotRow)
                {
                    continue;
                }

                double factor = tableau[r, entering];
                if (Math.Abs(factor) < eps)
                {
                    continue;
                }

                for (int c = 0; c < colCount; c++)
                {
                    tableau[r, c] -= factor * tableau[pivotRow, c];
                }
            }

            basis[pivotRow] = entering;
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

        private static (double Score, double[] Solution) ScoreCandidate(
            double[,] coefficients,
            IList<int> constraintRows,
            IList<double> constraintValues,
            int targetRow,
            int targetSign,
            HashSet<int> softSet,
            HashSet<int> constraintSet,
            int totalRows,
            bool minimizeThrust,
            double[] candidate)
        {
            const double penaltyWeight = 1_000_000.0;
            double unintendedPenalty = 0;

            // Enforce constraints strictly (preemptive): any violation discards the candidate.
            for (int i = 0; i < constraintRows.Count; i++)
            {
                int row = constraintRows[i];
                double desired = constraintValues[i];
                double actual = EvaluateRow(coefficients, row, candidate);
                if (Math.Abs(actual - desired) > 1e-6)
                {
                    return (double.NegativeInfinity, candidate);
                }
            }

            if (minimizeThrust)
            {
                for (int r = 0; r < totalRows; r++)
                {
                    if (r == targetRow || constraintSet.Contains(r))
                    {
                        continue;
                    }

                    double deviation = EvaluateRow(coefficients, r, candidate);
                    if (!softSet.Contains(r) && Math.Abs(deviation) > 1e-6)
                    {
                        // Hard unintended axes remain hard in clean-up phase.
                        return (double.NegativeInfinity, candidate);
                    }

                    unintendedPenalty += Math.Abs(deviation);
                }
            }

            double objective = 0;
            if (targetRow >= 0)
            {
                objective = targetSign * EvaluateRow(coefficients, targetRow, candidate);
            }

            double thrust = candidate.Sum();
            double score = minimizeThrust
                ? -unintendedPenalty - thrust
                : objective - 1e-3 * thrust;

            return (score, candidate);
        }

        private static double[] SolveLeastSquares(
            double[,] coefficients,
            IList<int> constraintRows,
            IList<double> constraintValues,
            int columnCount,
            double eps)
        {
            if (constraintRows == null || constraintValues == null || constraintRows.Count == 0 || constraintRows.Count != constraintValues.Count)
            {
                return null;
            }

            int m = constraintRows.Count;
            int n = columnCount;
            var ata = new double[n, n];
            var atb = new double[n];

            for (int idx = 0; idx < m; idx++)
            {
                int row = constraintRows[idx];
                double b = constraintValues[idx];
                for (int c = 0; c < n; c++)
                {
                    double a = coefficients[row, c];
                    atb[c] += a * b;
                    for (int c2 = 0; c2 < n; c2++)
                    {
                        ata[c, c2] += a * coefficients[row, c2];
                    }
                }
            }

            for (int i = 0; i < n; i++)
            {
                ata[i, i] += eps;
            }

            var solution = SolveLinearSystem(ata, atb, eps);
            if (solution == null)
            {
                return null;
            }

            for (int i = 0; i < solution.Length; i++)
            {
                if (double.IsNaN(solution[i]) || double.IsInfinity(solution[i]))
                {
                    return null;
                }

                solution[i] = Math.Max(0, Math.Min(1.0, solution[i]));
            }

            return solution;
        }

        private static double[] SolveEqualitySystem(
            double[,] coefficients,
            IList<int> constraintRows,
            IList<double> constraintValues,
            int columnCount,
            double eps)
        {
            int m = constraintRows.Count;
            if (m == 0)
            {
                return null;
            }

            int n = columnCount;
            var a = new double[m, n];
            var b = new double[m];

            for (int r = 0; r < m; r++)
            {
                int rowIndex = constraintRows[r];
                b[r] = constraintValues[r];
                for (int c = 0; c < n; c++)
                {
                    a[r, c] = coefficients[rowIndex, c];
                }
            }

            var pivotCols = new int[m];
            for (int i = 0; i < m; i++)
            {
                pivotCols[i] = -1;
            }

            int pivotRow = 0;
            for (int col = 0; col < n && pivotRow < m; col++)
            {
                int best = pivotRow;
                double bestVal = Math.Abs(a[best, col]);
                for (int r = pivotRow + 1; r < m; r++)
                {
                    double val = Math.Abs(a[r, col]);
                    if (val > bestVal)
                    {
                        bestVal = val;
                        best = r;
                    }
                }

                if (bestVal < eps)
                {
                    continue;
                }

                if (best != pivotRow)
                {
                    SwapRows(a, best, pivotRow);
                    double tmp = b[best];
                    b[best] = b[pivotRow];
                    b[pivotRow] = tmp;
                }

                double pivot = a[pivotRow, col];
                for (int c = col; c < n; c++)
                {
                    a[pivotRow, c] /= pivot;
                }
                b[pivotRow] /= pivot;
                pivotCols[pivotRow] = col;

                for (int r = 0; r < m; r++)
                {
                    if (r == pivotRow)
                    {
                        continue;
                    }

                    double factor = a[r, col];
                    if (Math.Abs(factor) < eps)
                    {
                        continue;
                    }

                    for (int c = col; c < n; c++)
                    {
                        a[r, c] -= factor * a[pivotRow, c];
                    }
                    b[r] -= factor * b[pivotRow];
                }

                pivotRow++;
            }

            for (int r = pivotRow; r < m; r++)
            {
                bool allZero = true;
                for (int c = 0; c < n; c++)
                {
                    if (Math.Abs(a[r, c]) > eps)
                    {
                        allZero = false;
                        break;
                    }
                }

                if (!allZero && Math.Abs(b[r]) > eps)
                {
                    return null;
                }
            }

            var solution = new double[n];
            for (int r = pivotRow - 1; r >= 0; r--)
            {
                int col = pivotCols[r];
                if (col < 0)
                {
                    continue;
                }

                double value = b[r];
                for (int c = col + 1; c < n; c++)
                {
                    value -= a[r, c] * solution[c];
                }

                solution[col] = value;
            }

            for (int i = 0; i < solution.Length; i++)
            {
                if (double.IsNaN(solution[i]) || double.IsInfinity(solution[i]))
                {
                    return null;
                }

                solution[i] = Math.Max(0, Math.Min(1.0, solution[i]));
            }

            return solution;
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
