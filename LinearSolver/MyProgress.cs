namespace LinearSolver
{
    public class MyProgress<TResult>
    {
        public string Info { get; set; }
        public TResult Result { get; set; }
        public bool Done { get; set; }
    }
}
