namespace ExtendedSnapperLibrary
{
    [GenerateSerializer]
    public class TransactionContext
    {
        [Id(0)]
        public readonly long scheduleID;
        [Id(1)]
        public readonly long bid;
        [Id(2)]
        public readonly long tid;

        internal TransactionContext(long scheduleID, long bid, long tid)
        {
            this.scheduleID = scheduleID;
            this.bid = bid;
            this.tid = tid;
        }

        public override string ToString()
        {
            return "Schedule id: " + scheduleID + "bid: " + bid + "tid: " + tid;
        }
    }
}