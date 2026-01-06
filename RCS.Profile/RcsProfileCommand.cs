using LinearSolver;
using RCS;

namespace RCS.Profile
{
    public struct RcsProfileCommand
    {
        public static readonly RcsVector<Fraction> NOCOMMAND = new RcsVector<Fraction>(0, 0, 0);
        public RcsVector<Fraction> DesiredForce { get; set; }
        public RcsVector<Fraction> DesiredTorque { get; set; }

        public RcsProfileCommand(
            RcsVector<Fraction> desiredForce,
            RcsVector<Fraction> desiredTorque)
        {
            DesiredForce = desiredForce;
            DesiredTorque = desiredTorque;
        }

        public static implicit operator RcsProfileCommand(RcsEngineOptimiserCommand command)
        {
            return new RcsProfileCommand(command.DesiredForce, command.DesiredTorque);
        }
    }
}
