using Common.Entity;
using Common.Snapper.Product;
using Common.Snapper.Stock;
using ExtendedSnapperLibrary;
using ExtendedSnapperLibrary.Actor;
using ExtendedSnapperLibrary.ActorInterface;
using Marketplace.Grains.Common;
using Marketplace.Interface.Extended;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;

namespace Marketplace.Grains.Extended
{
    [Reentrant]
    public class ProductActorExt : ExtendedTransactionActor, IProductActorExt
    {
        private readonly Dictionary<long, Product> products;
        private long partitionId;
        private readonly ILogger<ProductActorExt> _logger;
        private long nStockPartitions;

        public ProductActorExt(ILogger<ProductActorExt> _logger)
        {
            this.products = new Dictionary<long, Product>();
            this._logger = _logger;
        }

        public Task<bool> AddProduct(Product product)
        {
           // Console.WriteLine($"Ingesting product with id {product.product_id} into the product actor");
           // this._logger.LogWarning("Product part {0}, adding product ID {1}", this.partitionId, product.product_id);
            return Task.FromResult(this.products.TryAdd(product.product_id, product));
        }

        public Task DeleteProduct(TransactionContext tctx, long productId)
        {
            //this._logger.LogWarning("Product part {0} delete product ({1}) operation", this.partitionId, productId);
            this.products[productId].active = false;
            this.products[productId].updated_at = DateTime.Now.ToLongDateString();
            //this._logger.LogWarning("Product part {0} finished delete product ({1}) operation", this.partitionId, productId);
            return Task.CompletedTask;
        }

        public Task<Product> GetProduct(long productId)
        {
           // this._logger.LogInformation("Product part {0}, returning product {1}", this.partitionId, productId);
            return Task.FromResult(this.products[productId]);
        }

        public Task<IList<Product>> GetProducts(TransactionContext tctx, long sellerId)
        {
           // this._logger.LogWarning("Product part {0}, returning products for seller {1}", this.partitionId, sellerId);
            return Task.FromResult((IList<Product>)this.products.Values.Select(q => q).Where(q => q.seller_id == sellerId).ToList());
        }

        public async Task Init()
        {
            this.partitionId = this.GetPrimaryKeyLong();
            var mgmt = GrainFactory.GetGrain<IMetadataGrain>(0);
            var dict = await mgmt.GetActorSettings(new List<string>() { "StockActor", "OrderActor", "PaymentActor" });
            this.nStockPartitions = dict["StockActor"];
        }

        public async Task<object> UpdateProduct(TransactionContext tctx, object input)
        {
            var uprodparam = (UpdateProductParameter)input;
            var newProd = uprodparam.Product;
            var newQuant = uprodparam.Quantity;

            //_logger.LogInformation($"product grain {partitionId} -- starting to update product with id {newProd.product_id}");

            var stockPartition = newProd.product_id % nStockPartitions;
            var stockGrain = GrainFactory.GetGrain<IExtendedTransactionActor>(stockPartition, "Marketplace.Grains.Extended.StockActorExt");
            //Checking if the product exists
            if (this.products.ContainsKey(newProd.product_id))
            {
                // _logger.LogInformation($"product grain {partitionId} -- product {newProd.product_id} exists in the products database");
                var oldActive = this.products[newProd.product_id].active;
                this.products[newProd.product_id] = newProd;
                //Checking if we need to change the active status
                if (oldActive != newProd.active)
                {
                    Console.WriteLine("This should not happen");
                    // _logger.LogInformation($"product grain {partitionId} -- active status of the product has changed, we need to activate the product");
                    //If the product is now not active anymore, we have to call the delete item method
                    //in the stock actor to indicate the same in the stock
                    if (!newProd.active)
                    {
                        await stockGrain.Execute(tctx, new FunctionCall("DeleteItem", new DeleteItemParameter(newProd.product_id), typeof(StockActorExt)));
                    }
                    else
                    {
                        //If the stock actor used to be deleted, but is now active, then we need to revive the 
                        await stockGrain.Execute(tctx, new FunctionCall("ReviveItem", new DeleteItemParameter(newProd.product_id), typeof(StockActorExt)));
                    }
                }
                else
                {
                    // _logger.LogInformation($"product grain {partitionId} -- active status of the product has not changed.");
                    await stockGrain.Execute(tctx, new FunctionCall("Noop", null, typeof(StockActorExt)));
                }

                // If we want to change the quantity
                if (newQuant != 0)
                {
                    await stockGrain.Execute(tctx, new FunctionCall("IncreaseStock", new IncreaseStockParameter(newProd.product_id, newQuant), typeof(StockActorExt)));
                }

                return (object)"success";
            }
            else
            // If the product does not exists
            {
                Console.WriteLine("This should not happen");
                //Perform the amount of Noops
                var one_or_two = () => { if (newQuant != 0) { return 2; } else { return 1; } };
                var i = 0;
                var bound = one_or_two.Invoke();
                while (i < bound)
                {
                    await stockGrain.Execute(tctx, new FunctionCall("Noop", null, typeof(StockActorExt)));
                    i++;
                }
                return (object)"fail";
            }
        }
    }
}
