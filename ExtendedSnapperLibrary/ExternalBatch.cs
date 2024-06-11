using Common.Snapper.Core;

namespace ExtendedSnapperLibrary;

[GenerateSerializer]
public class ExternalBatch
{
    [Id(0)]
    public readonly long bid;

    [Id(1)]
    public readonly long lastBid;

    /// <summary> schedule ID, List of (tid, actor access info) </summary>
    [Id(2)]
    public readonly Dictionary<long, List<(long, Dictionary<ActorID, int>)>> transactions;

    public ExternalBatch(long bid, long lastBid)
    {
        this.bid = bid;
        this.lastBid = lastBid;
        transactions = new Dictionary<long, List<(long, Dictionary<ActorID, int>)>>();
    }

    public void AddTransaction(long scheduleID, long tid, Dictionary<ActorID, int> actorAccessInfo)
    {
        if (!transactions.TryAdd(scheduleID, new() { (tid, actorAccessInfo) }))
        {
            transactions[scheduleID].Add((tid, actorAccessInfo));
        }
    }

    public override string ToString()
    {
        return $"Bid: {bid}, $LastBid: {lastBid}, " + "accesses: \n" +
            $"{string.Join("\n", transactions.Select(tr => $"ScheduleId: {tr.Key}, etids:{string.Join(",", tr.Value.Select(nr => nr.Item1))}"))}";
    }
}