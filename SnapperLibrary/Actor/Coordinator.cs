using System.Diagnostics;
using Common.Snapper.Core;
using Orleans.Concurrency;
using SnapperLibrary.ActorInterface;
using Utilities;

namespace SnapperLibrary.Actor
{
    [Reentrant]
    public class Coordinator : Grain, ICoordinator
    {
        long myID;
        ICoordinator neighbor;

        long maxCommittedBid;
        List<Dictionary<ActorID, int>> transactions;                      // actor access info for each transaction received so far
        List<TaskCompletionSource<TransactionContext>> waitTxnContext;    // wait for the context for each transaction in the 'transactions' list
        
        Dictionary<long, Dictionary<ActorID, Batch>> batchInfoPerActorPerBatch;       // bid, actor ID, batch info
        Dictionary<long, Tuple<long, long>> bidToLastBid;                             // bid, last bid, coord id
        Dictionary<long, Counter> numActorPerBatch;                                   // bid, num accessed actors
        Dictionary<long, TaskCompletionSource> waitBatchCommit;

        private readonly Helper helper;

        public Coordinator(Helper helper)
        {
            this.helper = helper;
        }

        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            maxCommittedBid = -1;
            myID = this.GetPrimaryKeyLong();
            transactions = new List<Dictionary<ActorID, int>>();
            waitTxnContext = new List<TaskCompletionSource<TransactionContext>>();
            batchInfoPerActorPerBatch = new Dictionary<long, Dictionary<ActorID, Batch>>();
            bidToLastBid = new Dictionary<long, Tuple<long, long>>();
            numActorPerBatch = new Dictionary<long, Counter>();
            waitBatchCommit = new Dictionary<long, TaskCompletionSource>();

            var neighborID = helper.MapCoordIDToNeighborID(myID);
            neighbor = GrainFactory.GetGrain<ICoordinator>(neighborID);
            //Console.WriteLine($"Coordinator {myID} is started, scheduleID = {scheduleID}, neighbor = {neighborID}");
            return base.OnActivateAsync(cancellationToken);
        }

        public Task Init()
        {
            _ = PassToken(new Token());
            return Task.CompletedTask;
        }

        public Task CheckGarbageCollection()
        {
            if (transactions.Count != 0)
                Console.WriteLine($"Coordinator: transactions has {transactions.Count} entries");
            if (waitTxnContext.Count != 0)
                Console.WriteLine($"Coordinator: waitTxnContext has {waitTxnContext.Count} entries");
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

        public async Task<TransactionContext> NewTransaction(Dictionary<ActorID, int> actorAccessInfo)
        {
            transactions.Add(actorAccessInfo);
            var promise = new TaskCompletionSource<TransactionContext>();
            waitTxnContext.Add(promise);
            return await promise.Task;
        }

        public async Task PassToken(Token token)
        {
            var bid = GenerateBatch(token);

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

            if (bid != -1) await EmitBatch(bid);
        }

        long GenerateBatch(Token token)
        {
            // STEP 1: check if there are any transactions to emit
            if (transactions.Count == 0) return -1;
            Debug.Assert(transactions.Count == waitTxnContext.Count);

            // STEP 2: generate the batch info for each accessed actor
            // Assume we use the tid of the first transaction in the batch as bid
            var bid = token.lastTid + 1;
            bidToLastBid.Add(bid, new Tuple<long, long>(token.lastBid, token.lastCoord));
            batchInfoPerActorPerBatch.Add(bid, new Dictionary<ActorID, Batch>());
            token.lastBid = bid;
            token.lastCoord = myID;
            
            var actors = batchInfoPerActorPerBatch[bid];
            for (var i = 0; i < transactions.Count; i++)
            {
                var txn = transactions[i];
                var cxt = waitTxnContext[i];

                var tid = ++token.lastTid;

                cxt.SetResult(new TransactionContext(bid, tid));

                foreach (var actor in txn)
                {
                    if (actors.ContainsKey(actor.Key) == false)
                    {
                        if (token.lastBidPerActor.ContainsKey(actor.Key) == false)
                            actors.Add(actor.Key, new Batch(bid, -1, this));
                        else
                            actors.Add(actor.Key, new Batch(bid, token.lastBidPerActor[actor.Key], this));
                        token.lastBidPerActor[actor.Key] = bid;
                    }
                    actors[actor.Key].transactions.Add(tid, new Counter(actor.Value));
                }
            }

            // STEP 3: garbage collection
            transactions.Clear();
            waitTxnContext.Clear();
            return bid;
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

                var actor = GrainFactory.GetGrain<ITransactionActor>(actorID.id, actorID.className);
                //Console.WriteLine($"coord send batch to actor {actorID.id}-{actorID.className}");
                await actor.ReceiveBatch(subBatch);
               // Console.WriteLine($"coord finish send batch to actor {actorID.id}-{actorID.className}");
            }
        }

        public async Task BatchComplete(long bid)
        {
            if (numActorPerBatch[bid].Decrement() == false) return;

            // wait for previous batch to commit
            var lastBid = bidToLastBid[bid].Item1;
            var lastCoord = GrainFactory.GetGrain<ICoordinator>(bidToLastBid[bid].Item2);
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
                var actor = GrainFactory.GetGrain<ITransactionActor>(actorID.id, actorID.className);
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
}