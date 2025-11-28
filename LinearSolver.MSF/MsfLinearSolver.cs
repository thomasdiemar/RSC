using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;
using LinearSolver;

namespace LinearSolver.MSF
{
    public class MsfLinearSolver : IMyLinearSolver
    {
        /// <summary>
        /// Solve the linear system as hard equalities with bounded decisions.
        /// </summary>
        private double[] SolveCore(double[,] coefficients, double[] constants)
        {
            if (coefficients == null)
                throw new ArgumentNullException(nameof(coefficients));
            if (constants == null)
                throw new ArgumentNullException(nameof(constants));

            int rows = coefficients.GetLength(0);
            int cols = coefficients.GetLength(1);

            if (constants.Length != rows)
                throw new ArgumentException("Constants vector dimension mismatch.", nameof(constants));

            var context = SolverContext.GetContext();
            context.ClearModel();
            var model = context.CreateModel();
            Term objective = 0;

            Decision[] decisions = new Decision[cols];
            for (int i = 0; i < cols; i++)
            {
                decisions[i] = new Decision(Domain.RealRange(0, 1), $"x{i}");
                model.AddDecision(decisions[i]);
            }

            for (int row = 0; row < rows; row++)
            {
                Term lhs = 0;
                for (int col = 0; col < cols; col++)
                    lhs += coefficients[row, col] * decisions[col];
                model.AddConstraint($"Eq_{row}", lhs == constants[row]);
            }

            model.AddGoal("MinZero", GoalKind.Minimize, 0);
            context.Solve();

            var result = new double[cols];
            for (int i = 0; i < cols; i++)
                result[i] = decisions[i].ToDouble();

            return result;
        }

        /// <summary>
        /// Emit setup, constraint, and final snapshots for the hard-equality solver.
        /// </summary>
        public IEnumerable<MyProgress<Fraction[]>> Solve(Fraction[,] coefficients, Fraction[] constants)
        {
            if (coefficients == null)
                throw new ArgumentNullException(nameof(coefficients));
            if (constants == null)
                throw new ArgumentNullException(nameof(constants));

            int rows = coefficients.GetLength(0);
            int cols = coefficients.GetLength(1);

            if (constants.Length != rows)
                throw new ArgumentException("Constants vector dimension mismatch.", nameof(constants));

            var context = SolverContext.GetContext();
            context.ClearModel();
            var model = context.CreateModel();

            Decision[] decisions = new Decision[cols];
            for (int i = 0; i < cols; i++)
            {
                decisions[i] = new Decision(Domain.RealRange(0, 1), $"x{i}");
                model.AddDecision(decisions[i]);
                yield return MsfProgressFactory.CreateProgress(MsfProgressFactory.Project(decisions, solved: false));
            }

            for (int row = 0; row < rows; row++)
            {
                Term lhs = 0;
                for (int col = 0; col < cols; col++)
                    lhs += coefficients[row, col].ToDouble() * decisions[col];
                model.AddConstraint($"Eq_{row}", lhs == constants[row].ToDouble());
                yield return MsfProgressFactory.CreateProgress(MsfProgressFactory.Project(decisions, solved: false));
            }

            model.AddGoal("MinZero", GoalKind.Minimize, 0);
            yield return MsfProgressFactory.CreateProgress(MsfProgressFactory.Project(decisions, solved: false));

            context.Solve();

            var solvedResultProgress = MsfProgressFactory.Project(decisions, solved: true);
            yield return MsfProgressFactory.CreateProgress(solvedResultProgress);
        }
    }

    public class MsfGoalLinearSolver : IMyLinearSolver
    {
        private const double SoftZeroWeight = 1.0;
        private const double RegularizationWeight = 1.0;

        /// <summary>
        /// Solve the goal-based system with soft zeros and lexicographic goals.
        /// </summary>
        private double[] SolveCore(double[,] coefficients, double[] constants)
        {
            if (coefficients == null)
                throw new ArgumentNullException(nameof(coefficients));
            if (constants == null)
                throw new ArgumentNullException(nameof(constants));

            int rows = coefficients.GetLength(0);
            int cols = coefficients.GetLength(1);

            if (constants.Length != rows)
                throw new ArgumentException("Constants vector dimension mismatch.", nameof(constants));

            var context = SolverContext.GetContext();
            context.ClearModel();
            var model = context.CreateModel();

            Decision[] decisions = new Decision[cols];
            for (int i = 0; i < cols; i++)
            {
                decisions[i] = new Decision(Domain.RealRange(0, 1), $"x{i}");
                model.AddDecision(decisions[i]);
            }

            // Primary goals
            for (int row = 0; row < rows; row++)
            {
                Term lhs = 0;
                for (int col = 0; col < cols; col++)
                    lhs += coefficients[row, col] * decisions[col];

                if (double.IsNaN(constants[row]))
                    continue;

                if (constants[row] > 0)
                    model.AddGoal($"Max_{row}", GoalKind.Maximize, lhs);
                else if (constants[row] < 0)
                    model.AddGoal($"Min_{row}", GoalKind.Minimize, lhs);
                else
                    model.AddConstraint($"Eq_{row}", lhs == constants[row]);
            }

            // Soft-zero goals via absolute value auxiliary variables.
            for (int row = 0; row < rows; row++)
            {
                if (!double.IsNaN(constants[row]))
                    continue;

                Term lhs = 0;
                for (int col = 0; col < cols; col++)
                    lhs += coefficients[row, col] * decisions[col];

                var absVar = new Decision(Domain.RealNonnegative, $"abs_{row}");
                model.AddDecision(absVar);
                model.AddConstraint($"AbsPos_{row}", absVar >= lhs);
                model.AddConstraint($"AbsNeg_{row}", absVar >= -lhs);
                model.AddGoal($"SoftZero_{row}", GoalKind.Minimize, SoftZeroWeight * absVar);
            }

            // Mild bias toward lower overall thrust usage.
            Term totalThrust = 0;
            for (int i = 0; i < cols; i++)
                totalThrust += decisions[i];
            model.AddGoal("Reg_TotalThrust", GoalKind.Minimize, RegularizationWeight * totalThrust);
            context.Solve();

            var result = new double[cols];
            for (int i = 0; i < cols; i++)
                result[i] = decisions[i].ToDouble();

            return result;
        }

