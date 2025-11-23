using LinearSolver.MSF;

namespace RCS.MSF
{
    public class MfsRcsEngineOptimiser : ARcsEngineOptimiser<MsfLinearSolver>
    {
        public MfsRcsEngineOptimiser() : base(new MsfLinearSolver())
        {
        }
    }
}
