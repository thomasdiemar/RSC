using LinearSolver;

namespace RCS
{
    public class RcsThruster
    {
        public RcsVector<int> Direction { get; set; }
        public RcsVector<int> Position { get; set; }

        public RcsThruster(RcsVector<int> direction, RcsVector<int> position)
        {
            Direction = direction;
            Position = position;
        }

    }

}
