using Common.Snapper.Core;
using ExtendedSnapperLibrary.ActorInterface;

namespace ExtendedSnapperLibrary
{
    [GenerateSerializer]
    public class Token
    {
        /// <summary> the coordinator who emitted the last batch </summary>
        [Id(0)]
        internal IExtendedCoordinator lastCoord;

        /// <summary> the bid of last emit batch </summary>
        [Id(1)]
        internal long lastEmitBid;

        /// <summary> actor ID, last emitted bid </summary>
        [Id(2)]
        internal Dictionary<ActorID, long> lastBidPerActor;

        [Id(3)]
        internal long maxCommittedBid;

        public Token()
        {
            lastEmitBid = -1;
            maxCommittedBid = -1;
            lastBidPerActor = new Dictionary<ActorID, long>();
        }
    }
}