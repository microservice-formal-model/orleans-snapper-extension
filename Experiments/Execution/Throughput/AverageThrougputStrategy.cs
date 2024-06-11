using Experiments.Execution.Result;
using Experiments.ExperimentsModel;

namespace Experiments.Execution.Throughput
{
    public class AverageThrougputStrategy : IThroughputStrategy
    {
        private DateTime? start;

        private readonly TimeSpan MAX_THROUGHPUT_TIME;
        int transactionsCount;
        /// <summary>
        /// Lock to avoid concurrent addition of start points.
        /// This lock will only be used once in the beginning of the experiments
        /// and does not decreas the performance when measuring throughput.
        /// </summary>
        private readonly object startPointLock;
        public AverageThrougputStrategy(int runtime)
        {
            start = null;
            transactionsCount = 0;
            startPointLock = new();
            MAX_THROUGHPUT_TIME = TimeSpan.FromSeconds(runtime);
        }

        public ThroughputType GetThroughputType()
        {
            return ThroughputType.AVERAGE;
        }

        IResult IThroughputStrategy.GetResult(Experiment experiment)
        {
            //We have 14000 tr in 2400ms then we need to divide it by 2400 
            try
            {
                //Calculate the amount of transactions per 1000 milliseconds -> 1 sec    
                double transactionsPerSecond = transactionsCount / MAX_THROUGHPUT_TIME.TotalMilliseconds * 1000.0d;
                return new AverageThroughputResult(Convert.ToInt32(transactionsPerSecond),experiment);
            }
            catch (Exception)
            {
                throw;
            }   
        }

        public void AddStartPoint(DateTime when)
        {
            lock (startPointLock)
            {
                if(start == null)
                {
                    start = when;
                }
            }
        }

        public void CollectThroughput(DateTime when)
        {
            //If the time distance to our start point is smaller
            //then the desired measurement period
            if(start != null && when - start <= MAX_THROUGHPUT_TIME)
            {
                transactionsCount++;
            }
        }
    }
}
