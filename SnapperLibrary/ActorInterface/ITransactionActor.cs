using Common.Snapper.Core;
using Orleans.Concurrency;

namespace SnapperLibrary.ActorInterface;

public interface ITransactionActor : IGrainWithIntegerKey
{
    /* Client submit a transaction request to this API. */
    Task<TransactionResult> StartTransaction(FunctionCall firstFunc, Dictionary<ActorID, int> actorAccessInfo);

    /* An actor invoke transactional calls on another actor via this API. */
    Task<object> Execute(TransactionContext context, FunctionCall call);

    Task CheckGarbageCollection();

    [OneWay]
    internal Task ReceiveBatch(Batch batch);

    [OneWay]
    internal Task BatchCommit(long bid);
}