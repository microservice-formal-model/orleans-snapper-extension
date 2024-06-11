using Common.Entity;
using Common.Snapper.Product;
using Common.Snapper.Stock;
using Marketplace.Grains.Common;
using Marketplace.Grains.TransactionalOrleans.ActorInterfaces;
using Orleans.Transactions.Abstractions;

namespace Marketplace.Grains.TransactionalOrleans.Actors
{
    public class ProductActorTransOrl : Grain, IProductActorTransOrl
    {
        private readonly ITransactionalState<Dictionary<long, Product>> _products;
        private long nStockPartitions;

        public ProductActorTransOrl(
            [TransactionalState(nameof(products))]
            ITransactionalState<Dictionary<long, Product>> products)
        {
            _products = products;
        }

        public async Task<bool> AddProduct(Product product)
        {
            bool res = false;
            await _products.PerformUpdate(products => res = products.TryAdd(product.product_id, product));
            return res;
        }

        public async Task Init()
        {
            var mgmt = GrainFactory.GetGrain<IMetadataGrain>(0);
            var dict = await mgmt.GetActorSettings(new List<string>() { "StockActor", "OrderActor", "PaymentActor" });
            this.nStockPartitions = dict["StockActor"];
        }

        public async Task UpdateProduct(UpdateProductParameter upp)
        {
            var newProd = upp.Product;
            var newQuant = upp.Quantity;

            Console.WriteLine($"Starting to update product with id {newProd.product_id}");
            var stockPartition = newProd.product_id % nStockPartitions;
            var stockGrain = GrainFactory.GetGrain<IStockActorTransOrl>(stockPartition);
            //Checking if the product exists
            if (await _products.PerformUpdate(p => p.ContainsKey(newProd.product_id)))
            {
                // _logger.LogInformation($"product grain {partitionId} -- product {newProd.product_id} exists in the products database");
                bool oldActive = false;
                await _products.PerformUpdate(p =>
                {
                    oldActive = p[newProd.product_id].active;
                    p[newProd.product_id] = newProd;
                });
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
            }
        }
    }
}
