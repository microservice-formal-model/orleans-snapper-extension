using SnapperLibrary.ActorInterface;
using System.Diagnostics;

namespace SnapperLibrary
{
    [GenerateSerializer]
    public class Batch
    {
        [Id(0)]
        internal readonly int scheduleID = 0;
        [Id(1)]
        internal readonly long bid;
        [Id(2)]
        internal readonly long lastBid;
        [Id(3)]
        internal readonly ICoordinator coordinator;

        // It is the list of transactions that will access this actor
        // Transactions in the list should be executed in the order of tids[Id(0)]
        [Id(4)]
        internal SortedList<long, Counter> transactions;

        internal Batch(long bid, long lastBid, ICoordinator coordinator)
        {
            this.bid = bid;
            this.lastBid = lastBid;
            this.coordinator = coordinator;
            transactions = new SortedList<long, Counter>();
        }

        internal void AddTransaction(long tid, int num)
        {
            Debug.Assert(transactions.ContainsKey(tid) == false);
            transactions.Add(tid, new Counter(num));
        }

        internal Tuple<bool, bool> FinishAccess(long tid) 
        {
            Debug.Assert(transactions.First().Key == tid);
            var isTxnFinished = transactions.First().Value.Decrement();
            if (isTxnFinished) transactions.RemoveAt(0);

            var isBatchFinished = transactions.Count == 0;
            return new Tuple<bool, bool>(isTxnFinished, isBatchFinished);
        }

        internal long GetPrevTransaction(long tid)
        {
            Debug.Assert(transactions.ContainsKey(tid));
            var index = transactions.IndexOfKey(tid);
            if (index == 0) return -1;
            return transactions.GetKeyAtIndex(index - 1);
        }
    }
}