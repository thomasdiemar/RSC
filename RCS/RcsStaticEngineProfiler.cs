using System;
using System.Collections.Generic;
using System.Linq;

public class RcsStaticEngineProfiler
{
    IRcsEngineOptimiser Optimiser = new RcsEngineOptimiser();

    public IEnumerable<MyProgress<RcsEngineProfile>> Profile(RcsEngine rcsEngine)
    {
        var profile = new RcsEngineProfile();
        var nocommand = new RcsVector<Fraction>(0, 0, 0);
        var command1 = new RcsVector<Fraction>(1, 0, 0);
        var command2 = new RcsVector<Fraction>(0, 1, 0);
        var command3 = new RcsVector<Fraction>(0, 0, 1);
        
        var success = false;
        foreach (var result in ProfileCommand(rcsEngine, new RcsCommand(command1, nocommand), profile))
        {
            success = result.Done && result.Result.ThrusterOutputs.Any(x => x.Value.Any(y => y.Value != 0));
            yield return result;
        }
        
        if (!success)
        {
            foreach (var result in ProfileCommand(rcsEngine, new RcsCommand(command1, nocommand, true, false), profile))
            {
                yield return result;
            }
        }
        
        yield return new MyProgress<RcsEngineProfile>
        {
            Info = "Profiling done",
            Result = profile,
            Done = true
        };
    }
    IEnumerable<MyProgress<RcsEngineProfile>> ProfileCommand(RcsEngine rcsEngine, RcsCommand rcsCommand, RcsEngineProfile profile)
    {
        foreach (var result in Optimiser.Optimise(rcsEngine, rcsCommand))
        {
            if (result.Done)
                profile.AddProfileCommand(rcsCommand, result.Result.ThrusterOutputs);

            yield return new MyProgress<RcsEngineProfile>
            {
                Info = result.Info,
                Result = profile,
                Done = result.Done
            };
        }
    }
}

