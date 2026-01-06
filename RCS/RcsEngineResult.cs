using LinearSolver;
using System.Collections.Generic;
using System.Xml.Schema;

namespace RCS
{
    public class RcsEngineResult
    {
        public IReadOnlyDictionary<string, float> ThrusterOutputs { get; }
        public RcsVector<float> ResultantForce { get; }
        public RcsVector<float> ResultantTorque { get; }

        public RcsEngineResult(
            IReadOnlyDictionary<string, float> thrusterOutputs,
            RcsVector<float> resultantForce,
            RcsVector<float> resultantTorque)
        {
            ThrusterOutputs = thrusterOutputs;
            ResultantForce = resultantForce;
            ResultantTorque = resultantTorque;
        }
    }
}
