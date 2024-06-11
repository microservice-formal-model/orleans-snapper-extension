using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.ExperimentsModel.Csv
{
    public class CheckoutInformationMap : ClassMap<CheckoutInformation>
    {
        public CheckoutInformationMap() 
        {
            Map(ci => ci.Size);
            Map(ci => ci.TotalAmount);
        }
    }
}
