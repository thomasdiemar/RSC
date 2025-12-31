using LinearSolver;
using System.Collections.Generic;

namespace RCS
{
    public interface IRcsEngineOptimiser
    {
        IEnumerable<MyProgress<RcsEngineResult>> Optimise(RcsEngine engine, RcsCommand command);
    }
}
