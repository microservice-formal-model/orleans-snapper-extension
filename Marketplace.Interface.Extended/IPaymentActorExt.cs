using ExtendedSnapperLibrary;
using ExtendedSnapperLibrary.ActorInterface;

namespace Marketplace.Interface.Extended
{
    public interface IPaymentActorExt : IGrainWithIntegerKey, IExtendedTransactionActor
    {
        public Task ProcessFailedOrder(TransactionContext tctx, long customerId, long orderId);
        public Task<object> ProcessPayment(TransactionContext tctx, object input);
        public Task Init();
    }
}
