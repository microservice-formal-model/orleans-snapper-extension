using Common.Snapper.Product;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.Workload.PropertyMaps
{
    public sealed class UpdateProductParameterMap : ClassMap<UpdateProductParameter>
    {
        public UpdateProductParameterMap() 
        {
            Map(up => up.Quantity);
            References<ProductMap>(up => up.Product);
        }
    }
}
