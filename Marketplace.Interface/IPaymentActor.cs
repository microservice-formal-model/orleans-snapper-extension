using System;
using Orleans;
using System.Threading.Tasks;
using SnapperLibrary;

namespace Marketplace.Interfaces
{
    public interface IPaymentActor : IGrainWithIntegerKey
    {
        public Task ProcessFailedOrder(TransactionContext tctx, long customerId, long orderId);
        public Task<object> ProcessPayment(TransactionContext tctx, object input);
        public Task Init();
    }
}

