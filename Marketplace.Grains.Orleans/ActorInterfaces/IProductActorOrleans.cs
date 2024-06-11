using Common.Entity;
using Common.Snapper.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marketplace.Grains.Orleans.ActorInterfaces
{
    public interface IProductActorOrleans : IGrainWithIntegerKey
    { 
        // seller worker calls it
        public Task UpdateProduct(UpdateProductParameter upp);

        public Task<bool> AddProduct(Product product);

        public Task Init();
    }
}
