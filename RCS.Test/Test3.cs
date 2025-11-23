using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCS;
using System.Collections.Generic;
using System.Linq;

namespace ThrusterOptimizationTests
{
    [TestClass]
    public class ThrusterControlTests2
    {
        public TestContext TestContext { get; set; } = default!;

        [TestMethod]
        public void ThrusterAllocation_MaxFx_12Thrusters()
        {
            var thrusters = ThrusterTestData.CreateThrusters();

            var engine = new RcsEngine(thrusters);
            var optimiser = new RcsEngineOptimiser();

            var command = new RcsCommand(new RcsVector(1, 0, 0), new RcsVector());
            var result = optimiser.Optimise(engine, command);

            LogResult("Max Fx", result);

            const double expectedFx = 2.0;
            const double tolerance = 1e-6;
            Assert.AreEqual(expectedFx, result.ResultantForce.X, tolerance, "F_x does not match maximum achievable value");
        }

        [TestMethod]
        public void ThrusterAllocation_MinFx_12Thrusters()
        {
            var thrusters = ThrusterTestData.CreateThrusters();

            var engine = new RcsEngine(thrusters);
            var optimiser = new RcsEngineOptimiser();

            var command = new RcsCommand(new RcsVector(-1, 0, 0), new RcsVector());
            var result = optimiser.Optimise(engine, command);

            LogResult("Min Fx", result);

            const double expectedFx = -2.0;
            const double tolerance = 1e-6;
            Assert.AreEqual(expectedFx, result.ResultantForce.X, tolerance, "F_x does not match minimum achievable value");
        }

        [TestMethod]
        public void ThrusterAllocation_MaxFy_12Thrusters()
        {
            var thrusters = ThrusterTestData.CreateThrusters();

            var engine = new RcsEngine(thrusters);
            var optimiser = new RcsEngineOptimiser();

            var command = new RcsCommand(new RcsVector(0, 1, 0), new RcsVector());
            var result = optimiser.Optimise(engine, command);

            LogResult("Max Fy", result);

            const double expectedFy = 2.0;
            const double tolerance = 1e-6;
            Assert.AreEqual(expectedFy, result.ResultantForce.Y, tolerance, "F_y does not match maximum achievable value");
        }

        [TestMethod]
        public void ThrusterAllocation_MinFy_12Thrusters()
        {
            var thrusters = ThrusterTestData.CreateThrusters();

            var engine = new RcsEngine(thrusters);
            var optimiser = new RcsEngineOptimiser();

            var command = new RcsCommand(new RcsVector(0, -1, 0), new RcsVector());
            var result = optimiser.Optimise(engine, command);

            LogResult("Min Fy", result);

            const double expectedFy = -2.0;
            const double tolerance = 1e-6;
            Assert.AreEqual(expectedFy, result.ResultantForce.Y, tolerance, "F_y does not match minimum achievable value");
        }

        [TestMethod]
        public void ThrusterAllocation_MaxFz_12Thrusters()
        {
            var thrusters = ThrusterTestData.CreateThrusters();

            var engine = new RcsEngine(thrusters);
            var optimiser = new RcsEngineOptimiser();

            var command = new RcsCommand(new RcsVector(0, 0, 1), new RcsVector());
            var result = optimiser.Optimise(engine, command);

            LogResult("Max Fz", result);

            const double expectedFz = 2.0;
            const double tolerance = 1e-6;
            Assert.AreEqual(expectedFz, result.ResultantForce.Z, tolerance, "F_z does not match maximum achievable value");
        }

        [TestMethod]
        public void ThrusterAllocation_MinFz_12Thrusters()
        {
            var thrusters = ThrusterTestData.CreateThrusters();

            var engine = new RcsEngine(thrusters);
            var optimiser = new RcsEngineOptimiser();

            var command = new RcsCommand(new RcsVector(0, 0, -1), new RcsVector());
            var result = optimiser.Optimise(engine, command);

            LogResult("Min Fz", result);

            const double expectedFz = -2.0;
            const double tolerance = 1e-6;
            Assert.AreEqual(expectedFz, result.ResultantForce.Z, tolerance, "F_z does not match minimum achievable value");
        }

