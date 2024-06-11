using Common.Entity;
using Common.Snapper.Stock;
using Marketplace.Grains.Orleans.ActorInterfaces;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;

namespace Marketplace.Grains.Orleans.Actors
{
    [Reentrant]
    public class StockActorOrleans : Grain, IStockActorOrleans
    {
        private readonly Dictionary<long, StockItem> items;
        private readonly ILogger<StockActorOrleans> _logger;
        private long partitionId;
        public StockActorOrleans(ILogger<StockActorOrleans> _logger)
        {
            items = new();
            this._logger = _logger;
            partitionId = this.GetPrimaryKeyLong();
        }
        public Task AddItem(StockItem item)
        {
            //Console.WriteLine($"Ingesting product with id {item.product_id} into the stock");
            try
            {
                //If product existed before, but has been deleted, else new product.
                if (this.items.ContainsKey(item.product_id) && this.items[item.product_id].active == false)
                {
                    //The product has been deleted and we are reactivating it with the new information
                    this.items[item.product_id] = item;
                }
                else
                {
                    //Product is new and we are inserting it in the database
                    this.items.Add(item.product_id, item);
                }
                this.items[item.product_id].created_at = DateTime.Now;
                return Task.CompletedTask;
            }
            catch (ArgumentNullException a)
            {
                //_logger.LogError("{firstMessage}",a.Message);
                return Task.CompletedTask;
            }
            catch (ArgumentException a)
            {
                //_logger.LogError("{firstMessage}",a.Message);
                return Task.CompletedTask;
            }
        }

        Func<(ItemStatus, ItemStatus)> out_to_in = () => (ItemStatus.OUT_OF_STOCK, ItemStatus.IN_STOCK);
        Func<(ItemStatus, ItemStatus)> in_to_in = () => (ItemStatus.IN_STOCK, ItemStatus.IN_STOCK);
        Func<(ItemStatus, ItemStatus)> out_to_out = () => (ItemStatus.OUT_OF_STOCK, ItemStatus.OUT_OF_STOCK);

        public Task<ItemStatus> AttemptReservation(AttemptReservationParameter arp)
        {
            var quantity = arp.quantity;
            var productId = arp.productId;

           //  Console.WriteLine("Stock actor {0} -- Attempting a reservation for product id {1}", this.partitionId, productId);

            if (!this.items.ContainsKey(productId))
            {
                //_logger.LogWarning("Stock grain {0} -- product with id {0} does not exist.",this.partitionId, productId);
                return  Task.FromResult(ItemStatus.OUT_OF_STOCK);
            }
            if (!this.items[productId].active)
            {
                //_logger.LogWarning("Stock grain {0} -- product with id {0} does not exist.", productId);
                return Task.FromResult(ItemStatus.DELETED);
            }
            if (this.items[productId].qty_available - this.items[productId].qty_reserved >= quantity)
            {
                this.items[productId].qty_reserved += quantity;
                this.items[productId].updated_at = DateTime.Now;
                // _logger.LogInformation("Stock grain {0} -- Succefully reserved product id {1}", this.partitionId, productId);
                return Task.FromResult(ItemStatus.IN_STOCK);
            }
            //_logger.LogWarning("Stock grain {0} -- The product with id {1} is out of stock.",this.partitionId, productId);
            return Task.FromResult(ItemStatus.OUT_OF_STOCK);
        }

        public Task ReviveItem(DeleteItemParameter dip)
        {
            var productId = dip.productId;
            this.items[productId].updated_at = DateTime.Now;
            this.items[productId].active = true;
            return Task.CompletedTask;
        }

        public Task CancelReservation(CancelReservationParameter crp)
        {
            var productId = crp.productId;
            var quantity = crp.quantity;
            // return item to stock
            this.items[productId].qty_reserved -= quantity;
            this.items[productId].updated_at = DateTime.Now;
            // _logger.LogInformation($"Stock grain {this.partitionId} -- successfully cancelled the reservation for product: {productId}");
            return Task.CompletedTask;
        }

        public Task ConfirmOrder(ConfirmOrderParameter cop)
        {
            //Console.WriteLine($"Confirming Order for productid: {cop.productId}.");
            var productId = cop.productId;
            // increase order count
            this.items[productId].order_count += 1;
            this.items[productId].updated_at = DateTime.Now;
            return Task.CompletedTask;
        }

        public Task ConfirmReservation(ConfirmReservationParameter crp)
        {
            var quantity = crp.quantity;
            var productId = crp.productId;
            // deduct from stock
            this.items[productId].qty_available -= quantity;
            this.items[productId].qty_reserved -= quantity;
            this.items[productId].updated_at = DateTime.Now;
            //Console.WriteLine($"Stock grain {this.partitionId} -- successfully confirming the reservation for product: {productId}");
            return Task.CompletedTask;
        }

        public Task DeleteItem(DeleteItemParameter dip)
        {
            var productId = dip.productId;
            this.items[productId].updated_at = DateTime.Now;
            this.items[productId].active = false;
            return Task.CompletedTask;
        }

        public Task<(ItemStatus, ItemStatus)> IncreaseStock(IncreaseStockParameter isp)
        {
            var productId = isp.productId;
            var quantity = isp.quantity;
            try
            {
                this.items[productId].qty_available += quantity;
                this.items[productId].updated_at = DateTime.Now;
                if (this.items[productId].qty_available == quantity)
                {
                    return Task.FromResult(out_to_in.Invoke());
                }

                return Task.FromResult(in_to_in.Invoke());
            }
            catch (KeyNotFoundException kex)
            {
                // _logger.LogInformation($"Stock grain {this.partitionId} -- " +
                //     $"Trying to increase quantity for a non existing item, reffering to product with id {productId}");
                return Task.FromResult(out_to_out.Invoke());
            }
        }
    }
}
