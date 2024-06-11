using Common.Snapper.Core;

namespace ExtendedSnapperLibrary
{
    public class SnapperTransaction : IEquatable<SnapperTransaction>
    {
        public FunctionCall FunctionCall { get; set; }

        public Dictionary<ActorID, int> Accesses { get; set; }

        public long Etid { get; set; }

        public SnapperTransaction(FunctionCall func, Dictionary<ActorID, int> accesses, long etid)
        {
            FunctionCall = func;
            Accesses = accesses;
            Etid = etid;
        }

        public bool Equals(SnapperTransaction? other)
        {
            if (other != null)
            {
                return FunctionCall.Equals(other.FunctionCall) &&
                    Accesses.Count == other.Accesses.Count &&
                    !Accesses.Except(other.Accesses).Any() &&
                    Etid == other.Etid;

            }
            return false;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (obj is not SnapperTransaction st) return false;
            return st.Equals(this);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            foreach (var kvp in Accesses)
            {
                hash.Add(kvp.Key);
                hash.Add(kvp.Value);
            }
            hash.Add(FunctionCall);
            hash.Add(Etid);
            return hash.ToHashCode();
        }
    }
}
