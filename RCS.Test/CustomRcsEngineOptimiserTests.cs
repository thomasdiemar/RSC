using Microsoft.VisualStudio.TestTools.UnitTesting;
using LinearSolver.Custom;
using LinearSolver.MSF;
using RCS;
using LinearSolver;

namespace ThrusterOptimizationTests
{
    [TestClass]
    public class CustomRcsEngineOptimiserTests
    {
        public TestContext TestContext { get; set; } = null!;

        private readonly RcsEngineOptimiser<LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Simplex.LexicographicGoalSolver> customOptimiser = new RcsEngineOptimiser<LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Simplex.LexicographicGoalSolver>();
        private readonly RcsEngineOptimiser<MsfGoalLinearSolver> msfOptimiser = new RcsEngineOptimiser<MsfGoalLinearSolver>();

        [TestMethod] public void MaxFx_MatchesMsf() => AssertOptimisersMatch(new RcsVector<Fraction>(1, 0, 0), new RcsVector<Fraction>());
        [TestMethod] public void MinFx_MatchesMsf() => AssertOptimisersMatch(new RcsVector<Fraction>(-1, 0, 0), new RcsVector<Fraction>());
        [TestMethod] public void MaxFy_MatchesMsf() => AssertOptimisersMatch(new RcsVector<Fraction>(0, 1, 0), new RcsVector<Fraction>());
        [TestMethod] public void MinFy_MatchesMsf() => AssertOptimisersMatch(new RcsVector<Fraction>(0, -1, 0), new RcsVector<Fraction>());
        [TestMethod] public void MaxFz_MatchesMsf() => AssertOptimisersMatch(new RcsVector<Fraction>(0, 0, 1), new RcsVector<Fraction>());
        [TestMethod] public void MinFz_MatchesMsf() => AssertOptimisersMatch(new RcsVector<Fraction>(0, 0, -1), new RcsVector<Fraction>());

