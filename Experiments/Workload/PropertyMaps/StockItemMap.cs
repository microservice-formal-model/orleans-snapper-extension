using Common.Entity;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.Workload.PropertyMaps
{
    public sealed class StockItemMap : ClassMap<StockItem>
    {
        public StockItemMap() 
        {
            Map(it => it.product_id).Index(0);
            Map(it => it.seller_id).Index(1);
            Map(it => it.data).Index(2);
            Map(it => it.qty_reserved).Index(3);
            Map(it => it.qty_available).Index(4);
            Map(it => it.created_at).Index(5);
            Map(it => it.active).Index(6);
            Map(it => it.order_count).Index(7);
            Map(it => it.updated_at).Index(8);
            Map(it => it.ytd).Index(9);
        }
    }
}