        [TestMethod]
        public void ThrusterAllocation_MaxTx_12Thrusters()
        {
            var thrusters = ThrusterTestData.CreateThrusters();

            var engine = new RcsEngine(thrusters);
            var optimiser = new RcsEngineOptimiser();

            var command = new RcsCommand(new RcsVector(), new RcsVector(1, 0, 0));
            var result = optimiser.Optimise(engine, command);

            LogResult("Max Tx", result);

            Assert.IsTrue(result.ResultantTorque.X > 0, "Expected positive torque around X axis");
        }

        [TestMethod]
        public void ThrusterAllocation_MinTx_12Thrusters()
        {
            var thrusters = ThrusterTestData.CreateThrusters();

            var engine = new RcsEngine(thrusters);
            var optimiser = new RcsEngineOptimiser();

            var command = new RcsCommand(new RcsVector(), new RcsVector(-1, 0, 0));
            var result = optimiser.Optimise(engine, command);

            LogResult("Min Tx", result);

            Assert.IsTrue(result.ResultantTorque.X < 0, "Expected negative torque around X axis");
        }

        [TestMethod]
        public void ThrusterAllocation_MaxTy_12Thrusters()
        {
            var thrusters = ThrusterTestData.CreateThrusters();

            var engine = new RcsEngine(thrusters);
            var optimiser = new RcsEngineOptimiser();

            var command = new RcsCommand(new RcsVector(), new RcsVector(0, 1, 0));
            var result = optimiser.Optimise(engine, command);

            LogResult("Max Ty", result);

            Assert.IsTrue(result.ResultantTorque.Y > 0, "Expected positive torque around Y axis");
        }

        [TestMethod]
        public void ThrusterAllocation_MinTy_12Thrusters()
        {
            var thrusters = ThrusterTestData.CreateThrusters();

            var engine = new RcsEngine(thrusters);
            var optimiser = new RcsEngineOptimiser();

            var command = new RcsCommand(new RcsVector(), new RcsVector(0, -1, 0));
            var result = optimiser.Optimise(engine, command);

            LogResult("Min Ty", result);

            Assert.IsTrue(result.ResultantTorque.Y < 0, "Expected negative torque around Y axis");
        }

        [TestMethod]
        public void ThrusterAllocation_MaxTz_12Thrusters()
        {
            var thrusters = ThrusterTestData.CreateThrusters();

            var engine = new RcsEngine(thrusters);
            var optimiser = new RcsEngineOptimiser();

            var command = new RcsCommand(new RcsVector(), new RcsVector(0, 0, 1));
            var result = optimiser.Optimise(engine, command);

            LogResult("Max Tz", result);

            Assert.IsTrue(result.ResultantTorque.Z > 0, "Expected positive torque around Z axis");
        }

        [TestMethod]
        public void ThrusterAllocation_MinTz_12Thrusters()
        {
            var thrusters = ThrusterTestData.CreateThrusters();

            var engine = new RcsEngine(thrusters);
            var optimiser = new RcsEngineOptimiser();

            var command = new RcsCommand(new RcsVector(), new RcsVector(0, 0, -1));
            var result = optimiser.Optimise(engine, command);

            LogResult("Min Tz", result);

            Assert.IsTrue(result.ResultantTorque.Z < 0, "Expected negative torque around Z axis");
        }


        private void LogResult(string title, RcsEngineResult result)
        {
            TestContext.WriteLine($"=== Thruster Outputs ({title}) ===");
            foreach (var output in result.ThrusterOutputs.OrderBy(t => t.Key))
                TestContext.WriteLine($"{output.Key}: {output.Value:F6}");

            TestContext.WriteLine("\n=== Resultant Force ===");
            TestContext.WriteLine($"Fx: {result.ResultantForce.X:F6}");
            TestContext.WriteLine($"Fy: {result.ResultantForce.Y:F6}");
            TestContext.WriteLine($"Fz: {result.ResultantForce.Z:F6}");

            TestContext.WriteLine("\n=== Resultant Torque ===");
            TestContext.WriteLine($"Tx: {result.ResultantTorque.X:F6}");
            TestContext.WriteLine($"Ty: {result.ResultantTorque.Y:F6}");
            TestContext.WriteLine($"Tz: {result.ResultantTorque.Z:F6}");
        }
    }
}
