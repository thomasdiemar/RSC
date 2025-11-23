namespace LinearSolver
{
    public interface IMyLinearSolver
    {
        double[] Solve(double[,] coefficients, double[] constants);
    }
}
