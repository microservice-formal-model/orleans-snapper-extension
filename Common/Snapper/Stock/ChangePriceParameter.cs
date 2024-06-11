using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Snapper.Stock
{
    [GenerateSerializer]
    public class ChangePriceParameter
    {
        [Id(0)]
        public long price { get; set; }

        [Id(1)]
        public long productId { get; set; }

        public ChangePriceParameter(long price, long productId)
        {
            this.price = price;
            this.productId = productId;
        }
    }
}
