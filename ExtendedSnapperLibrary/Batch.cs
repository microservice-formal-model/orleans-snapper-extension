using ExtendedSnapperLibrary.ActorInterface;
using System.Diagnostics;

namespace ExtendedSnapperLibrary;

[GenerateSerializer]
public class Batch
{
    [Id(0)]
    public readonly long bid;

    [Id(1)]
    public long lastBid;

    /// <summary> the coordinator who emits this batch </summary>
    [Id(2)]
    internal readonly IExtendedCoordinator coordinator;

    /// <summary> schedule ID, (tid, number of access on this actor) </summary>
    [Id(3)]
    internal Dictionary<long, List<(long, Counter)>> transactions;

    public Batch(long bid, IExtendedCoordinator coordinator)
    {
        this.bid = bid;
        lastBid = -1;
        this.coordinator = coordinator;
        transactions = new Dictionary<long, List<(long, Counter)>>();
    }

    internal Tuple<bool, bool> FinishAccess(long scheduleID, long tid)
    {
        Debug.Assert(transactions[scheduleID].First().Item1 == tid);
        var isTxnFinished = transactions[scheduleID].First().Item2.Decrement();
        if (isTxnFinished) transactions[scheduleID].RemoveAt(0);

        if (transactions[scheduleID].Count == 0) transactions.Remove(scheduleID);
        var isBatchFinished = transactions.Count == 0;
        return new Tuple<bool, bool>(isTxnFinished, isBatchFinished);
    }

    internal void AddTransaction(long scheduleID, long tid, int num)
    {
        if (!transactions.ContainsKey(scheduleID)) transactions.Add(scheduleID, new List<(long, Counter)>());
        transactions[scheduleID].Add((tid, new Counter(num)));
    }

    internal long GetPrevTransaction(long scheduleID, long tid)
    {
        var index = 0;
        while (transactions[scheduleID][index].Item1 != tid) index++;

        if (index == 0) return -1;
        return transactions[scheduleID][index - 1].Item1;
    }
}