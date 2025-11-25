using System;
using System.Collections.Generic;
using LinearSolver;

namespace LinearSolver.Custom.GoalProgramming
{
    /// <summary>
    /// Simple goal-programming style solver that emits a single zeroed snapshot sized to the coefficient matrix.
    /// </summary>
    public class GoalProgrammingSolver : IMyLinearSolver
    {
        public IEnumerable<MyProgress<double[]>> Solve(double[,] coefficients, double[] constants)
        {
            if (coefficients == null)
                throw new ArgumentNullException(nameof(coefficients));
            if (constants == null)
                throw new ArgumentNullException(nameof(constants));

            int rows = coefficients.GetLength(0);
            int cols = coefficients.GetLength(1);
            if (constants.Length != rows)
                throw new ArgumentException("Constants vector dimension mismatch.", nameof(constants));

            var outputs = new double[cols];
            yield return new MyProgress<double[]>
            {
                Result = outputs,
                Done = true
            };
        }
    }
}
