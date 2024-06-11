using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.ExperimentsModel.Csv
{
    public class ExperimentMap : ClassMap<Experiment>
    {
        public ExperimentMap() 
        {
            Map(e => e.Id);
            Map(e => e.IsLocal);
            Map(e => e.AmountProducts);
            References<PartitioningMap>(e => e.Partitioning);
            References<DistributionMap>(e => e.Distribution);
            References<CheckoutInformationMap>(e => e.CheckoutInformation);
            References<UpdateProductInformationMap>(e => e.UpdateProductInformation);
        }
    }
}
