using LinearSolver.MSF;

namespace RCS.MSF
{
    public class MfsRcsEngineOptimiser : ARcsEngineOptimiser<MsfGoalLinearSolver>
    {
        //public MfsRcsEngineOptimiser() : base(new MsfLinearSolver())
        //{
        //}
        public MfsRcsEngineOptimiser() : base(new MsfGoalLinearSolver())
        {
        }
        
    }
}
