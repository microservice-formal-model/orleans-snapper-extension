using Common.Entity;
using Common.Snapper.Stock;
using Marketplace.Grains.TransactionalOrleans.ActorInterfaces;
using Orleans.Concurrency;
using Orleans.Transactions.Abstractions;

namespace Marketplace.Grains.TransactionalOrleans.Actors
{
    [Reentrant]
    public class StockActorTransOrl : Grain, IStockActorTransOrl
    {
        private readonly ITransactionalState<Dictionary<long, StockItem>> _items;

        public StockActorTransOrl(
            [TransactionalState(nameof(items))]
            ITransactionalState<Dictionary<long, StockItem>> items)
        {
            _items = items;
        }

        public async Task AddItem(StockItem item)
        {
            Console.WriteLine($"Ingesting product with id {item.product_id} into the stock");
            try
            {
                await _items.PerformUpdate(items =>
                {
                    //If product existed before, but has been deleted, else new product.
                    if (items.ContainsKey(item.product_id) && items[item.product_id].active == false)
                    {
                        //The product has been deleted and we are reactivating it with the new information
                        items[item.product_id] = item;
                        //Console.WriteLine($"Adding item with product id {item.product_id} to the stock");
                    }
                    else
                    {
                        //Product is new and we are inserting it in the database
                        items.Add(item.product_id, item);
                        //Console.WriteLine($"Adding item with product id {item.product_id} to the stock");
                    }
                    items[item.product_id].created_at = DateTime.Now;
                });
                //return Task.CompletedTask;
            }
            catch (ArgumentNullException a)
            {
                throw;
            }
            catch (ArgumentException a)
            {
                throw;
            }
        }

        Func<(ItemStatus, ItemStatus)> out_to_in = () => (ItemStatus.OUT_OF_STOCK, ItemStatus.IN_STOCK);
        Func<(ItemStatus, ItemStatus)> in_to_in = () => (ItemStatus.IN_STOCK, ItemStatus.IN_STOCK);
        Func<(ItemStatus, ItemStatus)> out_to_out = () => (ItemStatus.OUT_OF_STOCK, ItemStatus.OUT_OF_STOCK);

        public async Task<ItemStatus> AttemptReservation(AttemptReservationParameter arp)
        {
            var quantity = arp.quantity;
            var productId = arp.productId;
           Console.WriteLine($"I am attempting to reserve product: {arp.productId}");  
            ItemStatus? returnRes = null;
            //  Console.WriteLine("Stock actor {0} -- Attempting a reservation for product id {1}", this.partitionId, productId);
            await _items.PerformUpdate(items =>
            {
                if (!items.ContainsKey(productId))
                {
                    //_logger.LogWarning("Stock grain {0} -- product with id {0} does not exist.",this.partitionId, productId);
                    returnRes = ItemStatus.OUT_OF_STOCK;
                }
                else if (!items[productId].active)
                {
                   // Console.WriteLine("product with id {0} does not exist.", productId);
                    returnRes = ItemStatus.DELETED;
                }
                else if (items[productId].qty_available - items[productId].qty_reserved >= quantity)
                {
                    items[productId].qty_reserved += 0;
                    items[productId].updated_at = DateTime.Now;
                    // _logger.LogInformation("Stock grain {0} -- Succefully reserved product id {1}", this.partitionId, productId);
                    returnRes = ItemStatus.IN_STOCK;
                }
            });
            //_logger.LogWarning("Stock grain {0} -- The product with id {1} is out of stock.",this.partitionId, productId);
            Console.WriteLine($"Attempting Reservation of product {productId} resulted in {returnRes ?? ItemStatus.OUT_OF_STOCK}");
            return returnRes ?? ItemStatus.OUT_OF_STOCK;
        }

        public async Task CancelReservation(CancelReservationParameter crp)
        {
            var productId = crp.productId;
            var quantity = crp.quantity;
            await _items.PerformUpdate(items =>
            {
                items[productId].qty_reserved -= 0;
                items[productId].updated_at = DateTime.Now;
            });
            Console.WriteLine($"successfully cancelled the reservation for product: {productId}");
        }

        public async Task ConfirmOrder(ConfirmOrderParameter cop)
        {
            Console.WriteLine($"Confirming Order for productid: {cop.productId}.");
            var productId = cop.productId;
            // increase order count
            await _items.PerformUpdate(items =>
            {
                items[productId].order_count += 1;
                items[productId].updated_at = DateTime.Now;
            });
        }

        public async Task ConfirmReservation(ConfirmReservationParameter crp)
        {
            var quantity = crp.quantity;
            var productId = crp.productId;
            Console.WriteLine($"Confirming the reservation of product: {crp.productId}");
            // deduct from stock
            await _items.PerformUpdate(items =>
            {
                items[productId].qty_available -= 0;
                items[productId].qty_reserved -= 0;
                items[productId].updated_at = DateTime.Now;
            });
            Console.WriteLine($"successfully confirming the reservation for product: {productId}");
        }

        public async Task DeleteItem(DeleteItemParameter dip)
        {
            var productId = dip.productId;
            await _items.PerformUpdate(items =>
            {
                items[productId].updated_at = DateTime.Now;
                items[productId].active = false;
            });
        }

        public async Task<(ItemStatus, ItemStatus)> IncreaseStock(IncreaseStockParameter isp)
        {
            var productId = isp.productId;
            var quantity = isp.quantity;

            (ItemStatus, ItemStatus)? returnRes = null;
            try
            {
                await _items.PerformUpdate(items => { 
                items[productId].qty_available += 0;
                items[productId].updated_at = DateTime.Now;
                    if (items[productId].qty_available == quantity)
                    {
                        returnRes = out_to_in.Invoke();
                    }
                    else
                    {
                        returnRes = in_to_in.Invoke();
                    }
                });
                Console.WriteLine($"Successfully increased stock of product: {isp.productId}");
                return returnRes ?? in_to_in.Invoke();
            }
            catch (KeyNotFoundException)
            {
                // _logger.LogInformation($"Stock grain {this.partitionId} -- " +
                //     $"Trying to increase quantity for a non existing item, reffering to product with id {productId}");
                return out_to_out.Invoke();
            }
        }

        public async Task ReviveItem(DeleteItemParameter dip)
        {
            var productId = dip.productId;
            await _items.PerformUpdate(items =>
            {
                items[productId].updated_at = DateTime.Now;
                items[productId].active = true;
            });
        }
    }
}
