using Common.Snapper.Order;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.Workload.PropertyMaps
{
    public sealed class CheckoutParameterMap : ClassMap<CheckoutParameter>
    {
        public CheckoutParameterMap() 
        {
            Map(cp => cp.OrderId);
            References<CheckoutMap>(cp => cp.Checkout);
        }
    }
}
