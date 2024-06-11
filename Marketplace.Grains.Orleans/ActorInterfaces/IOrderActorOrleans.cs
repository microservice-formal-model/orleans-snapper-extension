using Common.Snapper.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marketplace.Grains.Orleans.ActorInterfaces
{
    public interface IOrderActorOrleans : IGrainWithIntegerKey
    {
        public Task Checkout(CheckoutParameter cp);
        public Task UpdateOrderStatus(UpdateOrderStatusParameter uosp);

        public Task Init();
    }
}
