using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.Plotting.Config.LineGraph
{
    public class Point
    {
        public required int Label { get; set; }

        public required List<int> Ids { get; set; }
    }
}
