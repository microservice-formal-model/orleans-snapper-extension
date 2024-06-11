using System.Diagnostics;
using Common.Snapper.Core;
using ExtendedSnapperLibrary.ActorInterface;
using Orleans.Concurrency;
using Utilities;

namespace ExtendedSnapperLibrary.Actor;

[Reentrant]
internal class ExtendedCoordinator : Grain, IExtendedCoordinator
{
    long myID;
    IExtendedCoordinator neighbor;

    long maxCommittedBid;
    SortedDictionary<long, ExternalBatch> externalBatches;                        // received external batches
    Dictionary<long, Dictionary<ActorID, Batch>> batchInfoPerActorPerBatch;       // bid, actor ID, batch info
    Dictionary<long, (long, IExtendedCoordinator)> bidToLastBid;                          // bid, last bid, last coord
    Dictionary<long, Counter> numActorPerBatch;                                   // bid, num accessed actors
    Dictionary<long, TaskCompletionSource> waitBatchCommit;

    private readonly Helper helper;

    public ExtendedCoordinator(Helper helper)
    {
        this.helper = helper;
        Console.WriteLine(helper.numCoordPerGroup);
    }

    public override Task OnActivateAsync(CancellationToken _)
    {
        maxCommittedBid = -1;
        myID = this.GetPrimaryKeyLong();
        externalBatches = new SortedDictionary<long, ExternalBatch>();
        batchInfoPerActorPerBatch = new Dictionary<long, Dictionary<ActorID, Batch>>();
        bidToLastBid = new Dictionary<long, (long, IExtendedCoordinator)>();
        numActorPerBatch = new Dictionary<long, Counter>();
        waitBatchCommit = new Dictionary<long, TaskCompletionSource>();

        var neighborID = helper.MapCoordIDToNeighborID(myID);
        neighbor = GrainFactory.GetGrain<IExtendedCoordinator>(neighborID);
        Console.WriteLine($"Coordinator {myID} is started, neighbor = {neighborID}");
        return Task.CompletedTask;
    }

    public Task Init()
    {
        _ = PassToken(new Token());
        return Task.CompletedTask;
    }

    public Task CheckGarbageCollection()
    {
        if (batchInfoPerActorPerBatch.Count != 0)
            Console.WriteLine($"Coordinator: batchInfoPerActorPerBatch has {batchInfoPerActorPerBatch.Count} entries");
        if (bidToLastBid.Count != 0)
            Console.WriteLine($"Coordinator: bidToLastBid has {bidToLastBid.Count} entries");
        if (numActorPerBatch.Count != 0)
            Console.WriteLine($"Coordinator: numActorPerBatch has {numActorPerBatch.Count} entries");
        if (waitBatchCommit.Count != 0)
            Console.WriteLine($"Coordinator: waitBatchCommit has {waitBatchCommit.Count} entries");
        return Task.CompletedTask;
    }

    public Task RegisterSchedule(ExternalBatch batch)
    {
        Debug.Assert(!externalBatches.ContainsKey(batch.bid));
        //Console.WriteLine("----------SNAPPER----------\nI received a batch: " + batch);
        externalBatches.Add(batch.bid, batch);
        return Task.CompletedTask;
    }

    public async Task PassToken(Token token)
    {
        var bids = GenerateBatches(token);
        //Console.WriteLine($"Generate {bids.Count} bids");

        // update token info
        if (token.maxCommittedBid < maxCommittedBid)
        {
            var expiredActors = new List<ActorID>();
            foreach (var item in token.lastBidPerActor)
                if (item.Value <= maxCommittedBid) expiredActors.Add(item.Key);
            foreach (var actor in expiredActors) token.lastBidPerActor.Remove(actor);
            token.maxCommittedBid = maxCommittedBid;
        }
        else maxCommittedBid = token.maxCommittedBid;

        // no need to wait for the batch being emitted
        _ = neighbor.PassToken(token);

        var tasks = new List<Task>();
        foreach (var bid in bids) tasks.Add(EmitBatch(bid));
        await Task.WhenAll(tasks);
    }

