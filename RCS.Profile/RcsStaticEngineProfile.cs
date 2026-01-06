using LinearSolver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RCS.Profile
{
    public class RcsStaticEngineProfile
    {
        private Dictionary<RcsProfileCommand, IReadOnlyDictionary<string, Fraction>> ThrusterOutputs =
            new Dictionary<RcsProfileCommand, IReadOnlyDictionary<string, Fraction>>();

        public void AddProfile(RcsProfileCommand command, IReadOnlyDictionary<string, Fraction> outputs)
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

        IReadOnlyDictionary<string, Fraction> GetSingleProfile(RcsProfileCommand command)
        {
            var c = ThrusterOutputs.ContainsKey(command) ? ThrusterOutputs[command] : null;
            return c;
        }

        public IReadOnlyDictionary<string, Fraction> GetProfile(RcsProfileCommand command)
        {
            var desiredForces = CreateProfileRscVector(command.DesiredForce)
                .SelectMany(recsvector =>
                {
                    var thrusters = GetSingleProfile(new RcsProfileCommand(recsvector.Item1, RcsProfileCommand.NOCOMMAND));
                    thrusters.Select(thruster => new KeyValuePair<string, Fraction>(thruster.Key, thruster.Value * recsvector.Item2));
                    return thrusters;
                })
                ;
            var desiredTorques = CreateProfileRscVector(command.DesiredTorque)
                .SelectMany(recsvector =>
                {
                    var thrusters = GetSingleProfile(new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, recsvector.Item1));
                    thrusters.Select(thruster => new KeyValuePair<string, Fraction>(thruster.Key, thruster.Value * recsvector.Item2));
                    return thrusters;
                })
                ;
            var profiles = desiredForces.Union(desiredTorques)
                .GroupBy(profile => profile.Key)
                .Select(group => new KeyValuePair<string, Fraction>(group.Key, group.Select(kv => kv.Value).Aggregate((a, b) => (a + b) / 2)))
                .ToDictionary(kv => kv.Key, kv => kv.Value)
                ;

            return profiles;
        }

        List<Tuple<RcsVector<Fraction>, Fraction>> CreateProfileRscVector(RcsVector<Fraction> vector)
        {
            var fractions = new List<Tuple<RcsVector<Fraction>, Fraction>>();
            var fraction = CreateProfileFraction(vector.X);
            if (fraction.Item1 != 0)
            {
                fractions.Add(Tuple.Create(new RcsVector<Fraction>(fraction.Item1, 0, 0),fraction.Item2));
            }
            fraction = CreateProfileFraction(vector.Y);
            if (fraction.Item1 != 0)
            {
                fractions.Add(Tuple.Create(new RcsVector<Fraction>(0, fraction.Item1, 0),fraction.Item2));
            }
            fraction = CreateProfileFraction(vector.Z);
            if (fraction.Item1 != 0)
            {
                fractions.Add(Tuple.Create(new RcsVector<Fraction>(0, 0, fraction.Item1),fraction.Item2));
            }
            return fractions;
        }

        Tuple<Fraction, Fraction> CreateProfileFraction(Fraction value)
        {
            if (value > 0)
            {
                return Tuple.Create(new Fraction(1), Fraction.Abs(value));
            }
            else if (value < 0)
            {
                return Tuple.Create(new Fraction(-1), Fraction.Abs(value));
            }
            else
            {
                return Tuple.Create(new Fraction(0), Fraction.Abs(value));
            }
        }

    }
}
