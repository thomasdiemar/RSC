using LinearSolver;
using System.Collections.Generic;
using System.Xml.Schema;

namespace RCS
{
    public class RcsEngineResult
    {
        public IReadOnlyDictionary<string, Fraction> ThrusterOutputs { get; }
        public RcsVector<Fraction> ResultantForce { get; }
        public RcsVector<Fraction> ResultantTorque { get; }

        public RcsEngineResult(
            IReadOnlyDictionary<string, Fraction> thrusterOutputs,
            RcsVector<Fraction> resultantForce,
            RcsVector<Fraction> resultantTorque)
        {
            ThrusterOutputs = thrusterOutputs;
            ResultantForce = resultantForce;
            ResultantTorque = resultantTorque;
        }
    }
}
