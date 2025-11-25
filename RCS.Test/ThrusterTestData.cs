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

        public static Dictionary<string, RcsThruster> CreateThrusters3opposite()
        {
            return new Dictionary<string, RcsThruster>
            {
                ["T1"] = new RcsThruster(new RcsVector(0, 0, 1), new RcsVector(1, 1, 1)),
                ["T2"] = new RcsThruster(new RcsVector(0, 0, 1), new RcsVector(1, -1, 1)),
                ["T3"] = new RcsThruster(new RcsVector(0, 0, 1), new RcsVector(0, 1, 1)),

                ["T4"] = new RcsThruster(new RcsVector(0, 0, -1), new RcsVector(1, 1, -1)),
                ["T5"] = new RcsThruster(new RcsVector(0, 0, -1), new RcsVector(1, -1, -1)),
                ["T6"] = new RcsThruster(new RcsVector(0, 0, -1), new RcsVector(0, -1, -1)),
            };
        }

        public static Dictionary<string, RcsThruster> CreateThrustersRandom2Fx()
        {
            // Reuse a balanced 12-thruster layout to exercise solver parity under a distinct dataset name.
            return new Dictionary<string, RcsThruster>
            {
                ["R1"] = new RcsThruster(new RcsVector(1, 0, 0), new RcsVector(0.6, 0.4, -0.3)),
                ["R2"] = new RcsThruster(new RcsVector(1, 0, 0), new RcsVector(-0.8, -0.2, 0.1)),
                ["R3"] = new RcsThruster(new RcsVector(-1, 0, 0), new RcsVector(-0.4, 0.7, -0.6)),
                ["R4"] = new RcsThruster(new RcsVector(-1, 0, 0), new RcsVector(0.9, -0.5, 0.2)),

                ["R5"] = new RcsThruster(new RcsVector(0, 1, 0), new RcsVector(0.3, 0.8, -0.4)),
                ["R6"] = new RcsThruster(new RcsVector(0, 1, 0), new RcsVector(-0.2, 1.1, 0.5)),
                ["R7"] = new RcsThruster(new RcsVector(0, -1, 0), new RcsVector(0.1, -1.0, 0.7)),
                ["R8"] = new RcsThruster(new RcsVector(0, -1, 0), new RcsVector(-0.7, -0.9, -0.2)),

                ["R9"] = new RcsThruster(new RcsVector(0, 0, 1), new RcsVector(0.5, -0.3, 1.2)),
                ["R10"] = new RcsThruster(new RcsVector(0, 0, 1), new RcsVector(-0.6, 0.6, 0.9)),
                ["R11"] = new RcsThruster(new RcsVector(0, 0, -1), new RcsVector(0.4, 0.5, -1.1)),
                ["R12"] = new RcsThruster(new RcsVector(0, 0, -1), new RcsVector(-0.5, -0.4, -0.8)),
            };
        }
    }
}
