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

            for (int row = 0; row < rows; row++)
            {
                Term lhs = 0;
                for (int col = 0; col < cols; col++)
                    lhs += coefficients[row, col] * decisions[col];

                if (constants[row] > 0)
                {
                    System.Diagnostics.Debug.WriteLine("Adding Max Goal " + lhs.ToString());
                    var goal = model.AddGoal($"Max_{row}", GoalKind.Maximize, lhs);
                    //goal.Order = 2;
                }
                else if (constants[row] < 0)
                {
                    System.Diagnostics.Debug.WriteLine("Adding Min Goal " + lhs.ToString());
                    var goal = model.AddGoal($"Min_{row}", GoalKind.Minimize,lhs );
                    //goal.Order = 2;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Adding Min(c) Goal " + lhs.ToString());
                    // For zero constants, we can add a constraint to keep it balanced
                    model.AddConstraint($"Eq_{row}", lhs == constants[row]);
                    //var goal = model.AddGoal($"Min_{row}", GoalKind.Minimize, 0);
                    //goal.Order = 1;

                }
            }
            //model.AddGoal("MinZero", GoalKind.Minimize, 0);
            context.Solve();

            var result = new double[cols];
            for (int i = 0; i < cols; i++)
                result[i] = decisions[i].ToDouble();

            return result;
        }
    }

}
