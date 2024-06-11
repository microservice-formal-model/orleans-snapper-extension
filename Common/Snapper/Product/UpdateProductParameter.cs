using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Snapper.Product
{
    [GenerateSerializer]
    public class UpdateProductParameter : IEquatable<UpdateProductParameter>
    {
        [Id(0)]
        public Entity.Product Product { get; set; }
        [Id(1)]
        public int Quantity { get; set; }

        public bool Equals(UpdateProductParameter other)
        {
            return Product.Equals(other.Product) &&
                Quantity == other.Quantity;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (obj is not UpdateProductParameter other) return false;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Product, Quantity);
        }
    }
}
