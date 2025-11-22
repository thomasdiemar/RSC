using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace RCS
{
    public class RcsEngineOptimiser
    {
        public RcsEngineResult MaximizeForceX(RcsEngine engine)
        {
            var context = SolverContext.GetContext();
            context.ClearModel();
            var model = context.CreateModel();

            var decisions = new Dictionary<string, Decision>();
            foreach (var kvp in engine.Thrusters)
            {
                var decision = new Decision(Domain.RealRange(0.0, 1.0), kvp.Key);
                model.AddDecision(decision);
                decisions[kvp.Key] = decision;
            }

            Term Fx = 0;
            foreach (var kvp in engine.Thrusters)
            {
                Fx += decisions[kvp.Key] * kvp.Value.Direction.X;
            }

            model.AddGoal("MaximizeFx", GoalKind.Maximize, Fx);
            context.Solve();

            var outputs = new Dictionary<string, double>();
            foreach (var decision in decisions)
            {
                outputs[decision.Key] = decision.Value.ToDouble();
            }

            var resultantForce = CalculateResultantForce(engine.Thrusters, outputs);
            var resultantTorque = CalculateResultantTorque(engine.Thrusters, outputs);

            return new RcsEngineResult(outputs, resultantForce, resultantTorque);
        }

        public RcsEngineResult MinimizeForceX(RcsEngine engine)
        {
            var context = SolverContext.GetContext();
            context.ClearModel();
            var model = context.CreateModel();

            var decisions = new Dictionary<string, Decision>();
            foreach (var kvp in engine.Thrusters)
            {
                var decision = new Decision(Domain.RealRange(0.0, 1.0), kvp.Key);
                model.AddDecision(decision);
                decisions[kvp.Key] = decision;
            }

            Term Fx = 0;
            foreach (var kvp in engine.Thrusters)
            {
                Fx += decisions[kvp.Key] * kvp.Value.Direction.X;
            }

            model.AddGoal("MinimizeFx", GoalKind.Minimize, Fx);
            context.Solve();

            var outputs = new Dictionary<string, double>();
            foreach (var decision in decisions)
            {
                outputs[decision.Key] = decision.Value.ToDouble();
            }

            var resultantForce = CalculateResultantForce(engine.Thrusters, outputs);
            var resultantTorque = CalculateResultantTorque(engine.Thrusters, outputs);

            return new RcsEngineResult(outputs, resultantForce, resultantTorque);
        }

        public RcsEngineResult MaximizeTorqueX(RcsEngine engine)
        {
            var context = SolverContext.GetContext();
            context.ClearModel();
            var model = context.CreateModel();

            var decisions = new Dictionary<string, Decision>();
            foreach (var kvp in engine.Thrusters)
            {
                var decision = new Decision(Domain.RealRange(0.0, 1.0), kvp.Key);
                model.AddDecision(decision);
                decisions[kvp.Key] = decision;
            }

            Term Tx = 0;
            foreach (var kvp in engine.Thrusters)
            {
                var thruster = kvp.Value;
                var pos = thruster.Position;
                var dir = thruster.Direction;
                Tx += pos.Y * decisions[kvp.Key] * dir.Z - pos.Z * decisions[kvp.Key] * dir.Y;
            }

            model.AddGoal("MaximizeTx", GoalKind.Maximize, Tx);
            context.Solve();

            var outputs = new Dictionary<string, double>();
            foreach (var decision in decisions)
            {
                outputs[decision.Key] = decision.Value.ToDouble();
            }

            var resultantForce = CalculateResultantForce(engine.Thrusters, outputs);
            var resultantTorque = CalculateResultantTorque(engine.Thrusters, outputs);

            return new RcsEngineResult(outputs, resultantForce, resultantTorque);
        }

        public RcsEngineResult MinimizeTorqueX(RcsEngine engine)
        {
            var context = SolverContext.GetContext();
            context.ClearModel();
            var model = context.CreateModel();

            var decisions = new Dictionary<string, Decision>();
            foreach (var kvp in engine.Thrusters)
            {
                var decision = new Decision(Domain.RealRange(0.0, 1.0), kvp.Key);
                model.AddDecision(decision);
                decisions[kvp.Key] = decision;
            }

            Term Tx = 0;
            foreach (var kvp in engine.Thrusters)
            {
                var thruster = kvp.Value;
                var pos = thruster.Position;
                var dir = thruster.Direction;
                Tx += pos.Y * decisions[kvp.Key] * dir.Z - pos.Z * decisions[kvp.Key] * dir.Y;
            }

            model.AddGoal("MinimizeTx", GoalKind.Minimize, Tx);
            context.Solve();

            var outputs = new Dictionary<string, double>();
            foreach (var decision in decisions)
            {
                outputs[decision.Key] = decision.Value.ToDouble();
            }

            var resultantForce = CalculateResultantForce(engine.Thrusters, outputs);
            var resultantTorque = CalculateResultantTorque(engine.Thrusters, outputs);

            return new RcsEngineResult(outputs, resultantForce, resultantTorque);
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
