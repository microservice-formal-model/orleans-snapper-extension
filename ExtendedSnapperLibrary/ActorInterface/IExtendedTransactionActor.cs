using Common.Snapper.Core;
using Orleans.Concurrency;

namespace ExtendedSnapperLibrary.ActorInterface;

public interface IExtendedTransactionActor : IGrainWithIntegerKey
{
    /// <summary> Client submit a transaction request to this API. </summary>
    Task<TransactionResult> StartTransaction(FunctionCall firstFunc, int globalWorkerId, Dictionary<ActorID, int> actorAccessInfo);

    /// <summary> An actor invoke transactional calls on another actor via this API. </summary>
    Task<object> Execute(TransactionContext context, FunctionCall call);

    Task CheckGarbageCollection();

    [OneWay]
    public Task ReceiveBatch(Batch batch);

    [OneWay]
    public Task BatchCommit(long bid);
}