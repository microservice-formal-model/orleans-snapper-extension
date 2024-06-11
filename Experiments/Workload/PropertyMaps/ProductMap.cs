using Common.Entity;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.Workload.PropertyMaps
{
    public sealed class ProductMap : ClassMap<Product>
    {
        public ProductMap() {
            Map(p => p.product_id);
            Map(p => p.price);           
            Map(p => p.freight_value);
            Map(p => p.sku);
            Map(p => p.category_name);
            Map(p => p.created_at);
            Map(p => p.updated_at);
            Map(p => p.description);
            Map(p => p.name);
            Map(p => p.active);
            Map(p => p.seller_id);
            Map(p => p.status);   
        }
    }
}
