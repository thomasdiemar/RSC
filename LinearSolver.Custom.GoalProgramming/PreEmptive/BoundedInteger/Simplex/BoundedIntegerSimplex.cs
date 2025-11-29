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

            var targetRows = new List<int>();
            var allRightHandSides = new double[tableau.RowCount];
            var allTolerances = new double[tableau.RowCount];
            var softRows = new List<int>();

            for (int r = 0; r < tableau.RowCount; r++)
            {
                allRightHandSides[r] = (double)tableau.GetRightHandSide(r);
                allTolerances[r] = (double)tableau.RowGoals[r].Tolerance;
            }

            // Collect locked constraints from already-solved lower priorities
            var lockedConstraints = new List<int>();
            var lockedValues = new List<double>();

            for (int r = 0; r < tableau.RowCount; r++)
            {
                var goal = tableau.RowGoals[r];

                // Check if this is a locked goal from a previous priority
                if (goal.Priority < priorityLevel && goal.Name.EndsWith("_Lock"))
                {
                    double lockedValue = (double)goal.RightHandSide;
                    lockedConstraints.Add(r);
                    lockedValues.Add(lockedValue);
                    Console.WriteLine($"[SolvePriority] Priority {priorityLevel} includes locked constraint from Priority {goal.Priority}: row {r} = {lockedValue}");
                }
            }

            // Initialize constraints with locked goals from previous priorities
            var constraintRows = new List<int>(lockedConstraints);
            var constraintValues = new List<double>(lockedValues);

            for (int r = 0; r < tableau.RowCount; r++)
            {
                var goal = tableau.RowGoals[r];
                if (goal.Priority != priorityLevel)
                {
                    continue;
                }

                bool isSoft = goal.Tolerance >= Fraction.MaxValue - Fraction.Epsilon;
                Console.WriteLine($"[SolvePriority] Row {r}: priority={goal.Priority}, tolerance={(double)goal.Tolerance:F0}, MaxValue={(double)Fraction.MaxValue:F0}, Epsilon={(double)Fraction.Epsilon:F12}, isSoft={isSoft}");
                var rhs = (double)goal.RightHandSide;

                if (isSoft)
                {
                    Console.WriteLine($"[SolvePriority] Adding row {r} to softRows");
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

            // Note: constraintRows includes locked goals from previous priorities (Step 3)
            if (targetRows.Count == 0 && constraintRows.Count == 0)
            {
                // No objectives and no constraints → return default solution
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

            // For priorities with only soft goals (no hard target), minimize unintended deviations
            bool shouldMinimizeSoft = (targetRow < 0 && softRows.Count > 0);
            Console.WriteLine($"[SolvePriority] Calling SolveEnumerating: priority={priorityLevel}, constraintRows.Count={constraintRows.Count} (including {lockedConstraints.Count} locked), targetRow={targetRow}, shouldMinimizeSoft={shouldMinimizeSoft}");
            var feasible = SolveEnumerating(coefficients, constraintRows, constraintValues, targetRow, targetSign, allRightHandSides, allTolerances, softRows, minimizeThrust: shouldMinimizeSoft);
            Console.WriteLine($"[SolvePriority] feasible solution: {(feasible == null ? "null" : string.Join(", ", feasible.Select((v, i) => $"x{i}={v:F3}")))}");

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
                var phase2Constraints = new List<int>(originalConstraintRows) { targetRow };
                var phase2Values = new List<double>(originalConstraintValues) { (double)objectiveValue / targetSign };
                // Secondary: minimize unintended axes + thrust (no target objective).
                var refined = SolveEnumerating(coefficients, phase2Constraints, phase2Values, targetRow: -1, targetSign: 1, allRightHandSides: allRightHandSides, allTolerances: allTolerances, softRows, minimizeThrust: true);
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

            Console.WriteLine($"[SearchFeasible] cols={cols}, constraintCount={constraintCount}, targetRow={targetRow}, minimizeThrust={minimizeThrust}");

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

                Console.WriteLine($"[SearchFeasible] Binary search: best={(best == null ? "null" : string.Join(",", best.Select(v => $"{v:F2}")))} score={bestScore:F6}");

                // When minimizing soft goals, don't return early - continue to try least squares for better solutions
                if (best != null && bestScore > double.NegativeInfinity / 2 && hasGoodObjective && !minimizeThrust)
                {
                    Console.WriteLine($"[SearchFeasible] Returning early from binary search");
                    return best;
                }
            }

            if (constraintCount > 0)
            {
                var exact = SolveEqualitySystem(coefficients, constraintRows, constraintValues, cols, eps);
                if (exact != null)
                {
                    var exactScore = ScoreCandidate(coefficients, constraintRows, constraintValues, targetRow, targetSign, softSet, constraintSet, rows, minimizeThrust, exact);
                    Console.WriteLine($"[SearchFeasible] SolveEqualitySystem: score={exactScore.Score:F6}");
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
                    Console.WriteLine($"[SearchFeasible] SolveLeastSquares: candidate={string.Join(",", lsCandidate.Select(v => $"{v:F2}"))} score={score.Score:F6}");

                    // Only update if better, unless we're in minimizeThrust mode and need any feasible solution
                    if (score.Score > bestScore + 1e-9 || (minimizeThrust && score.Score > double.NegativeInfinity))
                    {
                        bestScore = score.Score;
                        best = score.Solution;
                    }

                    if (minimizeThrust && score.Score > double.NegativeInfinity)
                    {
                        Console.WriteLine($"[SearchFeasible] Returning with LS solution");
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

            Console.WriteLine($"[SearchFeasible] Starting choose-and-fix enumeration: cols={cols}, constraintCount={constraintCount}, bestScore={bestScore:F6}");
            var variables = Enumerable.Range(0, cols).ToArray();
            var freeCombos = Choose(variables, constraintCount);
            Console.WriteLine($"[SearchFeasible] freeCombos count: {freeCombos.Count()}");

            int comboIndex = 0;
            int nullSolutionCount = 0;
            int outOfBoundsCount = 0;
            int validSolutionCount = 0;

            foreach (var free in freeCombos)
            {
                var freeSet = new HashSet<int>(free);
                var fixedVars = variables.Where(v => !freeSet.Contains(v)).ToArray();
                int assignmentCount = 1 << fixedVars.Length;
                if (comboIndex < 2) Console.WriteLine($"[SearchFeasible] Combo {comboIndex}: free={string.Join(",", free)}, fixedVars.Length={fixedVars.Length}, assignmentCount={assignmentCount}");
                comboIndex++;

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
                        nullSolutionCount++;
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
                        outOfBoundsCount++;
                        continue;
                    }

                    validSolutionCount++;

                    var scoreResult = ScoreCandidate(coefficients, constraintRows, constraintValues, targetRow, targetSign, softSet, constraintSet, rows, minimizeThrust, candidate);

                    if (validSolutionCount <= 5)
                    {
                        Console.WriteLine($"[SearchFeasible] Solution {validSolutionCount}: score={scoreResult.Score:F6}, thrust={candidate.Sum():F2}, bestScore={bestScore:F6}");
                    }

                    if (scoreResult.Score > bestScore + 1e-9)
                    {
                        Console.WriteLine($"[SearchFeasible] New best! combo={comboIndex-1}, mask={mask}, score={scoreResult.Score:F6} (was {bestScore:F6}), thrust={candidate.Sum():F2}");
                        Console.WriteLine($"[SearchFeasible] Solution: {string.Join(",", candidate.Select((v, i) => v > 1e-6 ? $"x{i}={v:F3}" : "").Where(s => s != ""))}");
                        bestScore = scoreResult.Score;
                        best = scoreResult.Solution;
                    }
                }
            }

            Console.WriteLine($"[SearchFeasible] Choose-and-fix summary: {validSolutionCount} valid, {nullSolutionCount} null, {outOfBoundsCount} out-of-bounds, total={validSolutionCount + nullSolutionCount + outOfBoundsCount}");

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
                Console.WriteLine($"[SearchFeasible] Trying SolveUnderdeterminedSystem with targetRow={targetRow}");
                var underdeterminedSolution = SolveUnderdeterminedSystem(
                    coefficients, constraintRows, constraintValues, targetRow, targetSign,
                    cols, constraintCount, eps);
                if (underdeterminedSolution != null)
                {
                    var score = ScoreCandidate(coefficients, constraintRows, constraintValues,
                        targetRow, targetSign, softSet, constraintSet, rows, minimizeThrust, underdeterminedSolution);
                    Console.WriteLine($"[SearchFeasible] SolveUnderdeterminedSystem: score={score.Score:F6}");
                    if (score.Score > bestScore + 1e-9)
                    {
                        bestScore = score.Score;
                        best = score.Solution;
                    }
                }
            }

            // Try active set QP solver for underdetermined systems with soft goals
            // Only call QP if choose-and-fix found a solution - QP may find better fractional solution
            if (cols > constraintCount && minimizeThrust && softSet.Count > 0 && best != null)
            {
                Console.WriteLine($"[SearchFeasible] Trying active set QP solver to improve solution");

                // Build soft goal matrix
                var softRowsList = softRows.ToList();
                var A_soft = new double[softRowsList.Count, cols];
                for (int i = 0; i < softRowsList.Count; i++)
                {
                    int row = softRowsList[i];
                    for (int j = 0; j < cols; j++)
                    {
                        A_soft[i, j] = coefficients[row, j];
                    }
                }

                // Build equality constraint matrix
                var A_eq = new double[constraintCount, cols];
                var b_eq = new double[constraintCount];
                for (int i = 0; i < constraintCount; i++)
                {
                    int row = constraintRows[i];
                    b_eq[i] = constraintValues[i];
                    for (int j = 0; j < cols; j++)
                    {
                        A_eq[i, j] = coefficients[row, j];
                    }
                }

                // Bounds
                var lower = new double[cols];
                var upper = new double[cols];
                for (int j = 0; j < cols; j++)
                {
                    lower[j] = 0.0;
                    upper[j] = 1.0;
                }

                // Initial solution - spread load evenly across all variables that can contribute
                var x0 = new double[cols];
                if (best != null)
                {
                    Array.Copy(best, x0, cols);
                }

                // Improve initial guess by distributing evenly across all variables
                // This helps QP solver explore the solution space better
                double totalLoad = 0;
                for (int j = 0; j < cols; j++)
                    totalLoad += x0[j];

                if (totalLoad < 0.01)  // If near zero, initialize with uniform distribution
                {
                    // Compute how much each variable needs to contribute to satisfy constraint
                    double avgContribution = 0;
                    for (int i = 0; i < constraintCount; i++)
                    {
                        avgContribution += Math.Abs(b_eq[i]);
                    }
                    avgContribution /= (cols * Math.Max(1, constraintCount));

                    for (int j = 0; j < cols; j++)
                    {
                        x0[j] = Math.Min(0.5, avgContribution);  // Start at mid-range
                    }

                    Console.WriteLine($"[SearchFeasible] QP: Using uniform initial guess with avg={avgContribution:F3}");
                }

                var qpSolution = SolveBoundedConstrainedLeastSquares(A_soft, A_eq, b_eq, lower, upper, x0, eps);
                if (qpSolution != null)
                {
                    var score = ScoreCandidate(coefficients, constraintRows, constraintValues,
                        targetRow, targetSign, softSet, constraintSet, rows, minimizeThrust, qpSolution);
                    Console.WriteLine($"[SearchFeasible] Active set QP: score={score.Score:F6}, solution={string.Join(",", qpSolution.Select(v => v > 1e-6 ? $"{v:F3}" : "0"))}");
                    if (score.Score > bestScore + 1e-9)
                    {
                        Console.WriteLine($"[SearchFeasible] QP solution is better! (was {bestScore:F6})");
                        bestScore = score.Score;
                        best = score.Solution;
                    }
                }
            }

            Console.WriteLine($"[SearchFeasible] Returning final: best={(best == null ? "null" : string.Join(",", best.Select(v => $"{v:F2}")))} score={bestScore:F6}");
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
            double bestThrust = double.PositiveInfinity;

            // Get objective coefficients
            var objCoeffs = new double[cols];
            for (int j = 0; j < cols; j++)
            {
                objCoeffs[j] = coefficients[targetRow, j] * targetSign;
            }

            // Strategy: Try fixing different subsets of variables to maximize the objective
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

        /// <summary>
        /// Solves bounded constrained least squares: min ||A_soft * x||² subject to A_eq * x = b_eq, lower ≤ x ≤ upper
        /// Uses active set method for quadratic programming with bounds.
        /// </summary>
        private static double[] SolveBoundedConstrainedLeastSquares(
            double[,] A_soft,      // Soft goal matrix (m_soft × n)
            double[,] A_eq,        // Equality constraint matrix (m_eq × n)
            double[] b_eq,         // Equality constraint RHS (m_eq)
            double[] lower,        // Lower bounds (n)
            double[] upper,        // Upper bounds (n)
            double[] x0,           // Initial feasible solution (n)
            double eps,
            int maxIterations = 100)
        {
            int n = A_soft.GetLength(1);  // Number of variables
            int m_soft = A_soft.GetLength(0);  // Number of soft goals
            int m_eq = A_eq.GetLength(0);  // Number of equality constraints

            // Active Set QP solver for soft goal minimization

            // Build H = A_soft^T * A_soft (objective Hessian)
            var H = new double[n, n];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < m_soft; k++)
                    {
                        sum += A_soft[k, i] * A_soft[k, j];
                    }
                    H[i, j] = sum;
                }
            }

            // c = 0 (no linear term for min ||A*x||²)
            var c = new double[n];

            // Active set: tracks which bounds are active
            // 0 = free, 1 = at lower bound, 2 = at upper bound
            var activeSet = new int[n];
            var x = new double[n];
            Array.Copy(x0, x, n);

            // Initialize active set based on x0 to avoid alpha=0 issues
            for (int i = 0; i < n; i++)
            {
                if (Math.Abs(x[i] - lower[i]) < eps)
                    activeSet[i] = 1;  // At lower bound
                else if (Math.Abs(x[i] - upper[i]) < eps)
                    activeSet[i] = 2;  // At upper bound
                else
                    activeSet[i] = 0;  // Free
            }

            for (int iter = 0; iter < maxIterations; iter++)
            {
                // Step 1: Solve equality-constrained QP with current active set
                var result = SolveEqualityConstrainedQP(H, c, A_eq, b_eq, activeSet, lower, upper, x, n, m_eq, eps);
                if (result.Item1 == null)
                {
                    break;
                }

                var p = result.Item1;  // Search direction
                var lambda = result.Item2;  // Lagrange multipliers for active bounds

                // Step 2: Check if we're at optimum (p ≈ 0 and all lambda ≥ 0)
                double pNorm = 0;
                for (int i = 0; i < n; i++)
                    pNorm += p[i] * p[i];
                pNorm = Math.Sqrt(pNorm);

                if (pNorm < eps)
                {
                    // Check Lagrange multipliers
                    int worstIdx = -1;
                    double worstLambda = 0;
                    for (int i = 0; i < n; i++)
                    {
                        if (activeSet[i] > 0 && lambda[i] < worstLambda)
                        {
                            worstLambda = lambda[i];
                            worstIdx = i;
                        }
                    }

                    if (worstIdx < 0 || worstLambda > -eps)
                    {
                        // Optimal solution found
                        return x;
                    }

                    // Remove most negative multiplier from active set
                    activeSet[worstIdx] = 0;
                    continue;
                }

                // Step 3: Line search - find maximum step that maintains feasibility
                double alpha = 1.0;
                int blockingIdx = -1;
                int blockingType = 0;

                for (int i = 0; i < n; i++)
                {
                    if (activeSet[i] > 0) continue;  // Already active

                    if (p[i] < -eps)
                    {
                        // Moving towards lower bound
                        double alphaToBound = (lower[i] - x[i]) / p[i];
                        if (alphaToBound < alpha)
                        {
                            alpha = alphaToBound;
                            blockingIdx = i;
                            blockingType = 1;
                        }
                    }
                    else if (p[i] > eps)
                    {
                        // Moving towards upper bound
                        double alphaToBound = (upper[i] - x[i]) / p[i];
                        if (alphaToBound < alpha)
                        {
                            alpha = alphaToBound;
                            blockingIdx = i;
                            blockingType = 2;
                        }
                    }
                }

                // Step 4: Take step
                for (int i = 0; i < n; i++)
                {
                    x[i] += alpha * p[i];
                }

                // Step 5: Add blocking constraint to active set (only if we made meaningful progress)
                if (blockingIdx >= 0 && alpha > eps)
                {
                    activeSet[blockingIdx] = blockingType;
                }
                else if (alpha <= eps)
                {
                    // No progress made - numerical issue or degenerate problem
                    break;
                }
            }

            return x;
        }

        /// <summary>
        /// Solves equality-constrained QP: min 0.5*x^T*H*x + c^T*x subject to A_eq*x = b_eq and active bound constraints
        /// Returns (search_direction, lagrange_multipliers)
        /// </summary>
        private static (double[], double[]) SolveEqualityConstrainedQP(
            double[,] H,
            double[] c,
            double[,] A_eq,
            double[] b_eq,
            int[] activeSet,
            double[] lower,
            double[] upper,
            double[] x,
            int n,
            int m_eq,
            double eps)
        {
            // Count free variables
            var freeVars = new List<int>();
            var activeVars = new List<int>();
            for (int i = 0; i < n; i++)
            {
                if (activeSet[i] == 0)
                    freeVars.Add(i);
                else
                    activeVars.Add(i);
            }

            int nFree = freeVars.Count;
            int nActive = activeVars.Count;

            if (nFree == 0)
            {
                // All variables fixed - compute Lagrange multipliers for active bounds

                var grad = new double[n];
                for (int i = 0; i < n; i++)
                {
                    grad[i] = c[i];
                    for (int j = 0; j < n; j++)
                    {
                        grad[i] += H[i, j] * x[j];
                    }
                }

                var pZero = new double[n];  // Zero direction
                var lambdaActive = new double[n];

                // Lagrange multiplier for active bound = gradient component
                for (int i = 0; i < nActive; i++)
                {
                    int ii = activeVars[i];
                    lambdaActive[ii] = grad[ii];
                }

                return (pZero, lambdaActive);
            }

            // Build reduced system for free variables only
            // KKT system: [H_f   A_f^T] [p_f  ]   [-g_f]
            //             [A_f   0    ] [lambda] = [r   ]
            //
            // where g_f = H_f*x_f + c_f (gradient w.r.t. free vars)
            //       r = b_eq - A_eq*x (constraint residual)

            int systemSize = nFree + m_eq;
            var KKT = new double[systemSize, systemSize];
            var rhs = new double[systemSize];

            // Compute g_f = H*x + c (full gradient), then extract free part
            var g = new double[n];
            for (int i = 0; i < n; i++)
            {
                g[i] = c[i];
                for (int j = 0; j < n; j++)
                {
                    g[i] += H[i, j] * x[j];
                }
            }

            // Build H_f (free × free submatrix of H)
            for (int i = 0; i < nFree; i++)
            {
                int ii = freeVars[i];
                rhs[i] = -g[ii];  // -g_f

                for (int j = 0; j < nFree; j++)
                {
                    int jj = freeVars[j];
                    KKT[i, j] = H[ii, jj];
                }
            }

            // Build A_f (m_eq × nFree submatrix of A_eq)
            for (int i = 0; i < m_eq; i++)
            {
                // Compute constraint residual: r_i = b_eq[i] - A_eq[i,:]*x
                double residual = b_eq[i];
                for (int j = 0; j < n; j++)
                {
                    residual -= A_eq[i, j] * x[j];
                }
                rhs[nFree + i] = residual;

                for (int j = 0; j < nFree; j++)
                {
                    int jj = freeVars[j];
                    KKT[nFree + i, j] = A_eq[i, jj];  // A_f (lower-left block)
                    KKT[j, nFree + i] = A_eq[i, jj];  // A_f^T (upper-right block)
                }
            }

            // Solve KKT system
            var solution = SolveLinearSystem(KKT, rhs, eps);
            if (solution == null)
            {
                return (null, null);
            }

            // Extract search direction (free variables)
            var p = new double[n];
            for (int i = 0; i < nFree; i++)
            {
                p[freeVars[i]] = solution[i];
            }

            // Compute Lagrange multipliers for active bounds
            // lambda_i = g_i - H_i,free * p_free for active variables
            var lambda = new double[n];
            for (int i = 0; i < nActive; i++)
            {
                int ii = activeVars[i];
                lambda[ii] = g[ii];
                for (int j = 0; j < nFree; j++)
                {
                    int jj = freeVars[j];
                    lambda[ii] -= H[ii, jj] * p[jj];
                }
            }

            return (p, lambda);
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
