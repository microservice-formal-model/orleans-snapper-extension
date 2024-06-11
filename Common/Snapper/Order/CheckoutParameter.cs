using Marketplace.Grains.Message;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Snapper.Order
{
    [GenerateSerializer]
    public class CheckoutParameter : IEquatable<CheckoutParameter>
    {
        [Id(0)]
        public long OrderId { get; set; }
        [Id(1)]
        public Checkout Checkout { get; set; }

        public CheckoutParameter() { }
        public CheckoutParameter(long orderId, Checkout checkout)
        {
            OrderId = orderId;
            Checkout = checkout;
        }

        public bool Equals(CheckoutParameter other)
        {
            return OrderId == other.OrderId &&
                Checkout.Equals(other.Checkout);
        }

        public override bool Equals(object obj)
        {
            if (obj is not CheckoutParameter other) return false;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(OrderId, Checkout);
        }

        public CheckoutParameter TransactionCopy()
        {
            return new CheckoutParameter(OrderId, Checkout.TransactionCopy());
        }
    }
}
