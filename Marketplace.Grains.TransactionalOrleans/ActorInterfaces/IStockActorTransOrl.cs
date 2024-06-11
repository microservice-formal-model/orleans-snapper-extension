using Common.Entity;
using Common.Snapper.Stock;

namespace Marketplace.Grains.TransactionalOrleans.ActorInterfaces
{
    public interface IStockActorTransOrl : IGrainWithIntegerKey
    {
        [Transaction(TransactionOption.Join)]
        public Task DeleteItem(DeleteItemParameter dip);
        [Transaction(TransactionOption.Join)]
        public Task<ItemStatus> AttemptReservation(AttemptReservationParameter arp);
        [Transaction(TransactionOption.Join)]
        public Task CancelReservation(CancelReservationParameter crp);
        [Transaction(TransactionOption.Join)]
        public Task ConfirmReservation(ConfirmReservationParameter crp);
        [Transaction(TransactionOption.Join)]
        public Task ConfirmOrder(ConfirmOrderParameter cop);
        [Transaction(TransactionOption.Join)]

        public Task ReviveItem(DeleteItemParameter dip);
        [Transaction(TransactionOption.Join)]

        // from seller
        public Task<(ItemStatus, ItemStatus)> IncreaseStock(IncreaseStockParameter isp);
        [Transaction(TransactionOption.Create)]
        // API
        public Task AddItem(StockItem item);
    }
}
