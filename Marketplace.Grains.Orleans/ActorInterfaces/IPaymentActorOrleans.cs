using Marketplace.Grains.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marketplace.Grains.Orleans.ActorInterfaces
{
    public interface IPaymentActorOrleans : IGrainWithIntegerKey
    {
        public Task ProcessPayment(Invoice invoice);

        public Task Init();
    }
}
