using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Snapper.Stock
{
    [GenerateSerializer]
    public class CancelReservationParameter
    {
        [Id(0)]
        public long productId { get; set; }
        [Id(1)]
        public int quantity { get; set; }

        public CancelReservationParameter(long productId, int quantity)
        {
            this.productId = productId;
            this.quantity = quantity;
        }
    }
}
