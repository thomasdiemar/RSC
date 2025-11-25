using System;
using LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Model;
using LinearSolver.Custom.GoalProgramming.Mathematics;

namespace LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Simplex
{
    /// <summary>
    /// Implements the bounded augmented ratio test from Agoritm_Rcs.tex §5.
    /// Determines whether the entering variable hits its opposite bound or a basic row hits a bound first.
    /// </summary>
    public sealed class BoundedAugmentedRatioTest : IRatioTest
    {
        public PivotType Evaluate(PreEmptiveIntegerTableau tableau)
        {
            if (tableau == null)
            {
                throw new ArgumentNullException(nameof(tableau));
            }

            var eps = Fraction.Epsilon;
            int entering = tableau.EnteringColumnIndex;
            if (entering < 0 || entering >= tableau.ColumnCount || entering == tableau.RightHandSideColumnIndex)
            {
                return Degenerate(tableau);
            }

            var enteringVariable = tableau.ColumnHeaders[entering];
            if (enteringVariable == null)
            {
                return Degenerate(tableau);
            }

            bool atLower = enteringVariable.BoundState == SolverBoundState.Lower;
            bool atUpper = enteringVariable.BoundState == SolverBoundState.Upper;
            if (!atLower && !atUpper)
            {
                return Degenerate(tableau);
            }

            var bestTheta = Fraction.MaxValue;
            var bestRow = -1;
            SolverBoundState bestRowTarget = SolverBoundState.Basic;

            for (int row = 0; row < tableau.RowCount; row++)
            {
                var state = tableau.RowStates[row];
                if (state.Priority != tableau.CurrentPriority)
                {
                    continue;
                }

                var directionCoefficient = tableau.GetCoefficient(row, entering);
                if (Fraction.Abs(directionCoefficient) <= eps)
                {
                    continue;
                }

                var rhsValue = tableau.GetRightHandSide(row);
                var lower = state.Bound?.Lower ?? Fraction.MinValue;
                var upper = state.Bound?.Upper ?? Fraction.MaxValue;

                if (directionCoefficient > eps && lower != Fraction.MinValue)
                {
                    var rowTheta = (rhsValue - lower) / directionCoefficient;
                    if (rowTheta > eps && IsBetter(rowTheta, row, bestTheta, bestRow))
                    {
                        bestTheta = rowTheta;
                        bestRow = row;
                        bestRowTarget = SolverBoundState.Lower;
                    }
                }
                else if (directionCoefficient < -eps && upper != Fraction.MaxValue)
                {
                    var rowTheta = (upper - rhsValue) / (-1 * directionCoefficient);
                    if (rowTheta > eps && IsBetter(rowTheta, row, bestTheta, bestRow))
                    {
                        bestTheta = rowTheta;
                        bestRow = row;
                        bestRowTarget = SolverBoundState.Upper;
                    }
                }
            }

            var thetaBound = atLower
                ? enteringVariable.UpperBound - enteringVariable.Value
                : enteringVariable.Value - enteringVariable.LowerBound;

            if (thetaBound < new Fraction(0))
            {
                thetaBound = new Fraction(0);
            }

            var theta = Fraction.Min(thetaBound, bestTheta);
            if (!(theta > eps))
            {
                return Degenerate(tableau);
            }

            if (thetaBound + eps < bestTheta)
            {
                tableau.Delta = thetaBound;
                tableau.KeyRow = -1;
                return PivotType.PreEmptiveBoundHit;
            }

            if (bestRow < 0)
            {
                return Degenerate(tableau);
            }

            tableau.Delta = bestTheta;
            tableau.KeyRow = bestRow;
            tableau.RowStates[bestRow].SetPendingPivotState(bestRowTarget);
            return PivotType.RowPivot;
        }

        private static bool IsBetter(Fraction theta, int row, Fraction bestTheta, int bestRow)
        {
            var eps = Fraction.Epsilon;
            return theta < bestTheta - eps ||
                   (Fraction.Abs(theta - bestTheta) <= eps && row < bestRow);
        }

        private static PivotType Degenerate(PreEmptiveIntegerTableau tableau)
        {
            tableau.Delta = new Fraction(0);
            tableau.KeyRow = -1;
            return PivotType.DegeneratePivot;
        }
    }
}
