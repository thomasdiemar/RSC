namespace RCS
{
    public struct RcsCommand
    {
        public RcsVector DesiredForce { get; set; }
        public RcsVector DesiredTorque { get; set; }

        public bool AllowNonCommandedForces { get; set; }
        public bool AllowNonCommandedTorques { get; set; }

        public RcsCommand(
            RcsVector desiredForce,
            RcsVector desiredTorque,
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
