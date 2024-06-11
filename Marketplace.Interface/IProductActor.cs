using System;
using Orleans;
using System.Threading.Tasks;
using System.Collections.Generic;
using SnapperLibrary;
using Common.Entity;

namespace Marketplace.Interfaces
{

    public interface IProductActor : IGrainWithIntegerKey
    {

        public Task<Product> GetProduct(long productId);

        public Task<IList<Product>> GetProducts(TransactionContext tctx, long sellerId);

        // public Task<Product> GetProductWithFreightValue(long productId, string zipCode);

        public Task DeleteProduct(TransactionContext tctx, long productId);

        // seller worker calls it
        public Task<object> UpdateProduct(TransactionContext tctx, object input);

        public Task<bool> AddProduct(Product product);

        public Task Init();
    }
    
}

