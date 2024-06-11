using Experiments.ExperimentsModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.Execution.Result
{
    public abstract class IResult
    {
        public readonly int ExperimentID;

        public readonly BenchmarkType BenchmarkType;

        public IResult(Experiment experiment)
        {
            ExperimentID = experiment.Id;
            BenchmarkType = experiment.BenchmarkType;
        }   
    }
}
