using Orleans;
using System;
namespace Common.Entity
{
    [GenerateSerializer]
    public class OrderItem
	{
        // Composite PK
        [Id(0)]
        public long order_id { get; set; }
        [Id(1)]
        public long order_item_id { get; set; }

        // FK
        [Id(2)]
        public long product_id { get; set; }

        // FK
        [Id(3)]
        public long seller_id { get; set; }

        [Id(4)]
        public decimal unit_price { get; set; }

        [Id(5)]
        public string shipping_limit_date { get; set; }

        [Id(6)]
        public decimal freight_value { get; set; }

        // not present in olist
        [Id(7)]
        public int quantity { get; set; }

        // without freight value
        [Id(8)]
        public decimal total_items { get; set; }

        // with freight value
        [Id(9)]
        public decimal total_amount { get; set; }
    }
}

