using System.Diagnostics;

namespace ExtendedSnapperLibrary
{
    internal class Schedule
    {
        readonly long actorID;
        long maxCompletedBid;
        long maxCommittedBid;                                                                       // the max bid committed by this actor so far
        Dictionary<long, Batch> batches;                                                            // <bid, batch info>
        Dictionary<long, TaskCompletionSource> waitBatchMsg;                                        // <bid, async task>, the task is set completed after the batch info message has arrived
        Dictionary<long, TaskCompletionSource> waitBatchComplete;                                   // <bid, async task>, the task is set completed after the whole batch has completed on the actor
        Dictionary<long, Dictionary<long, Dictionary<long, TaskCompletionSource>>> waitTxnComplete; // <bid, schedule ID, <tid, async task>>, the task is set completed when the transaction has completed on the actor
        Dictionary<long, TaskCompletionSource> waitBatchCommit;                                     // <bid, the task is set completed when the BatchCommit message arrives>

        internal Schedule(long actorID)
        {
            this.actorID = actorID;
            maxCompletedBid = -1;
            maxCommittedBid = -1;
            batches = new Dictionary<long, Batch>();
            waitBatchMsg = new Dictionary<long, TaskCompletionSource>();
            waitBatchComplete = new Dictionary<long, TaskCompletionSource>();
            waitTxnComplete = new Dictionary<long, Dictionary<long, Dictionary<long, TaskCompletionSource>>>();
            waitBatchCommit = new Dictionary<long, TaskCompletionSource>();
        }

        internal void CheckGarbageCollection()
        {
            if (batches.Count != 0)
                Console.WriteLine($"Schedule: batches has {batches.Count} entries");
            if (waitBatchMsg.Count != 0)
                Console.WriteLine($"Schedule: waitBatchMsg has {waitBatchMsg.Count} entries");
            if (waitBatchComplete.Count != 0)
                Console.WriteLine($"Schedule: waitBatchComplete has {waitBatchComplete.Count} entries");
            if (waitTxnComplete.Count != 0)
                Console.WriteLine($"Schedule: waitTxnComplete has {waitTxnComplete.Count} entries");
            if (waitBatchCommit.Count != 0)
                Console.WriteLine($"Schedule: waitBatchCommit has {waitBatchCommit.Count} entries");
        }

        internal void RegisterBatch(Batch batch)
        {
            batches.Add(batch.bid, batch);
            Debug.Assert(waitTxnComplete.ContainsKey(batch.bid) == false);

            waitTxnComplete.Add(batch.bid, new Dictionary<long, Dictionary<long, TaskCompletionSource>>());
            foreach (var item in batch.transactions)
            {
                var scheduleID = item.Key;
                waitTxnComplete[batch.bid].Add(scheduleID, new Dictionary<long, TaskCompletionSource>());
                foreach (var txn in item.Value)
                    waitTxnComplete[batch.bid][scheduleID].Add(txn.Item1, new TaskCompletionSource());
            }

            if (waitBatchMsg.ContainsKey(batch.bid)) waitBatchMsg[batch.bid].SetResult();
        }

        internal async Task WaitForTurn(TransactionContext cxt)
        {
            if (batches.ContainsKey(cxt.bid) == false) await WaitForBatchInfo(cxt.bid);
            var batchInfo = batches[cxt.bid];

           

            var prev = batchInfo.GetPrevTransaction(cxt.scheduleID, cxt.tid);
            if (prev != -1) await waitTxnComplete[cxt.bid][cxt.scheduleID][prev].Task;
            else if (batchInfo.lastBid != -1) await WaitForBatchCompletion(batchInfo.lastBid);
        }

        internal void FinishFunction(TransactionContext cxt)
        {
            var batchInfo = batches[cxt.bid];
            var res = batchInfo.FinishAccess(cxt.scheduleID, cxt.tid);
            if (res.Item1)
            {
                waitTxnComplete[cxt.bid][cxt.scheduleID][cxt.tid].SetResult();
                if (res.Item2)
                {
                    Debug.Assert(maxCompletedBid < cxt.bid);
                    maxCompletedBid = cxt.bid;
                    if (waitBatchComplete.ContainsKey(cxt.bid)) waitBatchComplete[cxt.bid].SetResult();
                    _ = batchInfo.coordinator.BatchComplete(cxt.bid);
                }
            }
        }

        internal void BatchCommit(long bid)
        {
            maxCommittedBid = Math.Max(maxCommittedBid, bid);

            // garbage collection
            if (waitBatchCommit.ContainsKey(bid))
            {
                waitBatchCommit[bid].SetResult();
                waitBatchCommit.Remove(bid);
            }
            if (waitBatchComplete.ContainsKey(bid)) waitBatchComplete.Remove(bid);
            if (waitBatchMsg.ContainsKey(bid)) waitBatchMsg.Remove(bid);
            batches.Remove(bid);
            waitTxnComplete.Remove(bid);
        }

        async Task WaitForBatchInfo(long bid)
        {
            if (waitBatchMsg.ContainsKey(bid) == false) waitBatchMsg.Add(bid, new TaskCompletionSource());
            await waitBatchMsg[bid].Task;
        }

        async Task WaitForBatchCompletion(long bid)
        {
            if (maxCompletedBid < bid)
            {
                if (waitBatchComplete.ContainsKey(bid) == false) waitBatchComplete.Add(bid, new TaskCompletionSource());
                await waitBatchComplete[bid].Task;
            }
        }

        internal async Task WaitForBatchCommit(long bid)
        {
            if (maxCommittedBid < bid)
            {
                if (waitBatchCommit.ContainsKey(bid) == false) waitBatchCommit.Add(bid, new TaskCompletionSource());
                await waitBatchCommit[bid].Task;
            }
        }
    }
}