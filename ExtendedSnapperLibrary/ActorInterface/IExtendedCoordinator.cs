using Orleans.Concurrency;

namespace ExtendedSnapperLibrary.ActorInterface;

public interface IExtendedCoordinator : IGrainWithIntegerKey
{
    public Task Init();

    public Task CheckGarbageCollection();

    // <external tid, <actor ID, number of accesses>>
    public Task RegisterSchedule(ExternalBatch batch);

    [OneWay]
    internal Task PassToken(Token token);

    [OneWay]
    internal Task BatchComplete(long bid);

    internal Task WaitBatchCommit(long bid);
}