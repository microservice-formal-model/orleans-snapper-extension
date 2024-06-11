using Common.Entity;
using Common.Snapper.Product;
using Common.Snapper.Stock;
using Marketplace.Grains.Common;
using Marketplace.Grains.Orleans.ActorInterfaces;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;

namespace Marketplace.Grains.Orleans.Actors
{
    [Reentrant]
    public class ProductActorOrleans : Grain, IProductActorOrleans
    {
        private readonly Dictionary<long, Product> products;
        //private long partitionId;
        private readonly ILogger<ProductActorOrleans> _logger;
        private long nStockPartitions;
        private long partitionId;

        public ProductActorOrleans(ILogger<ProductActorOrleans> _logger)
        {
            this.products = new Dictionary<long, Product>();
            this._logger = _logger;
            this.partitionId = this.GetPrimaryKeyLong();
        }

        public async Task Init()
        {
            var mgmt = GrainFactory.GetGrain<IMetadataGrain>(0);
            var dict = await mgmt.GetActorSettings(new List<string>() { "StockActor", "OrderActor", "PaymentActor" });
            this.nStockPartitions = dict["StockActor"];
        }
        public Task<bool> AddProduct(Product product)
        {
            //Console.WriteLine($"Ingesting product with id {product.product_id} into the product actor");
            return Task.FromResult(this.products.TryAdd(product.product_id, product));
        }

        public async Task UpdateProduct(UpdateProductParameter upp)
        {
            var newProd = upp.Product;
            var newQuant = upp.Quantity;

            //_logger.LogInformation($"product grain {partitionId} -- starting to update product with id {newProd.product_id}");

            var stockPartition = newProd.product_id % nStockPartitions;
            var stockGrain = GrainFactory.GetGrain<IStockActorOrleans>(stockPartition, "Marketplace.Grains.Orleans.Actors.StockActorOrleans");
            //Checking if the product exists
            if (this.products.ContainsKey(newProd.product_id))
            {
                // _logger.LogInformation($"product grain {partitionId} -- product {newProd.product_id} exists in the products database");
                var oldActive = this.products[newProd.product_id].active;
                this.products[newProd.product_id] = newProd;
                //Checking if we need to change the active status
                if (oldActive != newProd.active)
                {
                    if (!newProd.active)
                    {
                        await stockGrain.DeleteItem(new DeleteItemParameter(newProd.product_id));
                    }
                    else
                    {
                        //If the stock actor used to be deleted, but is now active, then we need to revive the 
                        await stockGrain.ReviveItem(new DeleteItemParameter(newProd.product_id));
                    }
                }

                // If we want to change the quantity
                if (newQuant != 0)
                {
                    await stockGrain.IncreaseStock(new IncreaseStockParameter(newProd.product_id, newQuant));
                }

                await Task.CompletedTask;
            }
        }
    }
}
