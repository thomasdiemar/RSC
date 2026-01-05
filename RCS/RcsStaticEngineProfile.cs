using System.Xml.Serialization;

public class RcsEngineProfile {
    Dictionary<RcsCommand,IReadOnlyDictionary<string, Fraction>> ThrusterOutputs = new Dictionary<RcsCommand,IReadOnlyDictionary<string, Fraction>>();
    public void AddProfileCommand(RcsCommand command, IReadOnlyDictionary<string, Fraction> outputs) {
        if(ThrusterOutputs.ContainsKey(command)) {
            ThrusterOutputs[command] = outputs;
        } else {
            ThrusterOutputs.Add(command, outputs);
        }
    }
}