using LinearSolver;

namespace RCS
{
    public struct RcsCommand
    {
        public RcsVector<Fraction> DesiredForce { get; set; }
        public RcsVector<Fraction> DesiredTorque { get; set; }

        public bool AllowNonCommandedForces { get; set; }
        public bool AllowNonCommandedTorques { get; set; }

        public RcsCommand(
            RcsVector<Fraction> desiredForce,
            RcsVector<Fraction> desiredTorque,
            bool allowNonCommandedForces = false,
            bool allowNonCommandedTorques = false)
        {
            DesiredForce = desiredForce;
            DesiredTorque = desiredTorque;
            AllowNonCommandedForces = allowNonCommandedForces;
            AllowNonCommandedTorques = allowNonCommandedTorques;
        }
    }
}
