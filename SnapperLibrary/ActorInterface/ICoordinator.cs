using Common.Snapper.Core;
using Orleans.Concurrency;

namespace SnapperLibrary.ActorInterface
{
    public interface ICoordinator : IGrainWithIntegerKey
    {
        public Task Init();

        public Task CheckGarbageCollection();

        [OneWay]
        internal Task PassToken(Token token);

        internal Task<TransactionContext> NewTransaction(Dictionary<ActorID, int> actorAccessInfo);

        [OneWay]
        internal Task BatchComplete(long bid);

        internal Task WaitBatchCommit(long bid);
    }
}