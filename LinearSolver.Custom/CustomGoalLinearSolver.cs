//using System;
//using System.Collections.Generic;
//using LinearSolver;

//namespace LinearSolver.Custom
//{
//    /// <summary>
//    /// Goal-based solver that mimics the preemptive behaviour of <c>MsfGoalLinearSolver</c>
//    /// without relying on MSF. Goals are processed in row order with exponentially decreasing
//    /// priority to approximate lexicographic optimisation, while all variables remain bounded [0,1].
//    /// </summary>
//    public class CustomGoalLinearSolver : IMyLinearSolver
//    {
//        private const double EqualityTolerance = 1e-12;
//        private const double SoftZeroTolerance = 1e-8;
//        private const double SoftZeroWeight = 1e-3;
//        private const double SnapTolerance = 1e-1;

//        /// <summary>
//        /// Explore bounded thrust assignments ({0,0.5,1}) and pick the lexicographically best candidate per weighted goals.
//        /// </summary>
//        /// <summary>
//        /// Explore bounded thrust assignments ({0,0.5,1}) and pick the lexicographically best candidate per weighted goals.
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

//            // Exhaustive search over {0,0.5,1} per thruster to mirror MSF extreme-point behaviour.
//            double[] levels = new[] { 0.0, 0.5, 1.0 };
//            double[] best = null;
//            double[] bestScores = null;
//            int[] priorityOrder = BuildPriorityOrder(constants);

//            var current = new double[cols];
//            Explore(0);
//            return best ?? new double[cols];

//            void Explore(int index)
//            {
//                if (index == cols)
//                {
//                    EvaluateCandidate();
//                    return;
//                }

//                for (int i = 0; i < levels.Length; i++)
//                {
//                    current[index] = levels[i];
//                    Explore(index + 1);
//                }
//            }

//            void EvaluateCandidate()
//            {
//                double[] rowValues = new double[rows];
//                for (int r = 0; r < rows; r++)
//                {
//                    double sum = 0;
//                    for (int c = 0; c < cols; c++)
//                        sum += coefficients[r, c] * current[c];
//                    rowValues[r] = sum;
//                }

//                // Reject hard equalities that are off tolerance.
//                for (int r = 0; r < rows; r++)
//                {
//                    if (double.IsNaN(constants[r]))
//                        continue;
//                    if (Math.Abs(constants[r]) < SoftZeroTolerance && Math.Abs(rowValues[r]) > 1e-6)
//                        return;
//                }

//                double[] scores = new double[rows];
//                for (int r = 0; r < rows; r++)
//                {
//                    double desired = constants[r];
//                    if (double.IsNaN(desired))
//                    {
//                        scores[r] = -Math.Abs(rowValues[r]);
//                    }
//                    else if (desired > 0)
//                    {
//                        scores[r] = rowValues[r];
//                    }
//                    else if (desired < 0)
//                    {
//                        scores[r] = -rowValues[r];
//                    }
//                    else
//                    {
//                        scores[r] = -Math.Abs(rowValues[r]); // equality rows prefer zero.
//                    }
//                }

//                if (best == null || LexicographicallyBetter(scores, bestScores))
//                {
//                    best = (double[])current.Clone();
//                    bestScores = scores;
//                }
//            }

//            bool LexicographicallyBetter(double[] candidate, double[] incumbent)
//            {
//                for (int pi = 0; pi < priorityOrder.Length; pi++)
//                {
//                    int idx = priorityOrder[pi];
//                    if (Math.Abs(candidate[idx] - incumbent[idx]) < 1e-9)
//                        continue;
//                    return candidate[idx] > incumbent[idx];
//                }
//                return false;
//            }
//        }

//        private static int[] BuildPriorityOrder(double[] constants)
//        {
//            var primary = new List<int>();
//            var equalities = new List<int>();
//            var softZeros = new List<int>();

//            for (int i = 0; i < constants.Length; i++)
//            {
//                if (double.IsNaN(constants[i]))
//                    softZeros.Add(i);
//                else if (Math.Abs(constants[i]) < SoftZeroTolerance)
//                    equalities.Add(i);
//                else
//                    primary.Add(i);
//            }

//            var order = new int[constants.Length];
//            int idxOut = 0;
//            foreach (var i in primary) order[idxOut++] = i;
//            foreach (var i in equalities) order[idxOut++] = i;
//            foreach (var i in softZeros) order[idxOut++] = i;
//            return order;
//        }

//        /// <summary>
//        /// Emit a single progress snapshot containing the best solution found.
//        /// </summary>
//        /// <summary>
//        /// Emit a single progress snapshot containing the best solution found.
//        /// </summary>
//        public IEnumerable<MyProgress<double[]>> Solve(double[,] coefficients, double[] constants)
//        {
//            var result = SolveCore(coefficients, constants);
//            yield return CreateProgress(result);
//        }

