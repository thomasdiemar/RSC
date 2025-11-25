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
        private const double EqualityTolerance = 1e-12;
        private const double SoftZeroTolerance = 1e-8;

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
            var best = SolveExhaustively(coefficients, constants);
            yield return new MyProgress<double[]>
            {
                Result = best,
                Done = true
            };
        }

        private double[] SolveExhaustively(double[,] coefficients, double[] constants)
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
            int cols = coefficients.GetLength(1);
            if (constants.Length != rows)
            {
                throw new ArgumentException("Constants vector length must match coefficient rows.", nameof(constants));
            }

            const int granularity = 4; // 0,0.25,0.5,0.75,1 to allow finer adjustments than the original {0,0.5,1}
            double[] levels = Enumerable.Range(0, granularity + 1)
                .Select(i => i / (double)granularity)
                .ToArray();
            double[] best = null;
            double[] bestScores = null;
            int[] priorityOrder = BuildPriorityOrder(constants);

            var current = new double[cols];
            Explore(0);
            return best ?? new double[cols];

            void Explore(int index)
            {
                if (index == cols)
                {
                    EvaluateCandidate();
                    return;
                }

                for (int i = 0; i < levels.Length; i++)
                {
                    current[index] = levels[i];
                    Explore(index + 1);
                }
            }

            void EvaluateCandidate()
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

                for (int r = 0; r < rows; r++)
                {
                    if (double.IsNaN(constants[r]))
                    {
                        continue;
                    }

                    if (Math.Abs(constants[r]) < SoftZeroTolerance && Math.Abs(rowValues[r]) > 1e-6)
                    {
                        return;
                    }
                }

                double[] scores = new double[rows];
                for (int r = 0; r < rows; r++)
                {
                    double desired = constants[r];
                    if (double.IsNaN(desired))
                    {
                        scores[r] = -Math.Abs(rowValues[r]);
                    }
                    else if (desired > 0)
                    {
                        scores[r] = rowValues[r];
                    }
                    else if (desired < 0)
                    {
                        scores[r] = -rowValues[r];
                    }
                    else
                    {
                        scores[r] = -Math.Abs(rowValues[r]);
                    }
                }

                if (best == null || LexicographicallyBetter(scores, bestScores))
                {
                    best = (double[])current.Clone();
                    bestScores = scores;
                }
            }

            bool LexicographicallyBetter(double[] candidate, double[] incumbent)
            {
                for (int pi = 0; pi < priorityOrder.Length; pi++)
                {
                    int idx = priorityOrder[pi];
                    if (Math.Abs(candidate[idx] - incumbent[idx]) < 1e-9)
                    {
                        continue;
                    }

                    return candidate[idx] > incumbent[idx];
                }

                return false;
            }
        }

        private static int[] BuildPriorityOrder(double[] constants)
        {
            var primary = new List<int>();
            var equalities = new List<int>();
            var softZeros = new List<int>();

            for (int i = 0; i < constants.Length; i++)
            {
                if (double.IsNaN(constants[i]))
                {
                    softZeros.Add(i);
                }
                else if (Math.Abs(constants[i]) < SoftZeroTolerance)
                {
                    equalities.Add(i);
                }
                else
                {
                    primary.Add(i);
                }
            }

            var order = new int[constants.Length];
            int idx = 0;
            foreach (var i in primary) order[idx++] = i;
            foreach (var i in equalities) order[idx++] = i;
            foreach (var i in softZeros) order[idx++] = i;
            return order;
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
