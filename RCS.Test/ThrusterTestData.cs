using RCS;
using System.Collections.Generic;

namespace ThrusterOptimizationTests
{
    internal static class ThrusterTestData
    {
        public static Dictionary<string, RcsThruster> CreateThrusters()
        {
            return new Dictionary<string, RcsThruster>
            {
                ["T1"] = new RcsThruster(new RcsVector(1, 0, 0), new RcsVector(1, 0.5, 0.5)),
                ["T2"] = new RcsThruster(new RcsVector(1, 0, 0), new RcsVector(1, -0.5, -0.5)),
                ["T3"] = new RcsThruster(new RcsVector(-1, 0, 0), new RcsVector(-1, 0.5, -0.5)),
                ["T4"] = new RcsThruster(new RcsVector(-1, 0, 0), new RcsVector(-1, -0.5, 0.5)),
                ["T5"] = new RcsThruster(new RcsVector(0, 1, 0), new RcsVector(0.5, 1, -0.5)),
                ["T6"] = new RcsThruster(new RcsVector(0, 1, 0), new RcsVector(-0.5, 1, 0.5)),
                ["T7"] = new RcsThruster(new RcsVector(0, -1, 0), new RcsVector(0.5, -1, 0.5)),
                ["T8"] = new RcsThruster(new RcsVector(0, -1, 0), new RcsVector(-0.5, -1, -0.5)),
                ["T9"] = new RcsThruster(new RcsVector(0, 0, 1), new RcsVector(0.5, -0.5, 1)),
                ["T10"] = new RcsThruster(new RcsVector(0, 0, 1), new RcsVector(-0.5, 0.5, 1)),
                ["T11"] = new RcsThruster(new RcsVector(0, 0, -1), new RcsVector(0.5, 0.5, -1)),
                ["T12"] = new RcsThruster(new RcsVector(0, 0, -1), new RcsVector(-0.5, -0.5, -1)),
            };
        }

        public static Dictionary<string, RcsThruster> CreateThrusters3Fx()
        {
            return new Dictionary<string, RcsThruster>
            {
                ["T1"] = new RcsThruster(new RcsVector(0, 0, 1), new RcsVector(1, -1, 0)),
                ["T2"] = new RcsThruster(new RcsVector(0, 0, 1), new RcsVector(-1, -1, 0)),
                ["T3"] = new RcsThruster(new RcsVector(0, 0, 1), new RcsVector(0, 1, 0)),
            };
        }

        public static Dictionary<string, RcsThruster> CreateThrusters4Fx()
        {
            return new Dictionary<string, RcsThruster>
            {
                ["T1"] = new RcsThruster(new RcsVector(0, 0, 1), new RcsVector(1, 1, 0)),
                ["T2"] = new RcsThruster(new RcsVector(0, 0, 1), new RcsVector(1, -1, 0)),
                ["T3"] = new RcsThruster(new RcsVector(0, 0, 1), new RcsVector(-1, 1, 0)),
                ["T4"] = new RcsThruster(new RcsVector(0, 0, 1), new RcsVector(-1, -1, 0)),
            };
        }
    }
}
