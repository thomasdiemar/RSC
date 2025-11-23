namespace RCS
{
    public interface IRcsEngineOptimiser
    {
        RcsEngineResult Optimise(RcsEngine engine, RcsCommand command);
    }
}