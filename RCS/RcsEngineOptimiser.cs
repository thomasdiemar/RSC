using System;
using System.Collections.Generic;
using System.Linq;
using LinearSolver.Custom;

namespace RCS
{
    public class RcsEngineOptimiser : IRcsEngineOptimiser
    {
        public RcsEngineResult Optimise(RcsEngine engine, RcsCommand command)
        {
            var thrusters = engine.Thrusters.OrderBy(t => t.Key).ToList();
            double[,] matrix = BuildCoefficientMatrix(thrusters);
            double[] desired = BuildDesiredVector(engine, command);

            var solver = new CustomLinearSolver();
            double[] outputsArray = solver.Solve(matrix, desired);

            var outputs = new Dictionary<string, double>();
            for (int i = 0; i < thrusters.Count; i++)
                outputs[thrusters[i].Key] = outputsArray[i];

            var resultantForce = CalculateResultantForce(engine.Thrusters, outputs);
            var resultantTorque = CalculateResultantTorque(engine.Thrusters, outputs);

            return new RcsEngineResult(outputs, resultantForce, resultantTorque);
        }

        public double[] BuildDesiredVector(RcsEngine engine, RcsCommand command)
        {
            double maxFx = 0, minFx = 0, maxFy = 0, minFy = 0, maxFz = 0, minFz = 0;
            double maxTx = 0, minTx = 0, maxTy = 0, minTy = 0, maxTz = 0, minTz = 0;

            foreach (var thruster in engine.Thrusters.Values)
            {
                var dir = thruster.Direction;
                var pos = thruster.Position;

                if (dir.X > 0) maxFx += dir.X; else minFx += dir.X;
                if (dir.Y > 0) maxFy += dir.Y; else minFy += dir.Y;
                if (dir.Z > 0) maxFz += dir.Z; else minFz += dir.Z;

                double txCoeff = pos.Y * dir.Z - pos.Z * dir.Y;
                double tyCoeff = pos.Z * dir.X - pos.X * dir.Z;
                double tzCoeff = pos.X * dir.Y - pos.Y * dir.X;

                if (txCoeff > 0) maxTx += txCoeff; else minTx += txCoeff;
                if (tyCoeff > 0) maxTy += tyCoeff; else minTy += tyCoeff;
                if (tzCoeff > 0) maxTz += tzCoeff; else minTz += tzCoeff;
            }

            return new[]
            {
                SelectDesired(maxFx, minFx, command.DesiredForce.X),
                SelectDesired(maxFy, minFy, command.DesiredForce.Y),
                SelectDesired(maxFz, minFz, command.DesiredForce.Z),
                SelectDesired(maxTx, minTx, command.DesiredTorque.X),
                SelectDesired(maxTy, minTy, command.DesiredTorque.Y),
                SelectDesired(maxTz, minTz, command.DesiredTorque.Z)
            };
        }

        private static double[,] BuildCoefficientMatrix(IReadOnlyList<KeyValuePair<string, RcsThruster>> thrusters)
        {
            double[,] matrix = new double[6, thrusters.Count];

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

            return matrix;
        }

        private static double SelectDesired(double max, double min, double requested)
        {
            if (requested > 0)
                return max;
            if (requested < 0)
                return min;
            return 0;
        }

        private static RcsVector CalculateResultantForce(
            IReadOnlyDictionary<string, RcsThruster> thrusters,
            IReadOnlyDictionary<string, double> outputs)
        {
            double fx = 0, fy = 0, fz = 0;

            foreach (var kvp in thrusters)
            {
                double thrust = outputs[kvp.Key];
                var dir = kvp.Value.Direction;

                fx += thrust * dir.X;
                fy += thrust * dir.Y;
                fz += thrust * dir.Z;
            }

            return new RcsVector(fx, fy, fz);
        }

        private static RcsVector CalculateResultantTorque(
            IReadOnlyDictionary<string, RcsThruster> thrusters,
            IReadOnlyDictionary<string, double> outputs)
        {
            double tx = 0, ty = 0, tz = 0;

            foreach (var kvp in thrusters)
            {
                double thrust = outputs[kvp.Key];
                var thruster = kvp.Value;
                var pos = thruster.Position;
                var dir = thruster.Direction;

                tx += pos.Y * thrust * dir.Z - pos.Z * thrust * dir.Y;
                ty += pos.Z * thrust * dir.X - pos.X * thrust * dir.Z;
                tz += pos.X * thrust * dir.Y - pos.Y * thrust * dir.X;
            }

            return new RcsVector(tx, ty, tz);
        }
    }
}
