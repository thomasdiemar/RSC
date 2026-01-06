using LinearSolver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RCS.Profile
{
    public class RcsStaticEngineProfile
    {
        private Dictionary<RcsProfileCommand, IReadOnlyDictionary<string, float>> ThrusterOutputs =
            new Dictionary<RcsProfileCommand, IReadOnlyDictionary<string, float>>();

        public void AddProfile(RcsProfileCommand command, IReadOnlyDictionary<string, float> outputs)
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

        IReadOnlyDictionary<string, float> GetSingleProfile(RcsProfileCommand command)
        {
            var c = ThrusterOutputs.ContainsKey(command) ? ThrusterOutputs[command] : null;
            return c;
        }

        public IReadOnlyDictionary<string, float> GetProfile(RcsProfileCommand command)
        {
            var desiredForces = CreateProfileRscVector(command.DesiredForce)
                .SelectMany(recsvector =>
                {
                    var thrusters = GetSingleProfile(new RcsProfileCommand(recsvector.Item1, RcsProfileCommand.NOCOMMAND));
                    thrusters.Select(thruster => new KeyValuePair<string, float>(thruster.Key, thruster.Value * recsvector.Item2));
                    return thrusters;
                })
                ;
            var desiredTorques = CreateProfileRscVector(command.DesiredTorque)
                .SelectMany(recsvector =>
                {
                    var thrusters = GetSingleProfile(new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, recsvector.Item1));
                    thrusters.Select(thruster => new KeyValuePair<string, float>(thruster.Key, thruster.Value * recsvector.Item2));
                    return thrusters;
                })
                ;
            var profiles = desiredForces.Union(desiredTorques)
                .GroupBy(profile => profile.Key)
                .Select(group => new KeyValuePair<string, float>(group.Key, group.Select(kv => kv.Value).Aggregate((a, b) => Clamp(a + b, -1f,1f))))
                .ToDictionary(kv => kv.Key, kv => kv.Value)
                ;

            return profiles;
        }

        List<Tuple<RcsVector<float>, float>> CreateProfileRscVector(RcsVector<float> vector)
        {
            var fractions = new List<Tuple<RcsVector<float>, float>>();
            var fraction = CreateProfileFraction(vector.X);
            if (fraction.Item1 != 0)
            {
                fractions.Add(Tuple.Create(new RcsVector<float>(fraction.Item1, 0, 0),fraction.Item2));
            }
            fraction = CreateProfileFraction(vector.Y);
            if (fraction.Item1 != 0)
            {
                fractions.Add(Tuple.Create(new RcsVector<float>(0, fraction.Item1, 0),fraction.Item2));
            }
            fraction = CreateProfileFraction(vector.Z);
            if (fraction.Item1 != 0)
            {
                fractions.Add(Tuple.Create(new RcsVector<float>(0, 0, fraction.Item1),fraction.Item2));
            }
            return fractions;
        }

        Tuple<float, float> CreateProfileFraction(float value)
        {
            if (value > 0)
            {
                return Tuple.Create(1f, Math.Abs(value));
            }
            else if (value < 0)
            {
                return Tuple.Create(-1f, Math.Abs(value));
            }
            else
            {
                return Tuple.Create(0f, Math.Abs(value));
            }
        }

        float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
