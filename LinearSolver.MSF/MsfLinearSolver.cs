using System;
using Microsoft.SolverFoundation.Services;
using LinearSolver;

namespace LinearSolver.MSF
{
    public class MsfLinearSolver : IMyLinearSolver
    {
        public double[] Solve(double[,] coefficients, double[] constants)
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
    }

    public class MsfGoalLinearSolver : IMyLinearSolver
    {
        private const double SoftZeroWeight = 1.0;
        private const double RegularizationWeight = 1.0;

        public double[] Solve(double[,] coefficients, double[] constants)
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
    }

}
