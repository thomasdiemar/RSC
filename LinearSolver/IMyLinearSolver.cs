using System;
using System.Collections.Generic;

namespace LinearSolver
{
    public interface IMyLinearSolver
    {
        /// <summary>
        /// Solve a bounded linear system and emit progress snapshots of the current solution.
        /// </summary>
        IEnumerable<MyProgress<double[]>> Solve(double[,] coefficients, double[] constants);
    }
}
