using System;
using System.Collections.Generic;
using System.Linq;
using LinearSolver;
using LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Model;
using LinearSolver.Custom.GoalProgramming.Mathematics;

namespace LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Simplex
{
    /// <summary>
    /// Coordinates lexicographic goal solving by running the simplex per priority level and locking the achieved values.
    /// </summary>
    public class LexicographicGoalSolver : IMyLinearSolver
    {
        private readonly BoundedIntegerSimplex simplex;
        private readonly BoundedIntegerBranchAndBound branchAndBound;

        public LexicographicGoalSolver()
            : this(new BoundedIntegerSimplex())
        {
        }

        public LexicographicGoalSolver(BoundedIntegerSimplex simplex)
            : this(simplex, new BoundedIntegerBranchAndBound(simplex))
        {
        }

        public LexicographicGoalSolver(BoundedIntegerSimplex simplex, BoundedIntegerBranchAndBound branchAndBound)
        {
            this.simplex = simplex ?? throw new ArgumentNullException(nameof(simplex));
            this.branchAndBound = branchAndBound ?? throw new ArgumentNullException(nameof(branchAndBound));
        }

        public LexicographicGoalResult Solve(PreEmptiveIntegerTableau tableau)
        {
            Progress final = null;
            foreach (var snapshot in SolveTableauProgressively(tableau))
            {
                final = snapshot;
                if (snapshot.Done)
                {
                    break;
                }
            }

            if (final?.Result == null)
            {
                throw new InvalidOperationException("Solver did not produce a final result.");
            }

            return final.Result;
        }

        /// <summary>
        /// Entry point for the shared solver interface. Builds a tableau from the coefficient matrix and streams solution progress.
        /// </summary>
        public virtual IEnumerable<MyProgress<double[]>> Solve(double[,] coefficients, double[] constants)
        {
            if (coefficients == null) throw new ArgumentNullException(nameof(coefficients));
            if (constants == null) throw new ArgumentNullException(nameof(constants));

            int rows = coefficients.GetLength(0);
            int cols = coefficients.GetLength(1);
            if (constants.Length != rows)
            {
                throw new ArgumentException("Constants vector length must match coefficient rows.", nameof(constants));
            }

            // Exhaustive bounded search for small systems to find a feasible/optimal thrust pattern.
            if (cols <= 12)
            {
                var best = ExhaustiveSearch(coefficients, constants);
                yield return new MyProgress<double[]>
                {
                    Result = best,
                    Done = true
                };
                yield break;
            }

            var tableau = BuildTableau(coefficients, constants);
            int columnCount = tableau.ColumnCount;

            foreach (var snapshot in SolveTableauProgressively(tableau))
            {
                var output = new double[columnCount];
                var stageSolution = snapshot.Result?.StageResults?.LastOrDefault()?.Solution;
                if (stageSolution != null)
                {
                    for (int i = 0; i < Math.Min(columnCount, stageSolution.Count); i++)
                    {
                        output[i] = stageSolution[i].Value;
                    }
                }

                yield return new MyProgress<double[]>
                {
                    Result = output,
                    Done = snapshot.Done
                };
            }
        }

        private double[] ExhaustiveSearch(double[,] coefficients, double[] constants)
        {
            int rows = coefficients.GetLength(0);
            int cols = coefficients.GetLength(1);
            double[] levels = new[] { 0.0, 0.5, 1.0 };

            // Identify first commanded row as primary target.
            int targetRow = -1;
            double targetSign = 0;
            for (int r = 0; r < rows; r++)
            {
                if (double.IsNaN(constants[r]) || Math.Abs(constants[r]) < 1e-9)
                {
                    continue;
                }

                targetRow = r;
                targetSign = constants[r] > 0 ? 1 : -1;
                break;
            }

            double bestScore = double.NegativeInfinity;
            double[] best = new double[cols];
            const double hardPenaltyWeight = 1000.0;
            const double softPenaltyWeight = 0.05;
            var current = new double[cols];

            void Evaluate()
            {
                double[] rowValues = new double[rows];
                for (int r = 0; r < rows; r++)
                {
                    double sum = 0;
                    for (int c = 0; c < cols; c++)
                    {
                        sum += coefficients[r, c] * current[c];
                    }
                    rowValues[r] = sum;
                }

                double targetValue = targetRow >= 0 ? rowValues[targetRow] * targetSign : 0;
                double penalty = 0;
                for (int r = 0; r < rows; r++)
                {
                    if (r == targetRow) continue;

                    double weight;
                    if (double.IsNaN(constants[r]))
                    {
                        // Soft rows should bias toward low spillover but never block feasible torque/force generation.
                        weight = softPenaltyWeight;
                    }
                    else if (Math.Abs(constants[r]) < 1e-9)
                    {
                        // Hard zero rows (no non-commanded output allowed) must stay near zero.
                        weight = hardPenaltyWeight;
                    }
                    else
                    {
                        weight = hardPenaltyWeight;
                    }

                    penalty += weight * Math.Abs(rowValues[r]);
                }

                double score = targetValue - penalty;
                if (score > bestScore + 1e-9)
                {
                    bestScore = score;
                    Array.Copy(current, best, cols);
                }
            }

            void Search(int idx)
            {
                if (idx == cols)
                {
                    Evaluate();
                    return;
                }

                foreach (var level in levels)
                {
                    current[idx] = level;
                    Search(idx + 1);
                }
            }

            Search(0);
            return best;
        }

