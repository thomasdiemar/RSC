using System;
using System.Collections.Generic;
using System.Linq;
using LinearSolver;

namespace RCS
{
    public class RcsEngineOptimiser<TSolver> : IRcsEngineOptimiser where TSolver : IMyLinearSolver, new()
    {
        private readonly TSolver solver;

        /// <summary>
        /// Create an optimiser with a default solver instance.
        /// </summary>
        public RcsEngineOptimiser() : this(new TSolver())
        {
        }

        /// <summary>
        /// Create an optimiser with an explicitly provided solver instance.
        /// </summary>
        public RcsEngineOptimiser(TSolver solver)
        {
            this.solver = solver;
        }

        /// <summary>
        /// Stream progress snapshots mapped from solver outputs to resultant force/torque.
        /// </summary>
        public IEnumerable<MyProgress<RcsEngineResult>> Optimise(RcsEngine engine, RcsEngineOptimiserCommand command)
        {
            var orderedThrusters = engine.Thrusters.OrderBy(t => t.Key).ToList();
            var matrix = BuildCoefficientMatrix(orderedThrusters);
            var desired = BuildDesiredVector(engine, command);

            foreach (var progress in solver.Solve(matrix, desired))
            {
                var outputs = MapOutputs(orderedThrusters, progress.Result);
                var resultantForce = CalculateResultantForce(engine.Thrusters, outputs);
                var resultantTorque = CalculateResultantTorque(engine.Thrusters, outputs);
                yield return CreateProgress(new RcsEngineResult(outputs, resultantForce, resultantTorque));
            }
        }

        /// <summary>
        /// Construct desired per-axis targets (max/min or soft zero) based on command and thruster limits.
        /// </summary>
        public Fraction[] BuildDesiredVector(RcsEngine engine, RcsEngineOptimiserCommand command)
        {
            Fraction maxFx = 0, minFx = 0, maxFy = 0, minFy = 0, maxFz = 0, minFz = 0;
            Fraction maxTx = 0, minTx = 0, maxTy = 0, minTy = 0, maxTz = 0, minTz = 0;

            foreach (var thruster in engine.Thrusters.Values)
            {
                var dir = thruster.Direction;
                var pos = thruster.Position;

                if (dir.X > 0) maxFx += dir.X; else minFx += dir.X;
                if (dir.Y > 0) maxFy += dir.Y; else minFy += dir.Y;
                if (dir.Z > 0) maxFz += dir.Z; else minFz += dir.Z;

                var txCoeff = pos.Y * dir.Z - pos.Z * dir.Y;
                var tyCoeff = pos.Z * dir.X - pos.X * dir.Z;
                var tzCoeff = pos.X * dir.Y - pos.Y * dir.X;

                if (txCoeff > 0) maxTx += txCoeff; else minTx += txCoeff;
                if (tyCoeff > 0) maxTy += tyCoeff; else minTy += tyCoeff;
                if (tzCoeff > 0) maxTz += tzCoeff; else minTz += tzCoeff;
            }

            return new[]
            {
                SelectDesired(maxFx, minFx, command.DesiredForce.X, command.AllowNonCommandedForces),
                SelectDesired(maxFy, minFy, command.DesiredForce.Y, command.AllowNonCommandedForces),
                SelectDesired(maxFz, minFz, command.DesiredForce.Z, command.AllowNonCommandedForces),
                SelectDesired(maxTx, minTx, command.DesiredTorque.X, command.AllowNonCommandedTorques),
                SelectDesired(maxTy, minTy, command.DesiredTorque.Y, command.AllowNonCommandedTorques),
                SelectDesired(maxTz, minTz, command.DesiredTorque.Z, command.AllowNonCommandedTorques)
            };
        }

        /// <summary>
        /// Build the 6xN coefficient matrix (force/torque rows by thruster).
        /// </summary>
        private static Fraction[,] BuildCoefficientMatrix(IReadOnlyList<KeyValuePair<string, RcsThruster>> thrusters)
        {
            var matrix = new Fraction[6, thrusters.Count];

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

        /// <summary>
        /// Select target per axis based on requested sign and soft-zero allowance.
        /// </summary>
        private static Fraction SelectDesired(Fraction max, Fraction min, Fraction requested, bool allowSoftZero)
        {
            if (requested > 0)
                return max;
            if (requested < 0)
                return min;
            return allowSoftZero ? Fraction.NaN : Fraction.Zero;
        }

        /// <summary>
        /// Map solver output vector back to thruster name/value pairs.
        /// </summary>
        private static Dictionary<string, Fraction> MapOutputs(
            IReadOnlyList<KeyValuePair<string, RcsThruster>> orderedThrusters,
            Fraction[] outputsArray)
        {
            var outputs = new Dictionary<string, Fraction>();
            for (int i = 0; i < orderedThrusters.Count; i++)
                outputs[orderedThrusters[i].Key] = outputsArray[i];
            return outputs;
        }

        /// <summary>
        /// Compute resultant force from thruster outputs.
        /// </summary>
        private static RcsVector<Fraction> CalculateResultantForce(
            IReadOnlyDictionary<string, RcsThruster> thrusters,
            IReadOnlyDictionary<string, Fraction> outputs)
        {
            Fraction fx = 0, fy = 0, fz = 0;

            foreach (var kvp in thrusters)
            {
                var thrust = outputs[kvp.Key];
                var dir = kvp.Value.Direction;

                fx += thrust * dir.X;
                fy += thrust * dir.Y;
                fz += thrust * dir.Z;
            }

            return new RcsVector<Fraction>(fx, fy, fz);
        }

        /// <summary>
        /// Compute resultant torque from thruster outputs.
        /// </summary>
        private static RcsVector<Fraction> CalculateResultantTorque(
            IReadOnlyDictionary<string, RcsThruster> thrusters,
            IReadOnlyDictionary<string, Fraction> outputs)
        {
            Fraction tx = 0, ty = 0, tz = 0;

            foreach (var kvp in thrusters)
            {
                var thrust = outputs[kvp.Key];
                var thruster = kvp.Value;
                var pos = thruster.Position;
                var dir = thruster.Direction;

                tx += pos.Y * thrust * dir.Z - pos.Z * thrust * dir.Y;
                ty += pos.Z * thrust * dir.X - pos.X * thrust * dir.Z;
                tz += pos.X * thrust * dir.Y - pos.Y * thrust * dir.X;
            }

            return new RcsVector<Fraction>(tx, ty, tz);
        }

        /// <summary>
        /// Wrap a snapshot result into progress metadata.
        /// </summary>
        private static MyProgress<RcsEngineResult> CreateProgress(RcsEngineResult result)
        {
            return new MyProgress<RcsEngineResult>
            {
                Result = result,
                Done = true
            };
        }
    }
}
