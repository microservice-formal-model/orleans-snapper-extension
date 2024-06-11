namespace SnapperLibrary
{
    [GenerateSerializer]
    public class TransactionContext
    {
        [Id(0)]
        public readonly int scheduleID = 0;
        [Id(1)]
        public readonly long bid;
        [Id(2)]
        public readonly long tid;

        internal TransactionContext(long bid, long tid)
        {
            this.bid = bid;
            this.tid = tid;
        }
    }
}