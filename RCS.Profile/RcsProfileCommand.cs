using LinearSolver;
using RCS;

namespace RCS.Profile
{
    public struct RcsProfileCommand
    {
        public static readonly RcsVector<float> NOCOMMAND = new RcsVector<float>(0, 0, 0);
        public RcsVector<float> DesiredForce { get; set; }
        public RcsVector<float> DesiredTorque { get; set; }

        public RcsProfileCommand(
            RcsVector<float> desiredForce,
            RcsVector<float> desiredTorque)
        {
            DesiredForce = desiredForce;
            DesiredTorque = desiredTorque;
        }

        public static implicit operator RcsProfileCommand(RcsCommand command)
        {
            return new RcsProfileCommand(command.DesiredForce, command.DesiredTorque);
        }
    }
}
