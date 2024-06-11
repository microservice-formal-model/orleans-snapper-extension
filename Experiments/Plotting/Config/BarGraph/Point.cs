using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.Plotting.Config.BarGraph
{
    public class Point
    {
        public required string Label { get; set; }  

        public required List<int> Ids { get; set; }
    }
}
