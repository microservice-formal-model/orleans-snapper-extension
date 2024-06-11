using Common.Entity;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Snapper.Order
{
    [GenerateSerializer]
    public class UpdateOrderStatusParameter
    {
        [Id(0)]
        public long orderId { get; set; }

        [Id(1)]
        public OrderStatus orderStatus { get; set; }

        public UpdateOrderStatusParameter(long orderId, OrderStatus orderStatus)
        {
            this.orderId = orderId;
            this.orderStatus = orderStatus;
        }
    }
}
