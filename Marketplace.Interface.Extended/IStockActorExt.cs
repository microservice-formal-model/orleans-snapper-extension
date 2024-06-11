using Common.Entity;
using ExtendedSnapperLibrary;
using ExtendedSnapperLibrary.ActorInterface;

namespace Marketplace.Interface.Extended
{
    public interface IStockActorExt : IGrainWithIntegerKey, IExtendedTransactionActor
    {
        public Task<object> DeleteItem(TransactionContext tctx, object input);
        public Task<object> AttemptReservation(TransactionContext tctx, object input);
        public Task<object> CancelReservation(TransactionContext tctx, object input);
        public Task<object> ConfirmReservation(TransactionContext tctx, object input);
        public Task<object> ConfirmOrder(TransactionContext tctx, object input);

        // from seller
        public Task<object> IncreaseStock(TransactionContext tctx, object Input);

        public Task<object> Noop(TransactionContext tctx, object input);

        // API
        public Task<object> AddItem(StockItem item);
        public Task<StockItem> GetItem(TransactionContext tctx, long itemId);
        public Task Init();
    }
}
