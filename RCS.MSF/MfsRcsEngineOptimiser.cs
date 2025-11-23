using Microsoft.SolverFoundation.Services;
using System;
using System.Collections.Generic;
using System.Text;


namespace RCS.MSF
{
    public class MfsRcsEngineOptimiser : IRcsEngineOptimiser
    {
       

        public RcsEngineResult Optimise(RcsEngine engine, RcsCommand command)
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
            Term Fy = 0;
            Term Fz = 0;
            Term Tx = 0;
            Term Ty = 0;
            Term Tz = 0;
            foreach (var kvp in engine.Thrusters)
            {
                var thruster = kvp.Value;
                var dir = thruster.Direction;
                var pos = thruster.Position;

                Fx += decisions[kvp.Key] * dir.X;
                Fy += decisions[kvp.Key] * dir.Y;
                Fz += decisions[kvp.Key] * dir.Z;

                Tx += pos.Y * decisions[kvp.Key] * dir.Z - pos.Z * decisions[kvp.Key] * dir.Y;
                Ty += pos.Z * decisions[kvp.Key] * dir.X - pos.X * decisions[kvp.Key] * dir.Z;
                Tz += pos.X * decisions[kvp.Key] * dir.Y - pos.Y * decisions[kvp.Key] * dir.X;
            }

            bool goalAdded = false;
            goalAdded |= TryAddGoal(model, Fx, command.DesiredForce.X, "Fx");
            goalAdded |= TryAddGoal(model, Fy, command.DesiredForce.Y, "Fy");
            goalAdded |= TryAddGoal(model, Fz, command.DesiredForce.Z, "Fz");
            goalAdded |= TryAddGoal(model, Tx, command.DesiredTorque.X, "Tx");
            goalAdded |= TryAddGoal(model, Ty, command.DesiredTorque.Y, "Ty");
            goalAdded |= TryAddGoal(model, Tz, command.DesiredTorque.Z, "Tz");

            if (!goalAdded)
                throw new InvalidOperationException("No force or torque goal specified in command.");

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

        private static bool TryAddGoal(Model model, Term term, double desired, string name)
        {
            if (desired > 0)
            {
                model.AddGoal($"Maximize{name}", GoalKind.Maximize, term);
                return true;
            }

            if (desired < 0)
            {
                model.AddGoal($"Minimize{name}", GoalKind.Minimize, term);
                return true;
            }

            return false;
        }
    }
}
