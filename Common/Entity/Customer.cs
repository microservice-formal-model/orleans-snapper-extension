using Orleans;
using System;
namespace Common.Entity
{
    /**
     * 
     */
    [GenerateSerializer]
    public class Customer
    {
        // olist data set
        [Id(0)]
        public long id { get; set; }

        // added
        [Id(1)]
        public string first_name { get; set; }
        [Id(2)]
        public string last_name { get; set; }
        [Id(3)]
        public string address { get; set; }
        [Id(4)]
        public string complement { get; set; }
        [Id(5)]
        public string birth_date { get; set; }
        [Id(6)]
        // olist data set
        public string zip_code_prefix { get; set; }
        [Id(7)]
        public string city { get; set; }
        [Id(8)]
        public string state { get; set; }
        [Id(9)]
        // card
        public string card_number { get; set; }
        [Id(10)]
        public string card_security_number { get; set; }
        [Id(11)]
        public string card_expiration { get; set; }
        [Id(12)]
        public string card_holder_name { get; set; }
        [Id(13)]
        public string card_type { get; set; }
        [Id(14)]
        // statistics
        public int success_payment_count { get; set; }
        [Id(15)]
        public int failed_payment_count { get; set; }
        [Id(16)]
        public int pending_deliveries_count { get; set; }
        [Id(17)]
        public int delivery_count { get; set; }
        [Id(18)]
        public int abandoned_cart_count { get; set; }
        [Id(19)]
        public decimal total_spent_items { get; set; }
        [Id(20)]
        public decimal total_spent_freights { get; set; }
        [Id(21)]
        // additional
        public string data { get; set; }

    }
}

