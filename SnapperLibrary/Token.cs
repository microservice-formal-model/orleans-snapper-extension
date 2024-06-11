using Common.Snapper.Core;

namespace SnapperLibrary
{
    [GenerateSerializer]
    public class Token
    {
        [Id(0)]
        internal long lastBid;   // the latest generated bid
        [Id(1)]
        internal long lastTid;   // the latest generated tid
        [Id(2)]
        internal long lastCoord; // the coordinator who emitted the last batch
        [Id(3)]
        internal Dictionary<ActorID, long> lastBidPerActor;
        [Id(4)]
        internal long maxCommittedBid;

        public Token()
        {
            lastBid = -1;
            lastTid = -1;
            lastCoord = -1;
            maxCommittedBid = -1;
            lastBidPerActor = new Dictionary<ActorID, long>();
        }
    }
}