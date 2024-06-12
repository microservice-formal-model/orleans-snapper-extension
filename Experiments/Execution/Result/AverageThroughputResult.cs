using Experiments.ExperimentsModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.Execution.Result
{
    public class AverageThroughputResult : IResult
    {
        public readonly int TransactionsPerSecond;

        public AverageThroughputResult(int TransactionsPerSecond, Experiment experiment) : base(experiment) 
        {
            this.TransactionsPerSecond = TransactionsPerSecond;

        }

        public override void PrintResult()
        {
            Console.WriteLine($"Average Throughput: {TransactionsPerSecond} Tr/sec");
        }
    }
}
