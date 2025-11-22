namespace RCS
{
    public class RcsThruster
    {
        public RcsVector Direction { get; set; }
        public RcsVector Position { get; set; }

        public RcsThruster(RcsVector direction, RcsVector position)
        {
            Direction = direction;
            Position = position;
        }
    }
}
