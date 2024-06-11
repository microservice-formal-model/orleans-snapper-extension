using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.ExperimentsModel.Csv
{
    public class DistributionMap : ClassMap<Distribution>
    {
        public DistributionMap()
        {
            Map(d => d.ZipfianConstant);
            Map(d => d.Type);
        }
    }
}
