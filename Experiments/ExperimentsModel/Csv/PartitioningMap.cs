using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.ExperimentsModel.Csv
{
    public class PartitioningMap : ClassMap<Partitioning>
    {
        public PartitioningMap()
        {
            Map(p => p.NOrderPartitions);
            Map(p => p.NPaymentPartitions);
            Map(p => p.NProductPartitions);
            Map(p => p.NStockPartitions);
        }
    }
}
