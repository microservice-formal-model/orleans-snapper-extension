using Experiments.ExperimentsModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.Execution.Result
{
    public class AverageLatencyResult : IResult
    {
        public readonly int AverageLatency;

        public AverageLatencyResult(int AverageLatency, Experiment experiment) : base(experiment)
        {
            this.AverageLatency = AverageLatency;
        }

        public override void PrintResult()
        {
            Console.WriteLine($"Average Latency: {AverageLatency} ms");
        }
    }
}
