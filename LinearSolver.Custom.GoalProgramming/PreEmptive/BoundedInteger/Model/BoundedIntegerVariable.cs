using System;
using LinearSolver;

namespace LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Model
{
    /// <summary>
    /// Represents a single decision variable in the bounded preemptive integer simplex.
    /// Keeps track of its bounds, integrality flag, and current status (LB/UB/Basic) as described in Agoritm_Rcs.tex §3.
    /// </summary>
    public sealed class BoundedIntegerVariable
    {
        public BoundedIntegerVariable(
            string name,
            Fraction lowerBound,
            Fraction upperBound,
            bool isInteger,
            int priorityLevel = 0)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Variable name must be provided.", nameof(name));
            }

            if (upperBound < lowerBound)
            {
                throw new ArgumentException("Upper bound must be greater than or equal to the lower bound.", nameof(upperBound));
            }

            Name = name;
            LowerBound = lowerBound;
            UpperBound = upperBound;
            IsInteger = isInteger;
            PriorityLevel = priorityLevel;
            Value = lowerBound;
            BoundState = SolverBoundState.Lower;
        }

        public string Name { get; }

        public Fraction LowerBound { get; }

        public Fraction UpperBound { get; }

        public bool IsInteger { get; }

        public int PriorityLevel { get; }

        public Fraction Value { get; private set; }

        public SolverBoundState BoundState { get; private set; }

        /// <summary>
        /// Updates the value of the variable while ensuring it respects bounds and integrality requirements.
        /// </summary>
        public void SetValue(Fraction value)
        {
            if (value < LowerBound || value > UpperBound)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"Value {value} is outside [{LowerBound}, {UpperBound}].");
            }

            Value = value;
            BoundState = ResolveBoundState(value);
        }

        /// <summary>
        /// Clones the variable so tableaux can be duplicated when locking earlier priorities.
        /// </summary>
        public BoundedIntegerVariable Clone()
        {
            var clone = new BoundedIntegerVariable(Name, LowerBound, UpperBound, IsInteger, PriorityLevel);
            clone.SetValue(Value);
            return clone;
        }

        public bool HasIntegralValue()
        {
            return !IsInteger || IsIntegral(Value);
        }

        public Fraction FractionalDistance()
        {
            if (!IsInteger)
            {
                return new Fraction(0);
            }

            var fraction = Value - Floor(Value);
            if (fraction < new Fraction(0))
            {
                fraction = -1 * fraction;
            }

            return fraction;
        }

        public static Fraction Floor(Fraction value)
        {
            var floor = (int)Math.Floor((double)value.Numerator / value.Denominator);
            return new Fraction(floor);
        }

        public static Fraction Ceiling(Fraction value)
        {
            var ceil = (int)Math.Ceiling((double)value.Numerator / value.Denominator);
            return new Fraction(ceil);
        }

        public BoundedIntegerVariable WithBounds(Fraction lowerBound, Fraction upperBound)
        {
            var copy = new BoundedIntegerVariable(Name, lowerBound, upperBound, IsInteger, PriorityLevel);
            var clamped = Value;
            if (clamped < lowerBound)
            {
                clamped = lowerBound;
            }
            else if (clamped > upperBound)
            {
                clamped = upperBound;
            }

            copy.SetValue(clamped);
            return copy;
        }

        private static bool IsIntegral(Fraction value)
        {
            return value.Denominator == 1;
        }

        private SolverBoundState ResolveBoundState(Fraction value)
        {
            if (value == LowerBound)
            {
                return SolverBoundState.Lower;
            }

            if (value == UpperBound)
            {
                return SolverBoundState.Upper;
            }

            return SolverBoundState.Basic;
        }
    }

    public enum SolverBoundState
    {
        Lower,
        Upper,
        Basic
    }
}
