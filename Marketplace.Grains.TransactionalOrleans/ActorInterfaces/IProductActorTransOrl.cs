using Common.Entity;
using Common.Snapper.Product;

namespace Marketplace.Grains.TransactionalOrleans.ActorInterfaces
{
    public interface IProductActorTransOrl : IGrainWithIntegerKey
    {
        [Transaction(TransactionOption.Create)]
        public Task UpdateProduct(UpdateProductParameter upp);
        [Transaction(TransactionOption.Create)]
        public Task<bool> AddProduct(Product product);
        [Transaction(TransactionOption.NotAllowed)]
        public Task Init();
    }
}
