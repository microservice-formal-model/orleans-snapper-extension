using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.Plotting.Config.LineGraph
{
    public class XAxis
    {
        public required List<Point> Points { get; set; }

        public required string Label { get; set; }
    }
}
