//using System;
//using System.Collections.Generic;
//using LinearSolver;

//namespace LinearSolver.Custom
//{
//    /// <summary>
//    /// Goal-style solver that mirrors the behaviour of the MSF goal solver:
//    /// - Rows with a requested value of 0 become hard equality constraints.
//    /// - Positive rows are maximised, negative rows are minimised (by maximising the negated row).
//    /// A small quadratic penalty is used to pick the minimal-norm solution when multiple optima exist.
//    /// </summary>
//    public class CustomLinearSolver : IMyLinearSolver
//    {
//        private const double EqualityTolerance = 1e-12;
//        private const double BoundTolerance = 1e-9;
//        private const double Regularisation = 0.0;

//        /// <summary>
//        /// Solve using projected gradient descent with weighted least squares, snapping to hard equalities.
//        /// </summary>
//        private double[] SolveCore(double[,] coefficients, double[] constants)
//        {
//            if (coefficients == null)
//                throw new ArgumentNullException(nameof(coefficients));
//            if (constants == null)
//                throw new ArgumentNullException(nameof(constants));

//            int rows = coefficients.GetLength(0);
//            int cols = coefficients.GetLength(1);

//            if (constants.Length != rows)
//                throw new ArgumentException("Constants vector dimension mismatch.", nameof(constants));

//            double[] targets = (double[])constants.Clone();
//            double[] weights = new double[rows];
//            for (int i = 0; i < rows; i++)
//            {
//                weights[i] = Math.Abs(targets[i]) < EqualityTolerance ? 500.0 : 1.0;
//            }

//            return SolveByProjectedGradient(coefficients, targets, weights);
//        }

//        /// <summary>
//        /// Stream progress snapshots for the iterative solve, ending with a post-projection solution.
//        /// </summary>
//        public IEnumerable<MyProgress<double[]>> Solve(double[,] coefficients, double[] constants)
//        {
//            if (coefficients == null)
//                throw new ArgumentNullException(nameof(coefficients));
//            if (constants == null)
//                throw new ArgumentNullException(nameof(constants));

//            int rows = coefficients.GetLength(0);
//            int cols = coefficients.GetLength(1);
//            if (constants.Length != rows)
//                throw new ArgumentException("Constants vector dimension mismatch.", nameof(constants));

//            double[] targets = (double[])constants.Clone();
//            double[] weights = new double[rows];
//            for (int i = 0; i < rows; i++)
//                weights[i] = Math.Abs(targets[i]) < EqualityTolerance ? 500.0 : 1.0;

//            double[] solution = new double[cols];
//            double[] gradient = new double[cols];
//            var equalityMatrix = BuildEqualityMatrix(coefficients, targets);

//            yield return CreateProgress(solution);

//            const int maxIterations = 100000;
//            const double step = 0.0002;
//            for (int iteration = 0; iteration < maxIterations; iteration++)
//            {
//                Array.Clear(gradient, 0, gradient.Length);

//                for (int row = 0; row < rows; row++)
//                {
//                    double dot = 0;
//                    for (int col = 0; col < cols; col++)
//                        dot += coefficients[row, col] * solution[col];

//                    double error = dot - targets[row];
//                    double weight = weights[row];

//                    for (int col = 0; col < cols; col++)
//                        gradient[col] += weight * error * coefficients[row, col];
//                }

//                double maxDelta = 0;
//                for (int col = 0; col < cols; col++)
//                {
//                    double g = 2 * gradient[col] + 2 * Regularisation * solution[col];
//                    double newValue = solution[col] - step * g;

//                    if (newValue < 0) newValue = 0;
//                    else if (newValue > 1) newValue = 1;

//                    double delta = Math.Abs(newValue - solution[col]);
//                    if (delta > maxDelta)
//                        maxDelta = delta;

//                    solution[col] = newValue;
//                }

//                if (equalityMatrix.GetLength(0) > 0)
//                {
//                    ProjectToEqualities(equalityMatrix, solution);
//                    for (int i = 0; i < solution.Length; i++)
//                    {
//                        if (solution[i] < 0) solution[i] = 0;
//                        else if (solution[i] > 1) solution[i] = 1;
//                        solution[i] = SnapToCommonValues(solution[i]);
//                    }
//                }

//                yield return CreateProgress(solution);

//                if (maxDelta < 1e-8)
//                    break;
//            }

//            // Final projection to satisfy equalities and clamp/snap.
//            if (equalityMatrix.GetLength(0) > 0)
//            {
//                ProjectToEqualities(equalityMatrix, solution);
//                for (int i = 0; i < solution.Length; i++)
//                {
//                    if (solution[i] < 0) solution[i] = 0;
//                    else if (solution[i] > 1) solution[i] = 1;
//                    solution[i] = SnapToCommonValues(solution[i]);
//                }
//            }

//            yield return CreateProgress(solution);
//        }

//        private static MyProgress<double[]> CreateProgress(double[] snapshot)
//        {
//            return new MyProgress<double[]>
//            {
//                Result = (double[])snapshot.Clone(),
//                Done = true
//            };
//        }

