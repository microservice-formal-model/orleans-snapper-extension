using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.Plotting.Json
{
    public class Series
    {
        public required string Name { get; set; }

        public required List<double> YValues;

        public required List<string> Text;

        public required List<string> X;

        public required string Color { get; set; }

    }
}