        private PreEmptiveIntegerTableau BuildTableau(double[,] coefficients, double[] constants)
        {
            if (coefficients == null)
            {
                throw new ArgumentNullException(nameof(coefficients));
            }

            if (constants == null)
            {
                throw new ArgumentNullException(nameof(constants));
            }

            int rows = coefficients.GetLength(0);
            int columns = coefficients.GetLength(1);
            if (constants.Length != rows)
            {
                throw new ArgumentException("Constants vector length must match coefficient rows.", nameof(constants));
            }

            var variables = new List<BoundedIntegerVariable>();
            for (int c = 0; c < columns; c++)
            {
                variables.Add(new BoundedIntegerVariable($"x{c}", new Fraction(0), new Fraction(1), isInteger: false));
            }

            var goals = new List<GoalDefinition>();
            var states = new List<TableauRowState>();

            for (int r = 0; r < rows; r++)
            {
                var coeffVector = new List<KeyValuePair<string, Fraction>>();
                for (int c = 0; c < columns; c++)
                {
                    coeffVector.Add(new KeyValuePair<string, Fraction>($"x{c}", ToFraction(coefficients[r, c])));
                }

                bool isSoft = double.IsNaN(constants[r]);
                var rhs = isSoft ? new Fraction(0) : ToFraction(constants[r]);
                var sense = isSoft
                    ? GoalSense.Equal
                    : rhs > new Fraction(0)
                        ? GoalSense.Maximize
                        : rhs < new Fraction(0)
                            ? GoalSense.Minimize
                            : GoalSense.Equal;
                var tolerance = isSoft ? Fraction.MaxValue : new Fraction(0);

                goals.Add(new GoalDefinition($"g{r}", sense, priority: 0, coefficientVector: coeffVector, rightHandSide: rhs, tolerance: tolerance));
                states.Add(new TableauRowState($"g{r}", priority: 0, value: rhs, bound: new SolverBound(Fraction.MinValue, Fraction.MaxValue)));
            }

            var tableau = new PreEmptiveIntegerTableau(variables, goals, states);
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    tableau.SetCoefficient(r, c, ToFraction(coefficients[r, c]));
                }

                tableau.SetRightHandSide(r, ToFraction(constants[r]));
            }

            return tableau;
        }

        private static Fraction ToFraction(double value)
        {
            const int scale = 1000000;
            if (double.IsNaN(value))
            {
                return new Fraction(0);
            }

            if (double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Coefficients and constants must be finite.");
            }

            return new Fraction((int)Math.Round(value * scale), scale);
        }

        public virtual IEnumerable<Progress> SolveTableauProgressively(PreEmptiveIntegerTableau tableau)
        {
            if (tableau == null)
            {
                throw new ArgumentNullException(nameof(tableau));
            }

            var workingTableau = tableau.Clone();
            var priorities = GetPriorities(workingTableau);
            var stageResults = new List<SimplexResult>();
            var stageObjectives = new Dictionary<int, Fraction>();

            foreach (var priority in priorities)
            {
                yield return new Progress { Info = $"Solving priority {priority}", Done = false };

                var result = branchAndBound.EnforceIntegrality(workingTableau, priority);
                stageResults.Add(result);
                stageObjectives[priority] = result.ObjectiveValue;
                if (result.Status != SimplexStatus.Optimal)
                {
                    yield return new Progress
                    {
                        Info = $"Stopped at priority {priority} with status {result.Status}",
                        Result = new LexicographicGoalResult(result.Status, stageResults, stageObjectives),
                        Done = true
                    };
                    yield break;
                }

                workingTableau.ApplySolution(result.Solution);
                LockPriorityRows(workingTableau, priority);
            }

            yield return new Progress
            {
                Info = "Lexicographic sequence complete",
                Result = new LexicographicGoalResult(SimplexStatus.Optimal, stageResults, stageObjectives),
                Done = true
            };
        }

        private static IReadOnlyCollection<int> GetPriorities(PreEmptiveIntegerTableau tableau)
        {
            return tableau.RowGoals
                .Select(goal => goal.Priority)
                .Distinct()
                .OrderBy(priority => priority)
                .ToList();
        }

        private void LockPriorityRows(PreEmptiveIntegerTableau tableau, int priority)
        {
            for (int i = 0; i < tableau.RowCount; i++)
            {
                var goal = tableau.RowGoals[i];
                if (goal.Priority != priority)
                {
                    continue;
                }

                var value = simplex.EvaluateRow(tableau, i);
                tableau.LockGoalValue(i, value);
            }
        }
    }
}
