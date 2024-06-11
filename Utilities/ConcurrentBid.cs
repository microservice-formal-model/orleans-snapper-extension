namespace Utilities
{
    public class ConcurrentBid
    {
        private int bid;

        public ConcurrentBid()
        {
            bid = -1;
        }

        public int GetNextBid()
        {
            lock (this)
            {
                bid += 1;
                return bid;
            }
        }
    }
}
