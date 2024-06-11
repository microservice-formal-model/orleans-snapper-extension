using Common.Entity;
using Common.Snapper.Stock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marketplace.Grains.Orleans.ActorInterfaces
{
    public interface IStockActorOrleans : IGrainWithIntegerKey
    {
        public Task DeleteItem(DeleteItemParameter dip);
        public Task<ItemStatus> AttemptReservation(AttemptReservationParameter arp);
        public Task CancelReservation(CancelReservationParameter crp);
        public Task ConfirmReservation(ConfirmReservationParameter crp);
        public Task ConfirmOrder(ConfirmOrderParameter cop);

        public Task ReviveItem(DeleteItemParameter dip);

        // from seller
        public Task<(ItemStatus,ItemStatus)> IncreaseStock(IncreaseStockParameter isp);

        // API
        public Task AddItem(StockItem item);
    }
}
