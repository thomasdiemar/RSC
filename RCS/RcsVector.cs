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

        public RcsVector(float x, float y, float z)
        {
            X = (T)System.Convert.ChangeType(x, typeof(T));
            Y = (T)System.Convert.ChangeType(y, typeof(T));
            Z = (T)System.Convert.ChangeType(z, typeof(T));
        }

        public RcsVector(double x, double y, double z) 
        { 
            X = (T)System.Convert.ChangeType(x, typeof(T));
            Y = (T)System.Convert.ChangeType(y, typeof(T));
            Z = (T)System.Convert.ChangeType(z, typeof(T));
        }
       
    }
}