//        private static MyProgress<double[]> CreateProgress(double[] snapshot)
//        {
//            return new MyProgress<double[]>
//            {
//                Result = (double[])snapshot.Clone(),
//                Done = true
//            };
//        }

//        /// <summary>
//        /// Build exponentially decreasing weights to approximate lexicographic goal ordering.
//        /// </summary>
//        private static double[] BuildPriorityWeights(double[] constants)
//        {
//            double[] weights = new double[constants.Length];
//            // Exponentially decreasing weight to approximate lexicographic order.
//            const double baseWeight = 10.0;
//            for (int i = 0; i < constants.Length; i++)
//            {
//                double importance = Math.Pow(baseWeight, constants.Length - i);
//                double magnitude;
//                if (double.IsNaN(constants[i]))
//                    magnitude = SoftZeroWeight;
//                else if (Math.Abs(constants[i]) < SoftZeroTolerance)
//                    magnitude = 1.0;
//                else
//                    magnitude = Math.Max(Math.Abs(constants[i]), 1.0);
//                weights[i] = importance * magnitude;
//            }
//            return weights;
//        }

//        private static double[,] BuildEqualityMatrix(double[,] coefficients, double[] constants)
//        {
//            int rows = coefficients.GetLength(0);
//            int cols = coefficients.GetLength(1);
//            var eqRows = new List<int>();
//            for (int row = 0; row < rows; row++)
//            {
//                if (double.IsNaN(constants[row]))
//                {
//                    // Soft zero: no equality row.
//                    continue;
//                }

//                if (Math.Abs(constants[row]) < EqualityTolerance)
//                {
//                    // Only enforce equality if this row is truly intended as hard zero (not soft zero).
//                    if (Math.Abs(constants[row]) < SoftZeroTolerance)
//                    {
//                        bool hasCoefficients = false;
//                        for (int col = 0; col < cols && !hasCoefficients; col++)
//                            hasCoefficients = Math.Abs(coefficients[row, col]) > EqualityTolerance;

//                        if (hasCoefficients)
//                            eqRows.Add(row);
//                    }
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

//        private static void ProjectToEqualities(double[,] equalityMatrix, double[] vector)
//        {
//            int eqCount = equalityMatrix.GetLength(0);
//            if (eqCount == 0)
//                return;

//            int cols = equalityMatrix.GetLength(1);
//            double[] ax = new double[eqCount];
//            for (int r = 0; r < eqCount; r++)
//            {
//                double sum = 0;
//                for (int c = 0; c < cols; c++)
//                    sum += equalityMatrix[r, c] * vector[c];
//                ax[r] = sum;
//            }

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
//                return;

//            for (int c = 0; c < cols; c++)
//            {
//                double correction = 0;
//                for (int r = 0; r < eqCount; r++)
//                    correction += equalityMatrix[r, c] * y[r];
//                vector[c] -= correction;
//            }
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
//                return matrix;

//            double[,] reduced = new double[pivotRow, cols];
//            for (int r = 0; r < pivotRow; r++)
//            {
//                int originalRow = rowOrder[r];
//                for (int c = 0; c < cols; c++)
//                    reduced[r, c] = matrix[originalRow, c];
//            }

//            return reduced;
//        }

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

//        private static double Snap(double value)
//        {
//            if (Math.Abs(value) < SnapTolerance) return 0;
//            if (Math.Abs(value - 0.5) < SnapTolerance) return 0.5;
//            if (Math.Abs(value - 1.0) < SnapTolerance) return 1.0;
//            return value;
//        }

//        private static void EnforceEqualitiesWithBounds(double[,] equalityMatrix, double[] solution)
//        {
//            int eqCount = equalityMatrix.GetLength(0);
//            if (eqCount == 0)
//                return;

//            for (int i = 0; i < 20; i++)
//            {
//                ProjectToEqualities(equalityMatrix, solution);
//                for (int c = 0; c < solution.Length; c++)
//                {
//                    if (solution[c] < 0) solution[c] = 0;
//                    else if (solution[c] > 1) solution[c] = 1;
//                }

//                if (MaxEqualityResidual(equalityMatrix, solution) < 1e-6)
//                {
//                    for (int c = 0; c < solution.Length; c++)
//                        solution[c] = Snap(solution[c]);
//                    return;
//                }
//            }

//            // If still infeasible, return zeros to respect hard constraints.
//            Array.Clear(solution, 0, solution.Length);
//        }

//        private static double MaxEqualityResidual(double[,] equalityMatrix, double[] vector)
//        {
//            int eqCount = equalityMatrix.GetLength(0);
//            int cols = equalityMatrix.GetLength(1);
//            double max = 0;
//            for (int r = 0; r < eqCount; r++)
//            {
//                double sum = 0;
//                for (int c = 0; c < cols; c++)
//                    sum += equalityMatrix[r, c] * vector[c];
//                max = Math.Max(max, Math.Abs(sum));
//            }
//            return max;
//        }
//    }
//}
