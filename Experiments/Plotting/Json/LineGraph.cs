using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.Plotting.Json
{
    internal class LineGraph : AJsonGraph
    {
        public required string Type { get; set; }

        public required List<Line> Lines { get; set; }
    }
}
