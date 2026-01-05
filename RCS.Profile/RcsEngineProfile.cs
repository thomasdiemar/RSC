using LinearSolver;
using System.Collections.Generic;

namespace RCS.Profile
{
    public class RcsEngineProfile
    {
        Dictionary<RcsProfileCommand, IReadOnlyDictionary<string, Fraction>> ThrusterOutputs =
            new Dictionary<RcsProfileCommand, IReadOnlyDictionary<string, Fraction>>();

        public void AddProfileCommand(RcsProfileCommand command, IReadOnlyDictionary<string, Fraction> outputs)
        {
            if (ThrusterOutputs.ContainsKey(command))
            {
                ThrusterOutputs[command] = outputs;
            }
            else
            {
                ThrusterOutputs.Add(command, outputs);
            }
        }

        public IReadOnlyDictionary<string, Fraction> GetProfileCommand(RcsProfileCommand command)
        {
            return ThrusterOutputs.ContainsKey(command) ? ThrusterOutputs[command] : null;
        }
    }
}
