using ExtendedSnapperLibrary;
using ExtendedSnapperLibrary.ActorInterface;

namespace Marketplace.Interface.Extended
{
    public interface IOrderActorExt : IGrainWithIntegerKey, IExtendedTransactionActor
    {
        public Task<object> Checkout(TransactionContext tctx, object input);
        public Task<object> UpdateOrderStatus(TransactionContext tctx, object input);

        public Task Init();

        public Task<long> GetNextOrderId();
    }
}
