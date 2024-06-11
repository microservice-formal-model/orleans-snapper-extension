using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.ExperimentsModel.Csv
{
    public class UpdateProductInformationMap : ClassMap<UpdateProductInformation>
    {
        public UpdateProductInformationMap()
        {
            Map(ui => ui.TotalAmount);
            Map(ui => ui.MinimumReplenish);
            Map(ui => ui.MaximumReplenish);
        }
    }
}
