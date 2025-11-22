using System.Collections.Generic;

namespace RCS
{
    public class RcsEngineResult
    {
        public IReadOnlyDictionary<string, double> ThrusterOutputs { get; }
        public RcsVector ResultantForce { get; }
        public RcsVector ResultantTorque { get; }

        public RcsEngineResult(
            IReadOnlyDictionary<string, double> thrusterOutputs,
            RcsVector resultantForce,
            RcsVector resultantTorque)
        {
            ThrusterOutputs = thrusterOutputs;
            ResultantForce = resultantForce;
            ResultantTorque = resultantTorque;
        }
    }
}
