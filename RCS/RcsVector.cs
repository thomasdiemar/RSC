namespace RCS
{
    public struct RcsVector
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public RcsVector(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
