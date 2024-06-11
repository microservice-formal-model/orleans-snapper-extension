using Experiments.Execution.Result;
using Experiments.ExperimentsModel;
using System.Diagnostics;

namespace Experiments.Execution.Latency
{
    internal class AverageLatencyStrategy : ILatencyStrategy
    {
        List<long> sumOfLatencies = new() { 0 };
        int currentIndex = 0;
        int nrOperations = 0;
        public void CollectLatency(Stopwatch operationDuration)
        {
            nrOperations++;
            if (sumOfLatencies[currentIndex] + operationDuration.ElapsedMilliseconds > int.MaxValue)
            {
                currentIndex++;
                sumOfLatencies.Add(0);
                sumOfLatencies[currentIndex] += operationDuration.ElapsedMilliseconds;
            } else
            {
                sumOfLatencies[currentIndex] += operationDuration.ElapsedMilliseconds;
            }
        }

        public LatencyType GetLatencyType()
        {
            return LatencyType.AVERAGE;
        }

        /// <summary>
        /// Returns result for average latency. 
        /// </summary>
        /// <param name="experiment">The experiment that this result belongs to.</param>
        /// <throws><c>OverflowException</c>, if the sum of all latencies is to big.</throws>
        /// <returns></returns>
        public IResult GetResult(Experiment experiment) 
        {
            int total = 0;
            foreach (long sum in sumOfLatencies)
            {
                total += Convert.ToInt32(sum) / nrOperations;
            }
            //Console.WriteLine("Total time of all operations: " + sumOfLatencies[currentIndex]);
            //Console.WriteLine("Total amount of operations: " + nrOperations);
            Console.WriteLine("Total latency: " + total);
            return new AverageLatencyResult(total, experiment);
        }
    }
}
