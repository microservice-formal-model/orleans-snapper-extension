using CsvHelper.Configuration;
using Experiments.Workload.PropertyMaps.TypeConverter;
using Marketplace.Grains.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.Workload.PropertyMaps
{
    public sealed class CheckoutMap : ClassMap<Checkout>
    {
        public CheckoutMap() 
        {
            Map(ch => ch.CreatedAt);
            Map(ch => ch.Items).TypeConverter<ItemsNodeConverter>();
            References<CustomerCheckoutMap>(ch => ch.CustomerCheckout);
        }
    }
}