//        private static double[] SolveByProjectedGradient(double[,] coefficients, double[] targets, double[] weights)
//        {
//            int rows = coefficients.GetLength(0);
//            int cols = coefficients.GetLength(1);
//            double[] solution = new double[cols];
//            double[] gradient = new double[cols];

//            const int maxIterations = 100000;
//            const double step = 0.0002;
//            for (int iteration = 0; iteration < maxIterations; iteration++)
//            {
//                Array.Clear(gradient, 0, gradient.Length);

//                for (int row = 0; row < rows; row++)
//                {
//                    double dot = 0;
//                    for (int col = 0; col < cols; col++)
//                        dot += coefficients[row, col] * solution[col];

//                    double error = dot - targets[row];
//                    double weight = weights[row];

//                    for (int col = 0; col < cols; col++)
//                        gradient[col] += weight * error * coefficients[row, col];
//                }

//                double maxDelta = 0;
//                for (int col = 0; col < cols; col++)
//                {
//                    double g = 2 * gradient[col] + 2 * Regularisation * solution[col];
//                    double newValue = solution[col] - step * g;

//                    if (newValue < 0) newValue = 0;
//                    else if (newValue > 1) newValue = 1;

//                    double delta = Math.Abs(newValue - solution[col]);
//                    if (delta > maxDelta)
//                        maxDelta = delta;

//                    solution[col] = newValue;
//                }

//                if (maxDelta < 1e-8)
//                    break;
//            }

//            var equalityMatrix = BuildEqualityMatrix(coefficients, targets);
//            if (equalityMatrix.GetLength(0) > 0)
//            {
//                ProjectToEqualities(equalityMatrix, solution);
//                for (int i = 0; i < solution.Length; i++)
//                {
//                    if (solution[i] < 0) solution[i] = 0;
//                    else if (solution[i] > 1) solution[i] = 1;

//                    solution[i] = SnapToCommonValues(solution[i]);
//                }
//            }

//            return solution;
//        }

//        /// <summary>
//        /// Extract independent equality rows (zero targets) for projection.
//        /// </summary>
//        private static double[,] BuildEqualityMatrix(double[,] coefficients, double[] targets)
//        {
//            int rows = coefficients.GetLength(0);
//            int cols = coefficients.GetLength(1);
//            var eqRows = new List<int>();
//            for (int row = 0; row < rows; row++)
//            {
//                if (Math.Abs(targets[row]) < EqualityTolerance)
//                {
//                    bool hasCoefficients = false;
//                    for (int col = 0; col < cols && !hasCoefficients; col++)
//                        hasCoefficients = Math.Abs(coefficients[row, col]) > EqualityTolerance;

//                    if (hasCoefficients)
//                        eqRows.Add(row);
//                }
//            }

//            double[,] equalityMatrix = new double[eqRows.Count, cols];
//            for (int i = 0; i < eqRows.Count; i++)
//            {
//                int rowIndex = eqRows[i];
//                for (int col = 0; col < cols; col++)
//                    equalityMatrix[i, col] = coefficients[rowIndex, col];
//            }

//            return RemoveDependentRows(equalityMatrix);
//        }

//        /// <summary>
//        /// Snap small numerical noise to typical thrust levels.
//        /// </summary>
//        private static double SnapToCommonValues(double value)
//        {
//            if (Math.Abs(value) < 2e-3)
//                return 0;
//            if (Math.Abs(value - 0.5) < 1e-3)
//                return 0.5;
//            if (Math.Abs(value - 1.0) < 1e-3)
//                return 1.0;
//            return value;
//        }

//        /// <summary>
//        /// Project vector onto equality constraints using normal equations.
//        /// </summary>
//        private static void ProjectToEqualities(double[,] equalityMatrix, double[] vector)
//        {
//            int eqCount = equalityMatrix.GetLength(0);
//            if (eqCount == 0)
//                return;

//            int cols = equalityMatrix.GetLength(1);
//            // Compute A * x
//            double[] ax = new double[eqCount];
//            for (int r = 0; r < eqCount; r++)
//            {
//                double sum = 0;
//                for (int c = 0; c < cols; c++)
//                    sum += equalityMatrix[r, c] * vector[c];
//                ax[r] = sum;
//            }

//            // Solve (A A^T) y = ax
//            double[,] aat = new double[eqCount, eqCount];
//            for (int r1 = 0; r1 < eqCount; r1++)
//            {
//                for (int r2 = 0; r2 < eqCount; r2++)
//                {
//                    double sum = 0;
//                    for (int c = 0; c < cols; c++)
//                        sum += equalityMatrix[r1, c] * equalityMatrix[r2, c];
//                    aat[r1, r2] = sum;
//                }
//            }

//            double[] y = new double[eqCount];
//            if (!SolveLinearSystem(aat, ax, y))
//                return; // Singular; skip projection.

