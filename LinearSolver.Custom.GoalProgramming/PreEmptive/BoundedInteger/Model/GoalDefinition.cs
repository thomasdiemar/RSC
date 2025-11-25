using System;
using System.Collections.Generic;
using System.Linq;
using LinearSolver.Custom.GoalProgramming.Mathematics;

namespace LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Model
{
    /// <summary>
    /// Describes a single lexicographic goal (see Agoritm_Rcs.tex §2).
    /// Captures the direction, coefficients, priority, and tolerances so stages can be locked across the preemptive sequence.
    /// </summary>
    public sealed class GoalDefinition
    {
        private readonly Dictionary<string, Fraction> coefficients;

        public GoalDefinition(
            string name,
            GoalSense sense,
            int priority,
            IEnumerable<KeyValuePair<string, Fraction>> coefficientVector,
            Fraction rightHandSide,
            Fraction tolerance)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Goal name must be provided.", nameof(name));
            }

            if (priority < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(priority), "Priority must be non-negative.");
            }

            if (coefficientVector == null)
            {
                throw new ArgumentNullException(nameof(coefficientVector));
            }

            Name = name;
            Sense = sense;
            Priority = priority;
            RightHandSide = rightHandSide;
            Tolerance = tolerance;
            coefficients = coefficientVector.ToDictionary(k => k.Key, k => k.Value);
        }

        public string Name { get; }

        public GoalSense Sense { get; }

        public int Priority { get; }

        public Fraction RightHandSide { get; }

        public Fraction Tolerance { get; }

        public IReadOnlyDictionary<string, Fraction> Coefficients => coefficients;

        public Fraction GetCoefficient(string variableName)
        {
            if (string.IsNullOrWhiteSpace(variableName))
            {
                return Zero;
            }

            return coefficients.TryGetValue(variableName, out var value) ? value : Zero;
        }

        /// <summary>
        /// Creates a new constraint fixing the goal value for lower-priority stages.
        /// </summary>
        public GoalDefinition CreateLock(Fraction achievedValue)
        {
            var lockCoefficients = coefficients.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return new GoalDefinition(
                name: $"{Name}_Lock",
                sense: GoalSense.Equal,
                priority: Priority,
                coefficientVector: lockCoefficients,
                rightHandSide: achievedValue,
                tolerance: Tolerance);
        }

        public GoalDefinition Clone()
        {
            return new GoalDefinition(Name, Sense, Priority, coefficients.ToList(), RightHandSide, Tolerance);
        }

        private static readonly Fraction Zero = new Fraction(0);
    }

    public enum GoalSense
    {
        Maximize,
        Minimize,
        Equal
    }
}
