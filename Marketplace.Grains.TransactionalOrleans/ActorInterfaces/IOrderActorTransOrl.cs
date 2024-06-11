using Common.Snapper.Order;
namespace Marketplace.Grains.TransactionalOrleans.ActorInterfaces
{
    public interface IOrderActorTransOrl : IGrainWithIntegerKey
    {
        [Transaction(TransactionOption.Create)]
        public Task Checkout(CheckoutParameter cp);
        [Transaction(TransactionOption.Join)]
        public Task UpdateOrderStatus(UpdateOrderStatusParameter uosp);
        [Transaction(TransactionOption.NotAllowed)]
        public Task Init();
    }
}
