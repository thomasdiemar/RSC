using LinearSolver.Custom;

namespace RCS.Custom
{
    public class CustomRcsEngineOptimiser : ARcsEngineOptimiser<CustomLinearSolver>
    {
        public CustomRcsEngineOptimiser() : base(new CustomLinearSolver())
        {
        }

        public CustomRcsEngineOptimiser(CustomLinearSolver solver) : base(solver)
        {
        }
    }
}
