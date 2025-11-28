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
                ["T1"] = new RcsThruster(new RcsVector<int>(1, 0, 0), new RcsVector<int>(2, 1, 1)),
                ["T2"] = new RcsThruster(new RcsVector<int>(1, 0, 0), new RcsVector<int>(2, -1, -1)),
                ["T3"] = new RcsThruster(new RcsVector<int>(-1, 0, 0), new RcsVector<int>(-2, 1, -1)),
                ["T4"] = new RcsThruster(new RcsVector<int>(-1, 0, 0), new RcsVector<int>(-2, -1, 1)),
                ["T5"] = new RcsThruster(new RcsVector<int>(0, 1, 0), new RcsVector<int>(1, 2, -1)),
                ["T6"] = new RcsThruster(new RcsVector<int>(0, 1, 0), new RcsVector<int>(-1, 2, 1)),
                ["T7"] = new RcsThruster(new RcsVector<int>(0, -1, 0), new RcsVector<int>(1, -2, 1)),
                ["T8"] = new RcsThruster(new RcsVector<int>(0, -1, 0), new RcsVector<int>(-1, -2, -1)),
                ["T9"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(1, -1, 2)),
                ["T10"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(-1, 1, 2)),
                ["T11"] = new RcsThruster(new RcsVector<int>(0, 0, -1), new RcsVector<int>(1, 1, -2)),
                ["T12"] = new RcsThruster(new RcsVector<int>(0, 0, -1), new RcsVector<int>(-1, -1, -2)),
            };
        }

        public static Dictionary<string, RcsThruster> CreateThrusters3Fx()
        {
            return new Dictionary<string, RcsThruster>
            {
                ["T1"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(1, -1, 0)),
                ["T2"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(-1, -1, 0)),
                ["T3"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(0, 1, 0)),
            };
        }

        public static Dictionary<string, RcsThruster> CreateThrusters4Fx()
        {
            return new Dictionary<string, RcsThruster>
            {
                ["T1"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(1, 1, 0)),
                ["T2"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(1, -1, 0)),
                ["T3"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(-1, 1, 0)),
                ["T4"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(-1, -1, 0)),
            };
        }

        public static Dictionary<string, RcsThruster> CreateThrusters3opposite()
        {
            return new Dictionary<string, RcsThruster>
            {
                ["T1"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(1, 1, 1)),
                ["T2"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(1, -1, 1)),
                ["T3"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(0, 1, 1)),

                ["T4"] = new RcsThruster(new RcsVector<int>(0, 0, -1), new RcsVector<int>(1, 1, -1)),
                ["T5"] = new RcsThruster(new RcsVector<int>(0, 0, -1), new RcsVector<int>(1, -1, -1)),
                ["T6"] = new RcsThruster(new RcsVector<int>(0, 0, -1), new RcsVector<int>(0, -1, -1)),
            };
        }
        public static Dictionary<string, RcsThruster> CreateThrustersRandom2Fx()
        {
            // Reuse a balanced 12-thruster layout to exercise solver parity under a distinct dataset name.
            return new Dictionary<string, RcsThruster>
            {
                ["R1"] = new RcsThruster(new RcsVector<int>(1, 0, 0), new RcsVector<int>(6, 4, -3)),
                ["R2"] = new RcsThruster(new RcsVector<int>(1, 0, 0), new RcsVector<int>(-8, -2, 1)),
                ["R3"] = new RcsThruster(new RcsVector<int>(-1, 0, 0), new RcsVector<int>(-4, 7, -6)),
                ["R4"] = new RcsThruster(new RcsVector<int>(-1, 0, 0), new RcsVector<int>(9, -5, 2)),

                ["R5"] = new RcsThruster(new RcsVector<int>(0, 1, 0), new RcsVector<int>(3, 8, -4)),
                ["R6"] = new RcsThruster(new RcsVector<int>(0, 1, 0), new RcsVector<int>(-2, 11, 5)),
                ["R7"] = new RcsThruster(new RcsVector<int>(0, -1, 0), new RcsVector<int>(1, -10, 7)),
                ["R8"] = new RcsThruster(new RcsVector<int>(0, -1, 0), new RcsVector<int>(-7, -9, -2)),

                ["R9"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(5, -3, 12)),
                ["R10"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(-6, 6, 9)),
                ["R11"] = new RcsThruster(new RcsVector<int>(0, 0, -1), new RcsVector<int>(4, 5, -11)),
                ["R12"] = new RcsThruster(new RcsVector<int>(0, 0, -1), new RcsVector<int>(-5, -4, -8)),
            };
        }
        public static Dictionary<string, RcsThruster> CreateThrustersRandomFx()
        {
            var rand = new Random();
            var a = rand.Next(100);
            var b = rand.Next(100);
            var c = rand.Next(100);
            var d = rand.Next(100);
            return new Dictionary<string, RcsThruster>
            {
                ["T1"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(a, b, 1)),
                ["T2"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(-1*c, d, 1))
            };
        }
    }
}
