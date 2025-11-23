using LinearSolver;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCS;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThrusterOptimizationTests
{
    [TestClass]
    public class LinearSolverComparisonTests
    {
        private const double Tolerance = 1e-6;

        public TestContext TestContext { get; set; } = null!;

        private static readonly KeyValuePair<string, RcsThruster>[] Thrusters =
            ThrusterTestData.CreateThrusters().OrderBy(t => t.Key).ToArray();

        private static readonly MatrixBuildResult CoefficientData = BuildCoefficientMatrix(Thrusters);
        private static readonly double[,] CoefficientMatrix = CoefficientData.Matrix;
        private static readonly string[] MatrixLogLines = CoefficientData.Log.ToArray();
        private static bool matrixLogged;

        [TestMethod] public void LinearSolver_MaxFx_MatchesMfs() => AssertSolverMatches(new RcsVector(1, 0, 0), new RcsVector());
        [TestMethod] public void LinearSolver_MinFx_MatchesMfs() => AssertSolverMatches(new RcsVector(-1, 0, 0), new RcsVector());
        [TestMethod] public void LinearSolver_MaxFy_MatchesMfs() => AssertSolverMatches(new RcsVector(0, 1, 0), new RcsVector());
        [TestMethod] public void LinearSolver_MinFy_MatchesMfs() => AssertSolverMatches(new RcsVector(0, -1, 0), new RcsVector());
        [TestMethod] public void LinearSolver_MaxFz_MatchesMfs() => AssertSolverMatches(new RcsVector(0, 0, 1), new RcsVector());
        [TestMethod] public void LinearSolver_MinFz_MatchesMfs() => AssertSolverMatches(new RcsVector(0, 0, -1), new RcsVector());

        [TestMethod] public void LinearSolver_MaxTx_MatchesMfs() => AssertSolverMatches(new RcsVector(), new RcsVector(1, 0, 0));
        [TestMethod] public void LinearSolver_MinTx_MatchesMfs() => AssertSolverMatches(new RcsVector(), new RcsVector(-1, 0, 0));
        [TestMethod] public void LinearSolver_MaxTy_MatchesMfs() => AssertSolverMatches(new RcsVector(), new RcsVector(0, 1, 0));
        [TestMethod] public void LinearSolver_MinTy_MatchesMfs() => AssertSolverMatches(new RcsVector(), new RcsVector(0, -1, 0));
        [TestMethod] public void LinearSolver_MaxTz_MatchesMfs() => AssertSolverMatches(new RcsVector(), new RcsVector(0, 0, 1));
        [TestMethod] public void LinearSolver_MinTz_MatchesMfs() => AssertSolverMatches(new RcsVector(), new RcsVector(0, 0, -1));

        private void AssertSolverMatches(RcsVector desiredForce, RcsVector desiredTorque)
        {
            LogMatrixIfNeeded();

            var engine = new RcsEngine(ThrusterTestData.CreateThrusters());
            var optimiser = new RcsEngineOptimiser();
            var command = new RcsCommand(desiredForce, desiredTorque);
            var optimiserResult = optimiser.Optimise(engine, command);

            double[] desired = BuildDesiredVector(optimiserResult);

            var linearResult = LinearSystemSolver.Solve(CoefficientMatrix, desired);
            var optimiserThrusts = Thrusters.Select(t => optimiserResult.ThrusterOutputs[t.Key]).ToArray();

            var linearResultants = LogResults("Linear Solver", linearResult);
            var optimiserResultants = LogResults("MSF Optimiser", optimiserThrusts);

            for (int i = 0; i < linearResult.Length; i++)
                Assert.AreEqual(optimiserThrusts[i], linearResult[i], Tolerance, $"Mismatch at thruster {Thrusters[i].Key}");

            Assert.AreEqual(optimiserResultants.force.X, linearResultants.force.X, Tolerance, "Fx mismatch");
            Assert.AreEqual(optimiserResultants.force.Y, linearResultants.force.Y, Tolerance, "Fy mismatch");
            Assert.AreEqual(optimiserResultants.force.Z, linearResultants.force.Z, Tolerance, "Fz mismatch");
            Assert.AreEqual(optimiserResultants.torque.X, linearResultants.torque.X, Tolerance, "Tx mismatch");
            Assert.AreEqual(optimiserResultants.torque.Y, linearResultants.torque.Y, Tolerance, "Ty mismatch");
            Assert.AreEqual(optimiserResultants.torque.Z, linearResultants.torque.Z, Tolerance, "Tz mismatch");
        }

        private static double[] BuildDesiredVector(RcsEngineResult result) =>
            new[]
            {
                result.ResultantForce.X,
                result.ResultantForce.Y,
                result.ResultantForce.Z,
                result.ResultantTorque.X,
                result.ResultantTorque.Y,
                result.ResultantTorque.Z
            };

        private static MatrixBuildResult BuildCoefficientMatrix(IReadOnlyList<KeyValuePair<string, RcsThruster>> thrusters)
        {
            double[,] matrix = new double[6, thrusters.Count];
            var log = new List<string>();
            var header = new StringBuilder("       ");
            foreach (var thruster in thrusters)
                header.AppendFormat("{0,10}", thruster.Key);
            log.Add(header.ToString());

            for (int col = 0; col < thrusters.Count; col++)
            {
                var thruster = thrusters[col].Value;
                var dir = thruster.Direction;
                var pos = thruster.Position;

                matrix[0, col] = dir.X;
                matrix[1, col] = dir.Y;
                matrix[2, col] = dir.Z;
                matrix[3, col] = pos.Y * dir.Z - pos.Z * dir.Y;
                matrix[4, col] = pos.Z * dir.X - pos.X * dir.Z;
                matrix[5, col] = pos.X * dir.Y - pos.Y * dir.X;
            }

            string[] rowNames = { "Fx", "Fy", "Fz", "Tx", "Ty", "Tz" };
            for (int row = 0; row < 6; row++)
            {
                var line = new StringBuilder();
                line.AppendFormat("{0,-4}", rowNames[row]);
                for (int col = 0; col < thrusters.Count; col++)
                    line.AppendFormat("{0,10:F4}", matrix[row, col]);
                log.Add(line.ToString());
            }

            return new MatrixBuildResult { Matrix = matrix, Log = log };
        }

        private (RcsVector force, RcsVector torque) LogResults(string title, double[] outputs)
        {
            TestContext.WriteLine($"=== {title} Thruster Outputs ===");
            for (int i = 0; i < outputs.Length; i++)
            {
                string name = Thrusters[i].Key;
                TestContext.WriteLine($"{name}: {outputs[i]:F6}");
            }

            var resultant = CalculateResultants(outputs);

            TestContext.WriteLine("Resultant Force: " +
                $"Fx={resultant.force.X:F6}, Fy={resultant.force.Y:F6}, Fz={resultant.force.Z:F6}");
            TestContext.WriteLine("Resultant Torque: " +
                $"Tx={resultant.torque.X:F6}, Ty={resultant.torque.Y:F6}, Tz={resultant.torque.Z:F6}");

            return resultant;
        }

        private static (RcsVector force, RcsVector torque) CalculateResultants(double[] outputs)
        {
            double fx = 0, fy = 0, fz = 0;
            double tx = 0, ty = 0, tz = 0;

            for (int i = 0; i < outputs.Length; i++)
            {
                var thruster = Thrusters[i].Value;
                double thrust = outputs[i];
                var dir = thruster.Direction;
                var pos = thruster.Position;

                fx += thrust * dir.X;
                fy += thrust * dir.Y;
                fz += thrust * dir.Z;

                tx += pos.Y * thrust * dir.Z - pos.Z * thrust * dir.Y;
                ty += pos.Z * thrust * dir.X - pos.X * thrust * dir.Z;
                tz += pos.X * thrust * dir.Y - pos.Y * thrust * dir.X;
            }

            return (new RcsVector(fx, fy, fz), new RcsVector(tx, ty, tz));
        }

        private void LogMatrixIfNeeded()
        {
            if (matrixLogged)
                return;

            lock (MatrixLogLines)
            {
                if (matrixLogged)
                    return;

                TestContext.WriteLine("=== Thruster Coefficient Matrix ===");
                foreach (var line in MatrixLogLines)
                    TestContext.WriteLine(line);

                matrixLogged = true;
            }
        }

        private class MatrixBuildResult
        {
            public double[,] Matrix { get; set; } = null!;
            public List<string> Log { get; set; } = null!;
        }
    }
}
