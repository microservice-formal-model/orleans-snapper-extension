using Experiments.Execution.Result;
using Experiments.ExperimentsModel;
using System.Diagnostics;

namespace Experiments.Execution.Throughput
{
    public interface IThroughputStrategy
    {

        void CollectThroughput(DateTime when);

        void AddStartPoint(DateTime when);
        IResult GetResult(Experiment experiment);

        ThroughputType GetThroughputType();
    }
}
