using Experiments.Execution.Result;
using Experiments.ExperimentsModel;
using Experiments.Plotting.Json;

namespace Experiments.Plotting.Config
{
    public abstract class APlot
    {
        public required string FileName { get; set; }
        public required List<int> Ids { get; set; } 

        public required string Name { get; set; }

        public required YAxisType YAxisType { get; set; }

        public abstract AJsonGraph GeneratePlot(List<IResult> experimentResults);

        internal static bool MatchTypes(YAxisType yAxisType, IResult e)
        {
            if (e is AverageLatencyResult)
            {
                return yAxisType == YAxisType.LATENCY;
            }
            else if (e is AverageThroughputResult)
            {
                return yAxisType == YAxisType.THROUGHPUT;
            }
            return false;
        }

        internal static double GetResult(IResult resultsWithIdAndBenchmark)
        {
            if (resultsWithIdAndBenchmark is AverageLatencyResult alr)
            {
                return alr.AverageLatency;
            }
            else if (resultsWithIdAndBenchmark is AverageThroughputResult atr)
            {
                return atr.TransactionsPerSecond;
            }
            else return -1;
        }

    }
}
