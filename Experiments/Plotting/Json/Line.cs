using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.Plotting.Json
{
    internal class Line
    {
        public required string Name { get; set; }   

        public required List<int> Y { get; set; }

        public required List<int> X { get; set; }
    }
}
