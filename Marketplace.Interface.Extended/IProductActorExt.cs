using Common.Entity;
using ExtendedSnapperLibrary;
using ExtendedSnapperLibrary.ActorInterface;

namespace Marketplace.Interface.Extended
{
    public interface IProductActorExt : IGrainWithIntegerKey, IExtendedTransactionActor
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
