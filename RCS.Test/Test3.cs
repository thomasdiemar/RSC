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
            var thrusters = CreateThrusters();

            var engine = new RcsEngine(thrusters);
            var optimiser = new RcsEngineOptimiser();

            var result = optimiser.MaximizeForceX(engine);

            TestContext.WriteLine("=== Thruster Outputs (Max Fx) ===");
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

            const double expectedFx = 2.0;
            const double tolerance = 1e-6;
            Assert.AreEqual(expectedFx, result.ResultantForce.X, tolerance, "F_x does not match maximum achievable value");
        }

        [TestMethod]
        public void ThrusterAllocation_MinFx_12Thrusters()
        {
            var thrusters = CreateThrusters();

            var engine = new RcsEngine(thrusters);
            var optimiser = new RcsEngineOptimiser();

            var result = optimiser.MinimizeForceX(engine);

            TestContext.WriteLine("=== Thruster Outputs (Min Fx) ===");
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

            const double expectedFx = -2.0;
            const double tolerance = 1e-6;
            Assert.AreEqual(expectedFx, result.ResultantForce.X, tolerance, "F_x does not match minimum achievable value");
        }

        [TestMethod]
        public void ThrusterAllocation_MaxTx_12Thrusters()
        {
            var thrusters = CreateThrusters();

            var engine = new RcsEngine(thrusters);
            var optimiser = new RcsEngineOptimiser();

            var result = optimiser.MaximizeTorqueX(engine);

            TestContext.WriteLine("=== Thruster Outputs (Max Tx) ===");
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

            Assert.IsTrue(result.ResultantTorque.X > 0, "Expected positive torque around X axis");
        }

        [TestMethod]
        public void ThrusterAllocation_MinTx_12Thrusters()
        {
            var thrusters = CreateThrusters();

            var engine = new RcsEngine(thrusters);
            var optimiser = new RcsEngineOptimiser();

            var result = optimiser.MinimizeTorqueX(engine);

            TestContext.WriteLine("=== Thruster Outputs (Min Tx) ===");
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

            Assert.IsTrue(result.ResultantTorque.X < 0, "Expected negative torque around X axis");
        }

        private static Dictionary<string, RcsThruster> CreateThrusters()
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
    }
}
