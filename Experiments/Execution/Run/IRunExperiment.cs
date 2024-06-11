using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Experiments.Execution.Latency;
using Experiments.Execution.Throughput;

namespace Experiments.Execution.Run
{
    public interface IRunExperiment
    {
        Task RunExperiment(ILatencyStrategy latencyStrategy, IThroughputStrategy throughputStrategy);

        Task ExecuteCheckoutWorker(int workerId, int globalWorkerId, Barrier barrier);

        Task ExecuteUpdateProductWorker(int workerId, int globalWorkerId, Barrier barrier);
        Task<ILatencyStrategy?> ReceiveLatencyResult();

        Task<IThroughputStrategy?> ReceiveThroughputResult();
    }
}