    List<long> GenerateBatches(Token token)
    {
        var bids = new List<long>();

        // STEP 1: check if there are any external batches to emit
        if (externalBatches.Count == 0) return bids;

        // STEP 2: generate the batch info for each accessed actor
        while (externalBatches.Count != 0)
        {
            var batch = externalBatches.First().Value;
            //Console.WriteLine($"try generate batch, bid = {batch.bid}, lastBid = {batch.lastBid}, token lastBid = {token.lastEmitBid}");
            if (token.lastEmitBid != batch.lastBid) break;

            bidToLastBid.Add(batch.bid, (batch.lastBid, token.lastCoord));
            token.lastEmitBid = batch.bid;
            token.lastCoord = this;

            var batchInfoPerActor = new Dictionary<ActorID, Batch>();
            foreach (var item in batch.transactions)   // tid, number of access on this actor
            {
                var scheduleID = item.Key;
                foreach (var txn in item.Value)
                {
                    var tid = txn.Item1;
                    foreach (var actor in txn.Item2)
                    {
                        if (!batchInfoPerActor.ContainsKey(actor.Key)) batchInfoPerActor.Add(actor.Key, new Batch(batch.bid, this));
                        batchInfoPerActor[actor.Key].AddTransaction(scheduleID, tid, actor.Value);

                        if (token.lastBidPerActor.ContainsKey(actor.Key)) batchInfoPerActor[actor.Key].lastBid = token.lastBidPerActor[actor.Key];
                    }
                }

            }

            foreach (var info in batchInfoPerActor) token.lastBidPerActor[info.Key] = info.Value.bid;
            batchInfoPerActorPerBatch.Add(batch.bid, batchInfoPerActor);
            bids.Add(batch.bid);

            externalBatches.Remove(batch.bid);
            //Console.WriteLine($"batch is generated, bid = {batch.bid}, lastBid = {batch.lastBid}, num Actor = {batchInfoPerActor.Count}");
        }
        return bids;
    }

    async Task EmitBatch(long bid)
    {
        // emit batch messages to related actors
        var actors = batchInfoPerActorPerBatch[bid];
        numActorPerBatch.Add(bid, new Counter(actors.Count));

        foreach (var item in actors)
        {
            var actorID = item.Key;
            var subBatch = item.Value;
            //Console.WriteLine($"try emit batch bid = {bid} to actor {actorID.className}-{actorID.id}");
            var actor = GrainFactory.GetGrain<IExtendedTransactionActor>(actorID.id, actorID.className);
            await actor.ReceiveBatch(subBatch);
            //Console.WriteLine($"emit batch bid = {bid} to actor {actorID.className}-{actorID.id}");
        }
    }

    public async Task BatchComplete(long bid)
    {
        if (numActorPerBatch[bid].Decrement() == false) return;

        // wait for previous batch to commit
        var lastBid = bidToLastBid[bid].Item1;
        var lastCoord = bidToLastBid[bid].Item2;
        if (maxCommittedBid < lastBid) await lastCoord.WaitBatchCommit(lastBid);
        maxCommittedBid = bid;
        if (waitBatchCommit.ContainsKey(bid))
        {
            waitBatchCommit[bid].SetResult();
            waitBatchCommit.Remove(bid);
        }

        // infor all related actors
        foreach (var item in batchInfoPerActorPerBatch[bid])
        {
            var actorID = item.Key;
            var actor = GrainFactory.GetGrain<IExtendedTransactionActor>(actorID.id, actorID.className);
            _ = actor.BatchCommit(bid);
        }

        // garbage collection
        batchInfoPerActorPerBatch.Remove(bid);
        bidToLastBid.Remove(bid);
        numActorPerBatch.Remove(bid);
    }

    public async Task WaitBatchCommit(long bid)
    {
        if (maxCommittedBid < bid)
        {
            if (waitBatchCommit.ContainsKey(bid) == false) waitBatchCommit.Add(bid, new TaskCompletionSource());
            await waitBatchCommit[bid].Task;
        }
    }
}