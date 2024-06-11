using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.Utilities.Distribution
{
    public interface IProcessedDistribution
    {
        public int GetSample(List<int> without);
    }
}
