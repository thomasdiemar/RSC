namespace RCS
{
    public struct RcsVector<T>
    {
        public T X { get; set; }
        public T Y { get; set; }
        public T Z { get; set; }

        public RcsVector(T x, T y, T z)
        {
            X = x;
            Y = y;
            Z = z;
        }
       
    }
}
