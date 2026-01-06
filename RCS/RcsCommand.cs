namespace RCS
{
    public struct RcsCommand
    {
        public RcsVector<float> DesiredForce { get; set; }
        public RcsVector<float> DesiredTorque { get; set; }
        public bool AllowNonCommandedForces { get; set; }
        public bool AllowNonCommandedTorques { get; set; }

        public RcsCommand(
            RcsVector<float> desiredForce,
            RcsVector<float> desiredTorque,
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
