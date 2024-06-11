using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.ExperimentsModel.Csv
{
    public class WorkersInformationMap : ClassMap<WorkersInformation>
    {
        public WorkersInformationMap()
        {
            Map(wi => wi.AmountUpdateProductWorkers);
            Map(wi => wi.AmountCheckoutWorkers);
        }
    }
}
