using System;
using System.Collections.Generic;
using System.Linq;
using LinearSolver;
using LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Model;

//codex resume 019aad7c-0210-76f0-bafb-7c4851ccb64f
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
        public virtual IEnumerable<MyProgress<Fraction[]>> Solve(Fraction[,] coefficients, Fraction[] constants)
        {
            if (coefficients == null) throw new ArgumentNullException(nameof(coefficients));
            if (constants == null) throw new ArgumentNullException(nameof(constants));

            int rows = coefficients.GetLength(0);
            int cols = coefficients.GetLength(1);
            if (constants.Length != rows)
            {
                throw new ArgumentException("Constants vector length must match coefficient rows.", nameof(constants));
            }

            var tableau = BuildTableau(coefficients, constants);
            int columnCount = tableau.ColumnCount;

            foreach (var snapshot in SolveTableauProgressively(tableau))
            {
                var output = new Fraction[columnCount];
                var stageSolution = snapshot.Result?.StageResults?.LastOrDefault()?.Solution;
                if (stageSolution != null)
                {
                    for (int i = 0; i < Math.Min(columnCount, stageSolution.Count); i++)
                    {
                        output[i] = stageSolution[i].Value;
                    }
                }

                yield return new MyProgress<Fraction[]>
                {
                    Result = output,
                    Done = snapshot.Done
                };
            }
        }

        private PreEmptiveIntegerTableau BuildTableau(Fraction[,] coefficients, Fraction[] constants)
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

        private static Fraction ToFraction(Fraction value)
        {
            return value;
            //const int scale = 1000000;
            //if (double.IsNaN(value))
            //{
            //    return new Fraction(0);
            //}

            //if (double.IsInfinity(value))
            //{
            //    throw new ArgumentOutOfRangeException(nameof(value), "Coefficients and constants must be finite.");
            //}

            //return new Fraction((int)Math.Round(value * scale), scale);
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