//            // correction = A^T * y
//            for (int c = 0; c < cols; c++)
//            {
//                double correction = 0;
//                for (int r = 0; r < eqCount; r++)
//                    correction += equalityMatrix[r, c] * y[r];
//                vector[c] -= correction;
//            }
//        }

//        /// <summary>
//        /// Remove linearly dependent equality rows via Gaussian elimination.
//        /// </summary>
//        private static double[,] RemoveDependentRows(double[,] matrix)
//        {
//            int rows = matrix.GetLength(0);
//            int cols = matrix.GetLength(1);
//            if (rows == 0)
//                return matrix;

//            double[,] temp = (double[,])matrix.Clone();
//            int[] rowOrder = new int[rows];
//            for (int i = 0; i < rows; i++)
//                rowOrder[i] = i;

//            int pivotRow = 0;
//            for (int col = 0; col < cols && pivotRow < rows; col++)
//            {
//                int bestRow = -1;
//                double bestVal = EqualityTolerance;
//                for (int r = pivotRow; r < rows; r++)
//                {
//                    double value = Math.Abs(temp[r, col]);
//                    if (value > bestVal)
//                    {
//                        bestVal = value;
//                        bestRow = r;
//                    }
//                }

//                if (bestRow == -1)
//                    continue;

//                SwapRows(temp, rowOrder, pivotRow, bestRow);

//                double pivot = temp[pivotRow, col];
//                for (int c = col; c < cols; c++)
//                    temp[pivotRow, c] /= pivot;

//                for (int r = 0; r < rows; r++)
//                {
//                    if (r == pivotRow)
//                        continue;
//                    double factor = temp[r, col];
//                    if (Math.Abs(factor) < EqualityTolerance)
//                        continue;
//                    for (int c = col; c < cols; c++)
//                        temp[r, c] -= factor * temp[pivotRow, c];
//                }

//                pivotRow++;
//            }

//            if (pivotRow == rows)
//                return matrix; // Full rank already.

//            double[,] reduced = new double[pivotRow, cols];
//            for (int r = 0; r < pivotRow; r++)
//            {
//                int originalRow = rowOrder[r];
//                for (int c = 0; c < cols; c++)
//                    reduced[r, c] = matrix[originalRow, c];
//            }

//            return reduced;
//        }

//        /// <summary>
//        /// Swap two rows and mirror the swap in the order index.
//        /// </summary>
//        private static void SwapRows(double[,] matrix, int[] order, int r1, int r2)
//        {
//            if (r1 == r2)
//                return;

//            int cols = matrix.GetLength(1);
//            for (int c = 0; c < cols; c++)
//            {
//                double tmp = matrix[r1, c];
//                matrix[r1, c] = matrix[r2, c];
//                matrix[r2, c] = tmp;
//            }

//            int orderTmp = order[r1];
//            order[r1] = order[r2];
//            order[r2] = orderTmp;
//        }

//        private static bool SolveLinearSystem(double[,] matrix, double[] rhs, double[] solution)
//        {
//            int n = rhs.Length;
//            double[,] a = (double[,])matrix.Clone();
//            double[] b = (double[])rhs.Clone();

//            for (int pivot = 0; pivot < n; pivot++)
//            {
//                int maxRow = pivot;
//                double maxVal = Math.Abs(a[pivot, pivot]);
//                for (int row = pivot + 1; row < n; row++)
//                {
//                    double val = Math.Abs(a[row, pivot]);
//                    if (val > maxVal)
//                    {
//                        maxVal = val;
//                        maxRow = row;
//                    }
//                }

//                if (maxVal < EqualityTolerance)
//                    return false;

//                if (maxRow != pivot)
//                    SwapRows(a, b, pivot, maxRow);

//                double pivotVal = a[pivot, pivot];
//                for (int col = pivot; col < n; col++)
//                    a[pivot, col] /= pivotVal;
//                b[pivot] /= pivotVal;

//                for (int row = pivot + 1; row < n; row++)
//                {
//                    double factor = a[row, pivot];
//                    if (Math.Abs(factor) < EqualityTolerance)
//                        continue;
//                    for (int col = pivot; col < n; col++)
//                        a[row, col] -= factor * a[pivot, col];
//                    b[row] -= factor * b[pivot];
//                }
//            }

//            for (int row = n - 1; row >= 0; row--)
//            {
//                double sum = b[row];
//                for (int col = row + 1; col < n; col++)
//                    sum -= a[row, col] * solution[col];
//                solution[row] = sum;
//            }

//            return true;
//        }

//        /// <summary>
//        /// Swap matrix rows and RHS entries.
//        /// </summary>
//        private static void SwapRows(double[,] matrix, double[] rhs, int r1, int r2)
//        {
//            int cols = matrix.GetLength(1);
//            for (int c = 0; c < cols; c++)
//            {
//                double tmp = matrix[r1, c];
//                matrix[r1, c] = matrix[r2, c];
//                matrix[r2, c] = tmp;
//            }

//            double rhsTmp = rhs[r1];
//            rhs[r1] = rhs[r2];
//            rhs[r2] = rhsTmp;
//        }
//    }
//}
