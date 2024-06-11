using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.Plotting.Config
{
    public class Plotting
    {
        public required string Location { get; set; }

        public required List<APlot> Plots { get; set; }
    }
}
