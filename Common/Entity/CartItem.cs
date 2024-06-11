using Orleans;
using System;
namespace Common.Entity
{
    /**
     * Entity not present in olist original data set
     * Thus, the basket item entity is derived from
     * the needs to process the order.
     * This could include the freight value...
     */
    [GenerateSerializer]
    public class CartItem : IEquatable<CartItem>
    {
        [Id(0)]
        public long ProductId { get; set; }
        [Id(1)]
        public long SellerId { get; set; }
        [Id(2)]
        public decimal UnitPrice { get; set; }
        [Id(3)]
        public decimal FreightValue { get; set; }
        [Id(4)]
        public int Quantity { get; set; }

        [Id(5)]
        public string ProductName { get; set; }

        public bool Equals(CartItem other)
        {
            return ProductId == other.ProductId &&
                SellerId == other.SellerId &&
                UnitPrice == other.UnitPrice &&
                FreightValue == other.FreightValue &&
                Quantity == other.Quantity &&
                ProductName == other.ProductName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ProductId, SellerId, UnitPrice, FreightValue, Quantity, ProductName);
        }
    }
}

