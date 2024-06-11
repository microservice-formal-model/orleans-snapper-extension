using System;
using System.Runtime.CompilerServices;
using Orleans;

namespace Common.Entity
{
    /**
     * A sub-type of customer.
     * Ideally, address and credit card info may change across customer checkouts
     * Basket and Order does not need to know all internal data about customers
     */
    [GenerateSerializer]
    public class CustomerCheckout : IEquatable<CustomerCheckout>
    {
        [Id(0)]
        public  long CustomerId { get; set; }

        /**
         * Delivery address (could be different from customer's address)
         */
        [Id(1)]
        public string FirstName { get; set; }
        [Id(2)]
        public string LastName { get; set; }
        [Id(3)]
        public string Street { get; set; }
        [Id(4)]
        public string Complement { get; set; }
        [Id(5)]
        public string City { get; set; }
        [Id(6)]
        public string State { get; set; }
        [Id(7)]
        public string ZipCode { get; set; }

        /**
         * Payment type
         */
        [Id(8)]
        public string PaymentType { get; set; }

        /**
         * Credit or debit card
         */
        [Id(9)]
        public string CardNumber { get; set; }
        [Id(10)]
        public string CardHolderName { get; set; }
        [Id(11)]
        public string CardExpiration { get; set; }
        [Id(12)]
        public string CardSecurityNumber { get; set; }
        [Id(13)]
        public string CardBrand { get; set; }
        [Id(14)]
        // if no credit card, must be null
        public int Installments { get; set; }

        public bool Equals(CustomerCheckout other)
        {
            return CustomerId == other.CustomerId &&
                FirstName == other.FirstName &&
                LastName == other.LastName &&
                Street == other.Street &&
                Complement == other.Complement &&
                City == other.City &&
                State == other.State &&
                ZipCode == other.ZipCode &&
                PaymentType == other.PaymentType &&
                CardNumber == other.CardNumber &&
                CardHolderName == other.CardHolderName &&
                CardExpiration == other.CardExpiration &&
                CardSecurityNumber == other.CardSecurityNumber &&
                CardBrand == other.CardBrand &&
                Installments == other.Installments;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(CustomerId);
            hash.Add(FirstName);
            hash.Add(LastName);
            hash.Add(Street);
            hash.Add(City);
            hash.Add(Complement);
            hash.Add(State);
            hash.Add(ZipCode);
            hash.Add(PaymentType);
            hash.Add(CardNumber);
            hash.Add(CardHolderName);
            hash.Add(CardExpiration);
            hash.Add(CardSecurityNumber);
            hash.Add(CardBrand);
            hash.Add(Installments);
            return hash.ToHashCode();
        }
    }
}

