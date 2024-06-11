using Experiments.Execution.Result;
using Experiments.ExperimentsModel;
using System.Diagnostics;

namespace Experiments.Execution.Latency
{
    public interface ILatencyStrategy
    {
        void CollectLatency(Stopwatch operationDuration);

        LatencyType GetLatencyType();

        IResult GetResult(Experiment experiment);
    }
}
