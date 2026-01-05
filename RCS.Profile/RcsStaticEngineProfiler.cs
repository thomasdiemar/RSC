using LinearSolver;
using RCS;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RCS.Profile
{
    public class RcsStaticEngineProfiler<TSolver> where TSolver : IMyLinearSolver, new()
    {
        private IRcsEngineOptimiser Optimiser = new RcsEngineOptimiser<TSolver>();
        private RcsVector<Fraction> NOCOMMAND = new RcsVector<Fraction>(0, 0, 0);

        public IEnumerable<MyProgress<RcsEngineProfile>> Profile(RcsEngine rcsEngine)
        {
            var profile = new RcsEngineProfile();

            var commands = new RcsCommand[]
            {
                new RcsCommand(new RcsVector<Fraction>(1, 0, 0), NOCOMMAND),
                new RcsCommand(new RcsVector<Fraction>(-1, 0, 0), NOCOMMAND),
                new RcsCommand(new RcsVector<Fraction>(0, 1, 0), NOCOMMAND),
                new RcsCommand(new RcsVector<Fraction>(0, -1, 0), NOCOMMAND),
                new RcsCommand(new RcsVector<Fraction>(0, 0, 1), NOCOMMAND),
                new RcsCommand(new RcsVector<Fraction>(0, 0, -1), NOCOMMAND),
                new RcsCommand(NOCOMMAND, new RcsVector<Fraction>(1, 0, 0)),
                new RcsCommand(NOCOMMAND, new RcsVector<Fraction>(-1, 0, 0)),
                new RcsCommand(NOCOMMAND, new RcsVector<Fraction>(0, 1, 0)),
                new RcsCommand(NOCOMMAND, new RcsVector<Fraction>(0, -1, 0)),
                new RcsCommand(NOCOMMAND, new RcsVector<Fraction>(0, 0, 1)),
                new RcsCommand(NOCOMMAND, new RcsVector<Fraction>(0, 0, -1))
            };

            foreach (var command in commands)
            {
                foreach (var progress in ProfileCommand(rcsEngine, command, profile))
                {
                    yield return progress;
                }
            }

            yield return new MyProgress<RcsEngineProfile>
            {
                Info = "Profiling done",
                Result = profile,
                Done = true
            };
        }

        private IEnumerable<MyProgress<RcsEngineProfile>> ProfileCommand(RcsEngine rcsEngine, RcsCommand command, RcsEngineProfile profile)
        {
            var success = false;
            foreach (var result in ProfileCommandOptimise(rcsEngine, command, profile))
            {
                success = result.Done && result.Result.GetProfileCommand(command).Any(x => x.Value != 0);
                yield return result;
            }

            if (!success)
            {
                command.AllowNonCommandedForces = !command.DesiredForce.Equals(NOCOMMAND);
                command.AllowNonCommandedTorques = !command.DesiredTorque.Equals(NOCOMMAND);
                foreach (var result in ProfileCommandOptimise(rcsEngine, command, profile))
                {
                    yield return result;
                }
            }
        }

        private IEnumerable<MyProgress<RcsEngineProfile>> ProfileCommandOptimise(RcsEngine rcsEngine, RcsCommand rcsCommand, RcsEngineProfile profile)
        {
            foreach (var result in Optimiser.Optimise(rcsEngine, rcsCommand))
            {
                if (result.Done)
                {
                    profile.AddProfileCommand(rcsCommand, result.Result.ThrusterOutputs);
                }

                yield return new MyProgress<RcsEngineProfile>
                {
                    Info = result.Info,
                    Result = profile,
                    Done = result.Done
                };
            }
        }
    }
}
