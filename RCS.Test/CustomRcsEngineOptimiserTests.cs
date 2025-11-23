using LinearSolver.Custom;
using RCS;
using RCS.Custom;
using RCS.MSF;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrusterOptimizationTests
{
    [TestClass]
    public class CustomRcsEngineOptimiserTests
    {
        public TestContext TestContext { get; set; } = null!;

        private readonly CustomRcsEngineOptimiser customOptimiser = new CustomRcsEngineOptimiser();
        private readonly MfsRcsEngineOptimiser mfsOptimiser = new MfsRcsEngineOptimiser();

        [TestMethod] public void MaxFx_MatchesMsf() => AssertOptimisersMatch(new RcsVector(1, 0, 0), new RcsVector());
        [TestMethod] public void MinFx_MatchesMsf() => AssertOptimisersMatch(new RcsVector(-1, 0, 0), new RcsVector());
        [TestMethod] public void MaxFy_MatchesMsf() => AssertOptimisersMatch(new RcsVector(0, 1, 0), new RcsVector());
        [TestMethod] public void MinFy_MatchesMsf() => AssertOptimisersMatch(new RcsVector(0, -1, 0), new RcsVector());
        [TestMethod] public void MaxFz_MatchesMsf() => AssertOptimisersMatch(new RcsVector(0, 0, 1), new RcsVector());
        [TestMethod] public void MinFz_MatchesMsf() => AssertOptimisersMatch(new RcsVector(0, 0, -1), new RcsVector());

        [TestMethod] public void MaxTx_MatchesMsf() => AssertOptimisersMatch(new RcsVector(), new RcsVector(1, 0, 0));
        [TestMethod] public void MinTx_MatchesMsf() => AssertOptimisersMatch(new RcsVector(), new RcsVector(-1, 0, 0));
        [TestMethod] public void MaxTy_MatchesMsf() => AssertOptimisersMatch(new RcsVector(), new RcsVector(0, 1, 0));
        [TestMethod] public void MinTy_MatchesMsf() => AssertOptimisersMatch(new RcsVector(), new RcsVector(0, -1, 0));
        [TestMethod] public void MaxTz_MatchesMsf() => AssertOptimisersMatch(new RcsVector(), new RcsVector(0, 0, 1));
        [TestMethod] public void MinTz_MatchesMsf() => AssertOptimisersMatch(new RcsVector(), new RcsVector(0, 0, -1));

        [TestMethod] public void ThreeFx_Thrusters_MaxFx_MatchesMsf() => AssertOptimisersMatch3Fx(new RcsVector(1, 0, 0), new RcsVector());

        private void AssertOptimisersMatch(RcsVector desiredForce, RcsVector desiredTorque)
        {
            var engine = new RcsEngine(ThrusterTestData.CreateThrusters());
            var command = new RcsCommand(desiredForce, desiredTorque);

            var customResult = customOptimiser.Optimise(engine, command);
            var msfResult = mfsOptimiser.Optimise(engine, command);

            LogResult("Custom Optimiser", customResult);
            LogResult("MSF Optimiser", msfResult);

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

        private void AssertOptimisersMatch3Fx(RcsVector desiredForce, RcsVector desiredTorque)
        {
            var engine = new RcsEngine(ThrusterTestData.CreateThrusters3Fx());
            var command = new RcsCommand(desiredForce, desiredTorque);

            var customResult = customOptimiser.Optimise(engine, command);
            var msfResult = mfsOptimiser.Optimise(engine, command);

            LogResult("Custom Optimiser (3Fx)", customResult);
            LogResult("MSF Optimiser (3Fx)", msfResult);

            const double tolerance = 1e-6;
            foreach (var kvp in customResult.ThrusterOutputs)
            {
                double expected = msfResult.ThrusterOutputs[kvp.Key];
                Assert.AreEqual(expected, kvp.Value, tolerance, $"Thruster {kvp.Key} mismatch");
            }

            Assert.AreEqual(msfResult.ResultantForce.X, customResult.ResultantForce.X, tolerance, "Fx mismatch");
            Assert.AreEqual(msfResult.ResultantTorque.X, customResult.ResultantTorque.X, tolerance, "Tx mismatch");
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
