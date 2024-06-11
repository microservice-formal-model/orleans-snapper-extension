using Orleans;
using System;
namespace Common.Entity
{
    [GenerateSerializer]
    public class StockItem
	{
        [Id(0)]
        public long product_id { get; set; }

        [Id(1)]
        public long seller_id { get; set; }

        [Id(2)]
        public int qty_available { get; set; }

        [Id(3)]
        public int qty_reserved { get; set; }

        [Id(4)]
        public int order_count { get; set; }

        [Id(5)]
        public int ytd { get; set; }

        [Id(6)]
        public DateTime created_at { get; set; }
        [Id(7)]
        public DateTime updated_at { get; set; }

        [Id(8)]
        public bool active { get; set; } = true;

        [Id(9)]
        public string data { get; set; }
    }
}

