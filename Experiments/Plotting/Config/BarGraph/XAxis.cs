using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.Plotting.Config.BarGraph
{
    public class XAxis
    {
        public required string Label { get; set; }

        public required List<Point> Points { get; set; }
    }
}