        [TestMethod] public void MaxTx_MatchesMsf() => AssertOptimisersMatch(new RcsVector<Fraction>(), new RcsVector<Fraction>(1, 0, 0));
        [TestMethod] public void MinTx_MatchesMsf() => AssertOptimisersMatch(new RcsVector<Fraction>(), new RcsVector<Fraction>(-1, 0, 0));
        [TestMethod] public void MaxTy_MatchesMsf() => AssertOptimisersMatch(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, 1, 0));
        [TestMethod] public void MinTy_MatchesMsf() => AssertOptimisersMatch(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, -1, 0));
        [TestMethod] public void MaxTz_MatchesMsf() => AssertOptimisersMatch(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, 0, 1));
        [TestMethod] public void MinTz_MatchesMsf() => AssertOptimisersMatch(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, 0, -1));

        [TestMethod] public void ThreeFx_Thrusters_MaxFx_MatchesMsf() => AssertOptimisersMatch3Fx(new RcsVector<Fraction>(1, 0, 0), new RcsVector<Fraction>());
        [TestMethod] public void ThreeFx_Thrusters_MinFx_MatchesMsf() => AssertOptimisersMatch3Fx(new RcsVector<Fraction>(-1, 0, 0), new RcsVector<Fraction>());
        [TestMethod] public void ThreeFx_Thrusters_MaxFy_MatchesMsf() => AssertOptimisersMatch3Fx(new RcsVector<Fraction>(0, 1, 0), new RcsVector<Fraction>());
        [TestMethod] public void ThreeFx_Thrusters_MinFy_MatchesMsf() => AssertOptimisersMatch3Fx(new RcsVector<Fraction>(0, -1, 0), new RcsVector<Fraction>());
        [TestMethod] public void ThreeFx_Thrusters_MaxFz_MatchesMsf() => AssertOptimisersMatch3Fx(new RcsVector<Fraction>(0, 0, 1), new RcsVector<Fraction>());
        [TestMethod] public void ThreeFx_Thrusters_MinFz_MatchesMsf() => AssertOptimisersMatch3Fx(new RcsVector<Fraction>(0, 0, -1), new RcsVector<Fraction>());

        [TestMethod] public void ThreeFx_Thrusters_MaxTx_MatchesMsf() => AssertOptimisersMatch3Fx(new RcsVector<Fraction>(), new RcsVector<Fraction>(1, 0, 0));
        [TestMethod] public void ThreeFx_Thrusters_MinTx_MatchesMsf() => AssertOptimisersMatch3Fx(new RcsVector<Fraction>(), new RcsVector<Fraction>(-1, 0, 0));
        [TestMethod] public void ThreeFx_Thrusters_MaxTy_MatchesMsf() => AssertOptimisersMatch3Fx(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, 1, 0));
        [TestMethod] public void ThreeFx_Thrusters_MinTy_MatchesMsf() => AssertOptimisersMatch3Fx(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, -1, 0));
        [TestMethod] public void ThreeFx_Thrusters_MaxTz_MatchesMsf() => AssertOptimisersMatch3Fx(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, 0, 1));
        [TestMethod] public void ThreeFx_Thrusters_MinTz_MatchesMsf() => AssertOptimisersMatch3Fx(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, 0, -1));

        [TestMethod] public void ThreeOpp_Thrusters_MaxFx_MatchesMsf() => AssertOptimisersMatch3Opp(new RcsVector<Fraction>(1, 0, 0), new RcsVector<Fraction>());
        [TestMethod] public void ThreeOpp_Thrusters_MinFx_MatchesMsf() => AssertOptimisersMatch3Opp(new RcsVector<Fraction>(-1, 0, 0), new RcsVector<Fraction>());
        [TestMethod] public void ThreeOpp_Thrusters_MaxFy_MatchesMsf() => AssertOptimisersMatch3Opp(new RcsVector<Fraction>(0, 1, 0), new RcsVector<Fraction>());
        [TestMethod] public void ThreeOpp_Thrusters_MinFy_MatchesMsf() => AssertOptimisersMatch3Opp(new RcsVector<Fraction>(0, -1, 0), new RcsVector<Fraction>());
        [TestMethod] public void ThreeOpp_Thrusters_MaxFz_MatchesMsf() => AssertOptimisersMatch3Opp(new RcsVector<Fraction>(0, 0, 1), new RcsVector<Fraction>());
        [TestMethod] public void ThreeOpp_Thrusters_MinFz_MatchesMsf() => AssertOptimisersMatch3Opp(new RcsVector<Fraction>(0, 0, -1), new RcsVector<Fraction>());

        [TestMethod] public void ThreeOpp_Thrusters_MaxTx_MatchesMsf() => AssertOptimisersMatch3Opp(new RcsVector<Fraction>(), new RcsVector<Fraction>(1, 0, 0));
        [TestMethod] public void ThreeOpp_Thrusters_MinTx_MatchesMsf() => AssertOptimisersMatch3Opp(new RcsVector<Fraction>(), new RcsVector<Fraction>(-1, 0, 0));
        [TestMethod] public void ThreeOpp_Thrusters_MaxTy_MatchesMsf() => AssertOptimisersMatch3Opp(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, 1, 0));
        [TestMethod] public void ThreeOpp_Thrusters_MinTy_MatchesMsf() => AssertOptimisersMatch3Opp(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, -1, 0));
        [TestMethod] public void ThreeOpp_Thrusters_MaxTz_MatchesMsf() => AssertOptimisersMatch3Opp(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, 0, 1));
        [TestMethod] public void ThreeOpp_Thrusters_MinTz_MatchesMsf() => AssertOptimisersMatch3Opp(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, 0, -1));

        [TestMethod] public void FourFx_Thrusters_MaxFx_MatchesMsf() => AssertOptimisersMatch4Fx(new RcsVector<Fraction>(1, 0, 0), new RcsVector<Fraction>());
        [TestMethod] public void FourFx_Thrusters_MinFx_MatchesMsf() => AssertOptimisersMatch4Fx(new RcsVector<Fraction>(-1, 0, 0), new RcsVector<Fraction>());
        [TestMethod] public void FourFx_Thrusters_MaxFy_MatchesMsf() => AssertOptimisersMatch4Fx(new RcsVector<Fraction>(0, 1, 0), new RcsVector<Fraction>());
        [TestMethod] public void FourFx_Thrusters_MinFy_MatchesMsf() => AssertOptimisersMatch4Fx(new RcsVector<Fraction>(0, -1, 0), new RcsVector<Fraction>());
        [TestMethod] public void FourFx_Thrusters_MaxFz_MatchesMsf() => AssertOptimisersMatch4Fx(new RcsVector<Fraction>(0, 0, 1), new RcsVector<Fraction>());
        [TestMethod] public void FourFx_Thrusters_MinFz_MatchesMsf() => AssertOptimisersMatch4Fx(new RcsVector<Fraction>(0, 0, -1), new RcsVector<Fraction>());

        [TestMethod] public void AllowNonCommandedForces() => AssertOptimisersMatch4Fx(new RcsVector<Fraction>(), new RcsVector<Fraction>(1, 0, 0));
        [TestMethod] public void FourFx_Thrusters_MinTx_MatchesMsf() => AssertOptimisersMatch4Fx(new RcsVector<Fraction>(), new RcsVector<Fraction>(-1, 0, 0));
        [TestMethod] public void FourFx_Thrusters_MaxTy_MatchesMsf() => AssertOptimisersMatch4Fx(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, 1, 0));
        [TestMethod] public void FourFx_Thrusters_MinTy_MatchesMsf() => AssertOptimisersMatch4Fx(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, -1, 0));
        [TestMethod] public void FourFx_Thrusters_MaxTz_MatchesMsf() => AssertOptimisersMatch4Fx(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, 0, 1));
        [TestMethod] public void FourFx_Thrusters_MinTz_MatchesMsf() => AssertOptimisersMatch4Fx(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, 0, -1));

        [TestMethod] public void Random2Fx_Thrusters_MaxFx_MatchesMsf() => AssertOptimisersMatchRandom2Fx(new RcsVector<Fraction>(1, 0, 0), new RcsVector<Fraction>());
        [TestMethod] public void Random2Fx_Thrusters_MinFx_MatchesMsf() => AssertOptimisersMatchRandom2Fx(new RcsVector<Fraction>(-1, 0, 0), new RcsVector<Fraction>());
        [TestMethod] public void Random2Fx_Thrusters_MaxFy_MatchesMsf() => AssertOptimisersMatchRandom2Fx(new RcsVector<Fraction>(0, 1, 0), new RcsVector<Fraction>());
        [TestMethod] public void Random2Fx_Thrusters_MinFy_MatchesMsf() => AssertOptimisersMatchRandom2Fx(new RcsVector<Fraction>(0, -1, 0), new RcsVector<Fraction>());
        [TestMethod] public void Random2Fx_Thrusters_MaxFz_MatchesMsf() => AssertOptimisersMatchRandom2Fx(new RcsVector<Fraction>(0, 0, 1), new RcsVector<Fraction>());
        [TestMethod] public void Random2Fx_Thrusters_MinFz_MatchesMsf() => AssertOptimisersMatchRandom2Fx(new RcsVector<Fraction>(0, 0, -1), new RcsVector<Fraction>());

        [TestMethod] public void Random2Fx_Thrusters_MaxTx_MatchesMsf() => AssertOptimisersMatchRandom2Fx(new RcsVector<Fraction>(), new RcsVector<Fraction>(1, 0, 0));
        [TestMethod] public void Random2Fx_Thrusters_MinTx_MatchesMsf() => AssertOptimisersMatchRandom2Fx(new RcsVector<Fraction>(), new RcsVector<Fraction>(-1, 0, 0));
        [TestMethod] public void Random2Fx_Thrusters_MaxTy_MatchesMsf() => AssertOptimisersMatchRandom2Fx(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, 1, 0));
        [TestMethod] public void Random2Fx_Thrusters_MinTy_MatchesMsf() => AssertOptimisersMatchRandom2Fx(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, -1, 0));
        [TestMethod] public void Random2Fx_Thrusters_MaxTz_MatchesMsf() => AssertOptimisersMatchRandom2Fx(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, 0, 1));
        [TestMethod] public void Random2Fx_Thrusters_MinTz_MatchesMsf() => AssertOptimisersMatchRandom2Fx(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, 0, -1));

        [TestMethod] public void RandomFx_Thrusters_MaxFx_MatchesMsf() => AssertOptimisersMatchRandomFx(new RcsVector<Fraction>(1, 0, 0), new RcsVector<Fraction>());
        [TestMethod] public void RandomFx_Thrusters_MinFx_MatchesMsf() => AssertOptimisersMatchRandomFx(new RcsVector<Fraction>(-1, 0, 0), new RcsVector<Fraction>());
        [TestMethod] public void RandomFx_Thrusters_MaxFy_MatchesMsf() => AssertOptimisersMatchRandomFx(new RcsVector<Fraction>(0, 1, 0), new RcsVector<Fraction>());
        [TestMethod] public void RandomFx_Thrusters_MinFy_MatchesMsf() => AssertOptimisersMatchRandomFx(new RcsVector<Fraction>(0, -1, 0), new RcsVector<Fraction>());
        [TestMethod] public void RandomFx_Thrusters_MaxFz_MatchesMsf() => AssertOptimisersMatchRandomFx(new RcsVector<Fraction>(0, 0, 1), new RcsVector<Fraction>());
        [TestMethod] public void RandomFx_Thrusters_MinFz_MatchesMsf() => AssertOptimisersMatchRandomFx(new RcsVector<Fraction>(0, 0, -1), new RcsVector<Fraction>());

        [TestMethod] public void RandomFx_Thrusters_MaxTx_MatchesMsf() => AssertOptimisersMatchRandomFx(new RcsVector<Fraction>(), new RcsVector<Fraction>(1, 0, 0));
        [TestMethod] public void RandomFx_Thrusters_MinTx_MatchesMsf() => AssertOptimisersMatchRandomFx(new RcsVector<Fraction>(), new RcsVector<Fraction>(-1, 0, 0));
        [TestMethod] public void RandomFx_Thrusters_MaxTy_MatchesMsf() => AssertOptimisersMatchRandomFx(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, 1, 0));
        [TestMethod] public void RandomFx_Thrusters_MinTy_MatchesMsf() => AssertOptimisersMatchRandomFx(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, -1, 0));
        [TestMethod] public void RandomFx_Thrusters_MaxTz_MatchesMsf() => AssertOptimisersMatchRandomFx(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, 0, 1));
        [TestMethod] public void RandomFx_Thrusters_MinTz_MatchesMsf() => AssertOptimisersMatchRandomFx(new RcsVector<Fraction>(), new RcsVector<Fraction>(0, 0, -1));

        private void AssertOptimisersMatch(RcsVector<Fraction> desiredForce, RcsVector<Fraction> desiredTorque)
        {
            var engine = new RcsEngine(ThrusterTestData.CreateThrusters());
            var command = new RcsCommand(desiredForce, desiredTorque, allowNonCommandedForces: true);

            var customResult = customOptimiser.Optimise(engine, command).Last().Result;
            var msfResult = msfOptimiser.Optimise(engine, command).Last().Result;

            LogResult("Custom Optimiser", customResult);
            LogResult("MSF Optimiser", msfResult);

            const double tolerance = 1e-6;
            Assert.AreEqual(msfResult.ResultantForce.X, customResult.ResultantForce.X, tolerance, "Fx mismatch");
            Assert.AreEqual(msfResult.ResultantForce.Y, customResult.ResultantForce.Y, tolerance, "Fy mismatch");
            Assert.AreEqual(msfResult.ResultantForce.Z, customResult.ResultantForce.Z, tolerance, "Fz mismatch");
            Assert.AreEqual(msfResult.ResultantTorque.X, customResult.ResultantTorque.X, tolerance, "Tx mismatch");
            Assert.AreEqual(msfResult.ResultantTorque.Y, customResult.ResultantTorque.Y, tolerance, "Ty mismatch");
            Assert.AreEqual(msfResult.ResultantTorque.Z, customResult.ResultantTorque.Z, tolerance, "Tz mismatch");
        }

        private void AssertOptimisersMatch3Fx(RcsVector<Fraction> desiredForce, RcsVector<Fraction> desiredTorque)
        {
            var engine = new RcsEngine(ThrusterTestData.CreateThrusters3Fx());
            var command = new RcsCommand(desiredForce, desiredTorque);

            var customResult = customOptimiser.Optimise(engine, command).Last().Result;
            var msfResult = msfOptimiser.Optimise(engine, command).Last().Result;

            LogResult("Custom Optimiser (3Fx)", customResult);
            LogResult("MSF Optimiser (3Fx)", msfResult);

            const double tolerance = 1e-6;
            foreach (var kvp in customResult.ThrusterOutputs)
            {
                double expected = msfResult.ThrusterOutputs[kvp.Key];
                Assert.AreEqual(expected, kvp.Value, tolerance, $"Thruster {kvp.Key} mismatch");
            }

            Assert.AreEqual(msfResult.ResultantForce.X, customResult.ResultantForce.X, tolerance, "Fx mismatch");
            Assert.AreEqual(msfResult.ResultantForce.Y, customResult.ResultantForce.Y, tolerance, "Fy mismatch");
            Assert.AreEqual(msfResult.ResultantForce.Z, customResult.ResultantForce.Z, tolerance, "Fz mismatch");
            Assert.AreEqual(msfResult.ResultantTorque.X, customResult.ResultantTorque.X, tolerance, "Tx mismatch");
            Assert.AreEqual(msfResult.ResultantTorque.Y, customResult.ResultantTorque.Y, tolerance, "Ty mismatch");
            Assert.AreEqual(msfResult.ResultantTorque.Z, customResult.ResultantTorque.Z, tolerance, "Tz mismatch");
        }

        private void AssertOptimisersMatch3Opp(RcsVector<Fraction> desiredForce, RcsVector<Fraction> desiredTorque)
        {
            var engine = new RcsEngine(ThrusterTestData.CreateThrusters3opposite());
            var command = new RcsCommand(desiredForce, desiredTorque);

            var customResult = customOptimiser.Optimise(engine, command).Last().Result;
            var msfResult = msfOptimiser.Optimise(engine, command).Last().Result;

            LogResult("Custom Optimiser (3Opp)", customResult);
            LogResult("MSF Optimiser (3Opp)", msfResult);

            const double tolerance = 1e-6;
            foreach (var kvp in customResult.ThrusterOutputs)
            {
                double expected = msfResult.ThrusterOutputs[kvp.Key];
                Assert.AreEqual(expected, kvp.Value, tolerance, $"Thruster {kvp.Key} mismatch");
            }

            Assert.AreEqual(msfResult.ResultantForce.X, customResult.ResultantForce.X, tolerance, "Fx mismatch");
            Assert.AreEqual(msfResult.ResultantForce.Y, customResult.ResultantForce.Y, tolerance, "Fy mismatch");
            Assert.AreEqual(msfResult.ResultantForce.Z, customResult.ResultantForce.Z, tolerance, "Fz mismatch");
            Assert.AreEqual(msfResult.ResultantTorque.X, customResult.ResultantTorque.X, tolerance, "Tx mismatch");
            Assert.AreEqual(msfResult.ResultantTorque.Y, customResult.ResultantTorque.Y, tolerance, "Ty mismatch");
            Assert.AreEqual(msfResult.ResultantTorque.Z, customResult.ResultantTorque.Z, tolerance, "Tz mismatch");
        }

        private void AssertOptimisersMatch4Fx(RcsVector<Fraction> desiredForce, RcsVector<Fraction> desiredTorque)
        {
            var engine = new RcsEngine(ThrusterTestData.CreateThrusters4Fx());
            var command = new RcsCommand(desiredForce, desiredTorque, allowNonCommandedForces: true, allowNonCommandedTorques: true);

            var customResult = customOptimiser.Optimise(engine, command).Last().Result;
            var msfResult = msfOptimiser.Optimise(engine, command).Last().Result;

            LogResult("Custom Optimiser (4Fx)", customResult);
            LogResult("MSF Optimiser (4Fx)", msfResult);

            const double tolerance = 1e-6;
            foreach (var kvp in customResult.ThrusterOutputs)
            {
                double expected = msfResult.ThrusterOutputs[kvp.Key];
                Assert.AreEqual(expected, kvp.Value, tolerance, $"Thruster {kvp.Key} mismatch");
            }

            Assert.AreEqual(msfResult.ResultantForce.X, customResult.ResultantForce.X, tolerance, "Fx mismatch");
            Assert.AreEqual(msfResult.ResultantForce.Y, customResult.ResultantForce.Y, tolerance, "Fy mismatch");
            Assert.AreEqual(msfResult.ResultantForce.Z, customResult.ResultantForce.Z, tolerance, "Fz mismatch");
            Assert.AreEqual(msfResult.ResultantTorque.X, customResult.ResultantTorque.X, tolerance, "Tx mismatch");
            Assert.AreEqual(msfResult.ResultantTorque.Y, customResult.ResultantTorque.Y, tolerance, "Ty mismatch");
            Assert.AreEqual(msfResult.ResultantTorque.Z, customResult.ResultantTorque.Z, tolerance, "Tz mismatch");
        }

        private void AssertOptimisersMatchRandom2Fx(RcsVector<Fraction> desiredForce, RcsVector<Fraction> desiredTorque)
        {
            var engine = new RcsEngine(ThrusterTestData.CreateThrustersRandom2Fx());
            var command = new RcsCommand(desiredForce, desiredTorque, allowNonCommandedForces: true, allowNonCommandedTorques: true);

            var customResult = customOptimiser.Optimise(engine, command).Last().Result;
            var msfResult = msfOptimiser.Optimise(engine, command).Last().Result;

            LogResult("Custom Optimiser (Random2Fx)", customResult);
            LogResult("MSF Optimiser (Random2Fx)", msfResult);

            // For underdetermined systems, multiple optimal solutions exist
            // Only compare resultant forces/torques, not individual thruster values
            const double tolerance = 1e-6;

            Assert.AreEqual(msfResult.ResultantForce.X, customResult.ResultantForce.X, tolerance, "Fx mismatch");
            Assert.AreEqual(msfResult.ResultantForce.Y, customResult.ResultantForce.Y, tolerance, "Fy mismatch");
            Assert.AreEqual(msfResult.ResultantForce.Z, customResult.ResultantForce.Z, tolerance, "Fz mismatch");
            Assert.AreEqual(msfResult.ResultantTorque.X, customResult.ResultantTorque.X, tolerance, "Tx mismatch");
            Assert.AreEqual(msfResult.ResultantTorque.Y, customResult.ResultantTorque.Y, tolerance, "Ty mismatch");
            Assert.AreEqual(msfResult.ResultantTorque.Z, customResult.ResultantTorque.Z, tolerance, "Tz mismatch");
        }

        private void AssertOptimisersMatchRandomFx(RcsVector<Fraction> desiredForce, RcsVector<Fraction> desiredTorque)
        {
            var engine = new RcsEngine(ThrusterTestData.CreateThrustersRandomFx());
            var command = new RcsCommand(desiredForce, desiredTorque, allowNonCommandedForces: true, allowNonCommandedTorques: true);

            var customResult = customOptimiser.Optimise(engine, command).Last().Result;
            var msfResult = msfOptimiser.Optimise(engine, command).Last().Result;

            LogResult("Custom Optimiser (RandomFx)", customResult);
            LogResult("MSF Optimiser (RandomFx)", msfResult);

            // For underdetermined systems, multiple optimal solutions exist
            // Only compare resultant forces/torques, not individual thruster values
            const double tolerance = 1e-6;

            Assert.AreEqual(msfResult.ResultantForce.X, customResult.ResultantForce.X, tolerance, "Fx mismatch");
            Assert.AreEqual(msfResult.ResultantForce.Y, customResult.ResultantForce.Y, tolerance, "Fy mismatch");
            Assert.AreEqual(msfResult.ResultantForce.Z, customResult.ResultantForce.Z, tolerance, "Fz mismatch");
            Assert.AreEqual(msfResult.ResultantTorque.X, customResult.ResultantTorque.X, tolerance, "Tx mismatch");
            Assert.AreEqual(msfResult.ResultantTorque.Y, customResult.ResultantTorque.Y, tolerance, "Ty mismatch");
            Assert.AreEqual(msfResult.ResultantTorque.Z, customResult.ResultantTorque.Z, tolerance, "Tz mismatch");
        }

        private void LogResult(string title, RcsEngineResult result)
        {
            TestContext.WriteLine($"=== {title} Thruster Outputs ===");
            foreach (var kvp in result.ThrusterOutputs)
                TestContext.WriteLine($"{kvp.Key}: {kvp.Value:F6}");

            TestContext.WriteLine($"Resultant Force: Fx={result.ResultantForce.X:F6}, Fy={result.ResultantForce.Y:F6}, Fz={result.ResultantForce.Z:F6}");
            TestContext.WriteLine($"Resultant Torque: Tx={result.ResultantTorque.X:F6}, Ty={result.ResultantTorque.Y:F6}, Tz={result.ResultantTorque.Z:F6}");
        }
    }
}
