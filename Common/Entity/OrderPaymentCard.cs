using Orleans;
using System;
namespace Common.Entity
{
    [GenerateSerializer]
    public class OrderPaymentCard
	{
        // FKs
        [Id(0)]
        public long order_id { get; set; }
        [Id(1)]
        public int payment_sequential { get; set; }

        // card info coming from customer checkout
        [Id(2)]
        public string card_number { get; set; }

        [Id(3)]
        public string card_holder_name { get; set; }

        [Id(4)]
        public string card_expiration { get; set; }

        // public string card_security_number { get; set; }

        [Id(5)]
        public string card_brand { get; set; }
    }
}

