using System;
using System.Collections.Generic;

namespace LinearSolver.Custom
{
    /// <summary>
    /// Bounded linear solver for the RCS scenarios. It uses a minimum-norm pseudo-inverse with a
    /// simple active-set method so each thruster output remains in [0,1], closely mirroring the
    /// Microsoft Solver Foundation behaviour used in the tests.
    /// </summary>
    public class CustomLinearSolver : IMyLinearSolver
    {
        private const double Epsilon = 1e-9;

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

            var result = new double[cols];
            var remaining = (double[])constants.Clone();
            var freeMask = new bool[cols];
            for (int i = 0; i < cols; i++)
                freeMask[i] = true;

            while (true)
            {
                var freeIndices = GetFreeIndices(freeMask);
                if (freeIndices.Count == 0)
                    break;

                var subMatrix = ExtractMatrix(coefficients, freeIndices);
                var candidate = SolveMinimumNorm(subMatrix, remaining);

                int violatingIndex = FindViolatingIndex(candidate);
                if (violatingIndex == -1)
                {
                    for (int i = 0; i < freeIndices.Count; i++)
                        result[freeIndices[i]] = Clamp(candidate[i]);
                    break;
                }

                   	double boundValue = candidate[violatingIndex] < 0 ? 0 : 1;
                int thrusterIndex = freeIndices[violatingIndex];
                result[thrusterIndex] = boundValue;
                freeMask[thrusterIndex] = false;

                if (boundValue > 0)
                    SubtractColumn(coefficients, thrusterIndex, boundValue, remaining);
            }

            return result;
        }

        private static List<int> GetFreeIndices(bool[] mask)
        {
            var indices = new List<int>(mask.Length);
            for (int i = 0; i < mask.Length; i++)
                if (mask[i])
                    indices.Add(i);
            return indices;
        }

        private static double[,] ExtractMatrix(double[,] coefficients, IReadOnlyList<int> indices)
        {
            int rows = coefficients.GetLength(0);
            var matrix = new double[rows, indices.Count];

            for (int col = 0; col < indices.Count; col++)
            {
                int source = indices[col];
                for (int row = 0; row < rows; row++)
                    matrix[row, col] = coefficients[row, source];
            }

            return matrix;
        }

        private static double[] SolveMinimumNorm(double[,] matrix, double[] desired)
        {
            var transposed = Transpose(matrix);
            var product = Multiply(matrix, transposed);
            var y = SolveSquareSystem(product, desired);
            var solution = MultiplyMatrixVector(transposed, y);
            return solution;
        }

        private static double[,] Transpose(double[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            var result = new double[cols, rows];

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    result[j, i] = matrix[i, j];

            return result;
        }

        private static double[,] Multiply(double[,] left, double[,] right)
        {
            int rows = left.GetLength(0);
            int inner = left.GetLength(1);
            int cols = right.GetLength(1);
            var result = new double[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int k = 0; k < inner; k++)
                {
                    double val = left[i, k];
                    if (Math.Abs(val) < Epsilon)
                        continue;

                    for (int j = 0; j < cols; j++)
                        result[i, j] += val * right[k, j];
                }
            }

            return result;
        }

        private static double[] MultiplyMatrixVector(double[,] matrix, double[] vector)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            var result = new double[rows];

            for (int i = 0; i < rows; i++)
            {
                double sum = 0;
                for (int j = 0; j < cols; j++)
                    sum += matrix[i, j] * vector[j];
                result[i] = sum;
            }

            return result;
        }

        private static double[] SolveSquareSystem(double[,] matrix, double[] vector)
        {
            int size = matrix.GetLength(0);
            var augmented = new double[size, size + 1];

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    double value = matrix[i, j];
                    if (i == j)
                        value += 1e-8;
                    augmented[i, j] = value;
                }
                augmented[i, size] = vector[i];
            }

            for (int pivot = 0; pivot < size; pivot++)
            {
                int maxRow = pivot;
                double maxVal = Math.Abs(augmented[pivot, pivot]);
                for (int row = pivot + 1; row < size; row++)
                {
                    double val = Math.Abs(augmented[row, pivot]);
                    if (val > maxVal)
                    {
                        maxVal = val;
                        maxRow = row;
                    }
                }

                if (maxVal < Epsilon)
                    maxVal = Epsilon;

                if (maxRow != pivot)
                    SwapRows(augmented, pivot, maxRow, size + 1);

                double pivotValue = augmented[pivot, pivot];
                for (int col = pivot; col <= size; col++)
                    augmented[pivot, col] /= pivotValue;

                for (int row = 0; row < size; row++)
                {
                    if (row == pivot)
                        continue;

                    double factor = augmented[row, pivot];
                    if (Math.Abs(factor) < Epsilon)
                        continue;

                    for (int col = pivot; col <= size; col++)
                        augmented[row, col] -= factor * augmented[pivot, col];
                }
            }

            var result = new double[size];
            for (int i = 0; i < size; i++)
                result[i] = augmented[i, size];

            return result;
        }

        private static void SwapRows(double[,] matrix, int rowA, int rowB, int length)
        {
            for (int col = 0; col < length; col++)
            {
                double temp = matrix[rowA, col];
                matrix[rowA, col] = matrix[rowB, col];
                matrix[rowB, col] = temp;
            }
        }

        private static int FindViolatingIndex(double[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] < -Epsilon || values[i] > 1 + Epsilon)
                    return i;
            }

            return -1;
        }

        private static double Clamp(double value)
        {
            if (value < 0)
                return 0;
            if (value > 1)
                return 1;
            return value;
        }

        private static void SubtractColumn(double[,] coefficients, int columnIndex, double value, double[] desired)
        {
            int rows = coefficients.GetLength(0);
            for (int row = 0; row < rows; row++)
                desired[row] -= coefficients[row, columnIndex] * value;
        }
    }
}
