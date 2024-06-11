﻿using System.Diagnostics;

namespace SnapperLibrary
{
    internal class Schedule
    {
        readonly long actorID;
        readonly string actorName;
        long maxCompletedBid;
        long maxCommittedBid;                                                     // the max bid committed by this actor so far
        Dictionary<long, Batch> batches;                                          // <bid, batch info>
        Dictionary<long, TaskCompletionSource> waitBatchMsg;                      // <bid, async task>, the task is set completed after the batch info message has arrived
        Dictionary<long, TaskCompletionSource> waitBatchComplete;                 // <bid, async task>, the task is set completed after the whole batch has completed on the actor
        Dictionary<long, Dictionary<long, TaskCompletionSource>> waitTxnComplete; // <bid, <tid, async task>>, the task is set completed when the transaction has completed on the actor
        Dictionary<long, TaskCompletionSource> waitBatchCommit;                   // <bid, the task is set completed when the BatchCommit message arrives>

        internal Schedule(long actorID, string actorName)
        {
            this.actorID = actorID;
            this.actorName = actorName;
            maxCompletedBid = -1;
            maxCommittedBid = -1;
            batches = new Dictionary<long, Batch>();
            waitBatchMsg = new Dictionary<long, TaskCompletionSource>();
            waitBatchComplete = new Dictionary<long, TaskCompletionSource>();
            waitTxnComplete = new Dictionary<long, Dictionary<long, TaskCompletionSource>>();
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
            waitTxnComplete.Add(batch.bid, new Dictionary<long, TaskCompletionSource>());
            foreach (var txn in batch.transactions) waitTxnComplete[batch.bid].Add(txn.Key, new TaskCompletionSource());

            if (waitBatchMsg.ContainsKey(batch.bid)) waitBatchMsg[batch.bid].SetResult();
            //Console.WriteLine($"actor {actorID}-{actorName}: waitBatchMsg receive bid {batch.bid}");
        }

        internal async Task WaitForTurn(TransactionContext cxt)
        {
            if (batches.ContainsKey(cxt.bid) == false)
            {
                if (waitBatchMsg.ContainsKey(cxt.bid) == false) waitBatchMsg.Add(cxt.bid, new TaskCompletionSource());
                //Console.WriteLine($"actor {actorID}-{actorName}: wait for batch info {cxt.bid}");
                await waitBatchMsg[cxt.bid].Task;
                //Console.WriteLine($"{actorID}: get batch info {cxt.bid}");
            }
            var batchInfo = batches[cxt.bid];
            var prev = batchInfo.GetPrevTransaction(cxt.tid);
            if (prev != -1) await waitTxnComplete[cxt.bid][prev].Task;
            else if (batchInfo.lastBid != -1) await WaitForBatchCompletion(batchInfo.lastBid);
        }

        internal void FinishFunction(TransactionContext cxt)
        {
            var batchInfo = batches[cxt.bid];
            var res = batchInfo.FinishAccess(cxt.tid);
            if (res.Item1)
            {
                waitTxnComplete[cxt.bid][cxt.tid].SetResult();
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