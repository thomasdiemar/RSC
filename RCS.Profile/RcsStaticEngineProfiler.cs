using LinearSolver;
using RCS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace RCS.Profile
{
    public class RcsStaticEngineProfiler<TSolver> where TSolver : IMyLinearSolver, new()
    {
        private IRcsEngineOptimiser Optimiser = new RcsEngineOptimiser<TSolver>();
       

        public IEnumerable<MyProgress<RcsStaticEngineProfile>> Profile(RcsEngine rcsEngine)
        {
            var profile = new RcsStaticEngineProfile();

            var commands = new RcsCommand[]
            {
                new RcsCommand(new RcsVector<Fraction>(1, 0, 0), RcsProfileCommand.NOCOMMAND),
                new RcsCommand(new RcsVector<Fraction>(1, 0, 0), RcsProfileCommand.NOCOMMAND),
                new RcsCommand(new RcsVector<Fraction>(-1, 0, 0), RcsProfileCommand.NOCOMMAND),
                new RcsCommand(new RcsVector<Fraction>(0, 1, 0), RcsProfileCommand.NOCOMMAND),
                new RcsCommand(new RcsVector<Fraction>(0, -1, 0), RcsProfileCommand.NOCOMMAND),
                new RcsCommand(new RcsVector<Fraction>(0, 0, 1), RcsProfileCommand.NOCOMMAND),
                new RcsCommand(new RcsVector<Fraction>(0, 0, -1), RcsProfileCommand.NOCOMMAND),
                new RcsCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(1, 0, 0)),
                new RcsCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(-1, 0, 0)),
                new RcsCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(0, 1, 0)),
                new RcsCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(0, -1, 0)),
                new RcsCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(0, 0, 1)),
                new RcsCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(0, 0, -1))
            };

            foreach (var command in commands)
            {
                foreach (var progress in ProfileCommand(rcsEngine, command, profile))
                {
                    yield return progress;
                }
            }

            yield return new MyProgress<RcsStaticEngineProfile>
            {
                Info = "Profiling done",
                Result = profile,
                Done = true
            };
        }

        private IEnumerable<MyProgress<RcsStaticEngineProfile>> ProfileCommand(RcsEngine rcsEngine, RcsCommand command, RcsStaticEngineProfile profile)
        {
            var success = false;
            foreach (var result in ProfileCommandOptimise(rcsEngine, command, profile))
            {
                success = result.Done && result.Result.GetProfile(command).Any(x => x.Value != 0);
                yield return result;
            }

            if (!success)
            {
                command.AllowNonCommandedForces = !command.DesiredForce.Equals(RcsProfileCommand.NOCOMMAND);
                command.AllowNonCommandedTorques = !command.DesiredTorque.Equals(RcsProfileCommand.NOCOMMAND);
                foreach (var result in ProfileCommandOptimise(rcsEngine, command, profile))
                {
                    yield return result;
                }
            }
        }

        private IEnumerable<MyProgress<RcsStaticEngineProfile>> ProfileCommandOptimise(RcsEngine rcsEngine, RcsCommand rcsCommand, RcsStaticEngineProfile profile)
        {
            foreach (var result in Optimiser.Optimise(rcsEngine, rcsCommand))
            {
                if (result.Done)
                {
                    profile.AddProfile(rcsCommand, result.Result.ThrusterOutputs);
                }

                yield return new MyProgress<RcsStaticEngineProfile>
                {
                    Info = result.Info,
                    Result = profile,
                    Done = result.Done
                };
            }
        }
    }
}
