using System.Collections.Generic;

namespace RCS
{
    public class RcsEngine
    {
        public Dictionary<string, RcsThruster> Thrusters { get; }

        public RcsVector<int> CenterOfMass { get; set; } = new RcsVector<int>(0, 0, 0);

        public RcsEngine(Dictionary<string, RcsThruster> thrusters)
        {
            Thrusters = thrusters;
        }
    }
}
