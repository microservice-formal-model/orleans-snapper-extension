using Common.Entity;
using Common.Snapper.Stock;
using ExtendedSnapperLibrary;
using ExtendedSnapperLibrary.Actor;
using Marketplace.Interface.Extended;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;

namespace Marketplace.Grains.Extended
{
    [Reentrant]
    public class StockActorExt : ExtendedTransactionActor, IStockActorExt
    {
        private readonly Dictionary<long, StockItem> items;
        private readonly ILogger<StockActorExt> _logger;
        private long partitionId;

        public StockActorExt(ILogger<StockActorExt> _logger)
        {
            this.items = new();
            this._logger = _logger;
        }

        public Task Init()
        {
            this.partitionId = this.GetPrimaryKeyLong();
            return Task.CompletedTask;
        }

        public Task<object> DeleteItem(TransactionContext tctx, object input)
        {
            var productId = ((DeleteItemParameter)input).productId;
            this.items[productId].updated_at = DateTime.Now;
            this.items[productId].active = false;
            return Task.FromResult((object)"success");
        }

        public Task<object> ReviveItem(TransactionContext tctx, object input)
        {
            var productId = ((DeleteItemParameter)input).productId;
            this.items[productId].updated_at = DateTime.Now;
            this.items[productId].active = true;
            return Task.FromResult((object)"success");
        }

        // called by order actor only
        public Task<object> AttemptReservation(TransactionContext tctx, object input)
        {
            var attemptResParam = (AttemptReservationParameter)input;
            var quantity = attemptResParam.quantity;
            var productId = attemptResParam.productId;

           //Console.WriteLine("Stock actor {0} -- Attempting a reservation for product id {1}", this.partitionId, productId);

            if (!this.items.ContainsKey(productId))
            {
                //_logger.LogWarning("Stock grain {0} -- product with id {0} does not exist.",this.partitionId, productId);
                return Task.FromResult(Enum.ToObject(ItemStatus.OUT_OF_STOCK.GetType(), ItemStatus.OUT_OF_STOCK));
            }
            if (!this.items[productId].active)
            {
                //_logger.LogWarning("Stock grain {0} -- product with id {0} does not exist.", productId);
                return Task.FromResult(Enum.ToObject(ItemStatus.DELETED.GetType(), ItemStatus.DELETED));
            }
            if (this.items[productId].qty_available - this.items[productId].qty_reserved >= quantity)
            {
                //This part is set to 0 to still perform the operations but for the benchmark not to affect 
                //the quantity of the product
                this.items[productId].qty_reserved += 0;
                this.items[productId].updated_at = DateTime.Now;
                // _logger.LogInformation("Stock grain {0} -- Succefully reserved product id {1}", this.partitionId, productId);
                return Task.FromResult(Enum.ToObject(ItemStatus.IN_STOCK.GetType(), ItemStatus.IN_STOCK));
            }
            //_logger.LogWarning("Stock grain {0} -- The product with id {1} is out of stock.",this.partitionId, productId);
            return Task.FromResult(Enum.ToObject(ItemStatus.OUT_OF_STOCK.GetType(), ItemStatus.OUT_OF_STOCK));
        }

        // called by order actor only
        // deduct from stock reservation
        public Task<object> ConfirmReservation(TransactionContext tctx, object input)
        {
            var confirmResParam = (ConfirmReservationParameter)input;
            var quantity = confirmResParam.quantity;
            var productId = confirmResParam.productId;
            // deduct from stock
            //This part is set to 0 to still perform the operations but for the benchmark not to affect 
            //the quantity of the product
            this.items[productId].qty_available -= 0;
            this.items[productId].qty_reserved -= 0;
            this.items[productId].updated_at = DateTime.Now;
            //Console.WriteLine($"Stock grain {this.partitionId} -- successfully confirming the reservation for product: {productId}");
            return Task.FromResult((object)"success");
        }

        // called by payment and order actors only
        public Task<object> CancelReservation(TransactionContext tctx, object input)
        {
            var cancelReservation = (CancelReservationParameter)input;
            var productId = cancelReservation.productId;
            var quantity = cancelReservation.quantity;
            // return item to stock
            //This part is set to 0 to still perform the operations but for the benchmark not to affect 
            //the quantity of the product
            this.items[productId].qty_reserved -= 0;
            this.items[productId].updated_at = DateTime.Now;
            // _logger.LogInformation($"Stock grain {this.partitionId} -- successfully cancelled the reservation for product: {productId}");
            return Task.FromResult((object)"success");
        }

        // called by payment actor only.
        // deduct from stock available
        public Task<object> ConfirmOrder(TransactionContext tctx, object input)
        {
            var confirmOrderParam = (ConfirmOrderParameter)input;
            var productId = confirmOrderParam.productId;
            //Console.WriteLine($"Confirming Order for productid: {productId}.");
            this.items[productId].order_count += 1;
            this.items[productId].updated_at = DateTime.Now;
            return Task.FromResult((object)"success");
        }

        public Task<object> AddItem(StockItem item)
        {
            //Console.WriteLine($"Ingesting product with id {item.product_id} into the stock, partition: {this.partitionId}");
            //this._logger.LogWarning("Stock part {0}, adding product ID {1}", this.partitionId, item.product_id);
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
                return Task.FromResult((object)"success");

            }
            catch (ArgumentNullException a)
            {
                //Console.WriteLine(a.Message);
                return Task.FromResult((object)"fail");
            }
            catch (ArgumentException a)
            {
               // Console.WriteLine($"{a.Message}");
                return Task.FromResult((object)"fail");
            }
        }

        Func<(ItemStatus, ItemStatus)> out_to_in = () => (ItemStatus.OUT_OF_STOCK, ItemStatus.IN_STOCK);
        Func<(ItemStatus, ItemStatus)> in_to_in = () => (ItemStatus.IN_STOCK, ItemStatus.IN_STOCK);
        Func<(ItemStatus, ItemStatus)> out_to_out = () => (ItemStatus.OUT_OF_STOCK, ItemStatus.OUT_OF_STOCK);
        // Func<(ItemStatus, ItemStatus)> del = () => (ItemStatus.DELETED, ItemStatus.DELETED);

        /**
         * Returns a derived transition
         */
        public Task<object> IncreaseStock(TransactionContext tctx, object input)
        {
            var increaseStockParam = (IncreaseStockParameter)input;
            var productId = increaseStockParam.productId;
            var quantity = increaseStockParam.quantity;
            try
            {
                //For experiment purposes the quantity is never changed
                this.items[productId].qty_available += 0;
                this.items[productId].updated_at = DateTime.Now;
                if (this.items[productId].qty_available == quantity)
                {
                    return Task.FromResult((object)out_to_in.Invoke());
                }

                return Task.FromResult((object)in_to_in.Invoke());
            }
            catch (KeyNotFoundException kex)
            {
                // _logger.LogInformation($"Stock grain {this.partitionId} -- " +
                //     $"Trying to increase quantity for a non existing item, reffering to product with id {productId}");
                return Task.FromResult((object)out_to_out.Invoke());
            }
        }

        public Task<StockItem> GetItem(TransactionContext tctx, long itemId)
        {
            return Task.FromResult(items[itemId]);
        }

        public Task<object> Noop(TransactionContext tctx, object input)
        {
           // _logger.LogInformation("Stock grain -- Performing Noop");
            return Task.FromResult((object)"success");
        }
    }
}
