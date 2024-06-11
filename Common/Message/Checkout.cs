using System;
using Common.Entity;
using System.Collections.Generic;
using Orleans;
using System.Linq;

namespace Marketplace.Grains.Message
{
    /**
	 * Assemble all data necessary for processing a checkout
	 * Data structure delivered to order actor to proceed with order processing
	 */
    [GenerateSerializer]
    public class Checkout : IEquatable<Checkout>
	{
		[Id(0)]
        public DateTime CreatedAt { get; set; }
		[Id(1)]
        public CustomerCheckout CustomerCheckout { get; set; }
		[Id(2)]
        public List<CartItem> Items { get; set; }

        public bool Equals(Checkout other)
        {
            return CreatedAt.Equals(other.CreatedAt) && 
                CustomerCheckout.Equals(other.CustomerCheckout) && 
                //Equality check of lists respects order of items, which should be fine
                //given that the order they are stated in the csv files is deterministic and
                //the deserialization also follows this order, in comparison a set equality check
                //would be slightly slower
                Items.SequenceEqual(other.Items);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            foreach(CartItem i in Items)
            {
                hash.Add(i);
            }
            hash.Add(CreatedAt);
            hash.Add(CustomerCheckout);
            return hash.ToHashCode();
        }

        /// <summary>
        /// All items here are only read and never manipulated. We keep them as memory references.
        /// <c>CreatedAt</c> is deep copied.
        /// </summary>
        /// <returns></returns>
        public Checkout TransactionCopy() => new()
        {
            CreatedAt = CreatedAt,
            CustomerCheckout = CustomerCheckout,
            Items = Items
        };
    }
}

