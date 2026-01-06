using LinearSolver;

namespace RCS
{

    public struct RcsEngineOptimiserCommand
    {
        public RcsVector<Fraction> DesiredForce { get; set; }
        public RcsVector<Fraction> DesiredTorque { get; set; }
        public bool AllowNonCommandedForces { get; set; }
        public bool AllowNonCommandedTorques { get; set; }

        public RcsEngineOptimiserCommand(
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


        public static implicit operator RcsEngineOptimiserCommand(RcsCommand command)
        {
            return new RcsEngineOptimiserCommand(
                new RcsVector<Fraction>(new Fraction((int)command.DesiredForce.X), new Fraction((int)command.DesiredForce.Y), new Fraction((int)command.DesiredForce.Z)),
                new RcsVector<Fraction>(new Fraction((int)command.DesiredTorque.X), new Fraction((int)command.DesiredTorque.Y), new Fraction((int)command.DesiredTorque.Z)),
                command.AllowNonCommandedForces,
                command.AllowNonCommandedTorques);
        }
    }
}


