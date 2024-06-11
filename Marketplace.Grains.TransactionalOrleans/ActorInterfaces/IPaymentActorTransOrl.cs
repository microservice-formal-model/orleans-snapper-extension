using Marketplace.Grains.Message;

namespace Marketplace.Grains.TransactionalOrleans.ActorInterfaces
{
    public interface IPaymentActorTransOrl : IGrainWithIntegerKey
    {
        [Transaction(TransactionOption.Join)]
        public Task ProcessPayment(Invoice invoice);
        [Transaction(TransactionOption.NotAllowed)]
        public Task Init();
    }
}