        /// <summary>
        /// Emit granular snapshots during goal setup and after solving.
        /// </summary>
        public IEnumerable<MyProgress<Fraction[]>> Solve(Fraction[,] coefficients, Fraction[] constants)
        {
            if (coefficients == null)
                throw new ArgumentNullException(nameof(coefficients));
            if (constants == null)
                throw new ArgumentNullException(nameof(constants));

            int rows = coefficients.GetLength(0);
            int cols = coefficients.GetLength(1);

            if (constants.Length != rows)
                throw new ArgumentException("Constants vector dimension mismatch.", nameof(constants));

            var context = SolverContext.GetContext();
            context.ClearModel();
            var model = context.CreateModel();

            Decision[] decisions = new Decision[cols];
            for (int i = 0; i < cols; i++)
            {
                decisions[i] = new Decision(Domain.RealRange(0, 1), $"x{i}");
                model.AddDecision(decisions[i]);
            }

            // Initial snapshot (all zeros).
            yield return Snapshot(decisions, solved: false);

            // Primary goals
            for (int row = 0; row < rows; row++)
            {
                Term lhs = 0;
                for (int col = 0; col < cols; col++)
                    lhs += coefficients[row, col].ToDouble() * decisions[col];

                if (double.IsNaN(constants[row]))
                    continue;

                if (constants[row] > 0)
                    model.AddGoal($"Max_{row}", GoalKind.Maximize, lhs);
                else if (constants[row] < 0)
                    model.AddGoal($"Min_{row}", GoalKind.Minimize, lhs);
                else
                    model.AddConstraint($"Eq_{row}", lhs == constants[row].ToDouble());

                yield return Snapshot(decisions, solved: false);
            }

            // Soft-zero goals via absolute value auxiliary variables.
            for (int row = 0; row < rows; row++)
            {
                if (!double.IsNaN(constants[row]))
                    continue;

                Term lhs = 0;
                for (int col = 0; col < cols; col++)
                    lhs += coefficients[row, col].ToDouble() * decisions[col];

                var absVar = new Decision(Domain.RealNonnegative, $"abs_{row}");
                model.AddDecision(absVar);
                model.AddConstraint($"AbsPos_{row}", absVar >= lhs);
                model.AddConstraint($"AbsNeg_{row}", absVar >= -lhs);
                model.AddGoal($"SoftZero_{row}", GoalKind.Minimize, SoftZeroWeight * absVar);

                yield return Snapshot(decisions, solved: false);
            }

            // Mild bias toward lower overall thrust usage.
            Term totalThrust = 0;
            for (int i = 0; i < cols; i++)
                totalThrust += decisions[i];
            model.AddGoal("Reg_TotalThrust", GoalKind.Minimize, RegularizationWeight * totalThrust);

            // Snapshot before solve with all goals added.
            yield return Snapshot(decisions, solved: false);

            context.Solve();

            var final = new Fraction[cols];
            for (int i = 0; i < cols; i++)
                final[i] = new Fraction((decimal)decisions[i].ToDouble(), 1);

            yield return MsfProgressFactory.CreateProgress(final);
        }

        private static MyProgress<Fraction[]> Snapshot(Decision[] decisions, bool solved)
        {
            var values = MsfProgressFactory.Project(decisions, solved);

            return MsfProgressFactory.CreateProgress(values);
        }
    }

    internal static class MsfProgressFactory
    {
        public static LinearSolver.MyProgress<Fraction[]> CreateProgress(Fraction[] snapshot)
        {
            return new LinearSolver.MyProgress<Fraction[]>
            {
                Result = (Fraction[])snapshot.Clone(),
                Done = true
            };
        }

        public static Fraction[] Project(Decision[] decisions, bool solved)
        {
            var values = new Fraction[decisions.Length];
            if (solved)
            {
                for (int i = 0; i < decisions.Length; i++)
                    values[i] = new Fraction((decimal)decisions[i].ToDouble(), 1);
            }
            else
            {
                for (int i = 0; i < decisions.Length; i++)
                    values[i] = 0;
            }
            return values;
        }
    }
}
