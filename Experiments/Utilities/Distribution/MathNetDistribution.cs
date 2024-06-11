using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.Utilities.Distribution
{
    public class MathNetDistribution : IProcessedDistribution
    {
        private readonly IDiscreteDistribution StoredDistribution;

        private readonly ExperimentsModel.Distribution.DistributionType DistributionType;

        public MathNetDistribution(IDiscreteDistribution storedDistribution, ExperimentsModel.Distribution.DistributionType distributionType)
        {
            this.StoredDistribution = storedDistribution;
            this.DistributionType = distributionType;
        }

        public int GetSample(List<int> _)
        {
            if (DistributionType == ExperimentsModel.Distribution.DistributionType.UNIFORM)
            {
                return StoredDistribution.Sample();
            }
            else
            {
                return StoredDistribution.Sample() - 1;
            }
        }
    }
}
