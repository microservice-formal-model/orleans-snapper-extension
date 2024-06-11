using Orleans;
using System;
namespace Common.Entity
{
    [GenerateSerializer]
    public class Shipment
	{
        // PK
        [Id(0)]
        public long id;

        // FK
        [Id(1)]
        public long order_id;
        [Id(2)]
        public long customer_id;

        // materialized values from packages
        [Id(3)]
        public int package_count;
        [Id(4)]
        public decimal total_freight_value;

        // date all deliveries were requested
        [Id(5)]
        public DateTime request_date;

        // shipment status
        [Id(6)]
        public string status;

        // customer delivery address. the same for all packages/sellers
        [Id(7)]
        public string first_name { get; set; }

        [Id(8)]
        public string last_name { get; set; }

        [Id(9)]
        public string street { get; set; }

        [Id(10)]
        public string complement { get; set; }

        [Id(11)]
        public string zip_code_prefix { get; set; }

        [Id(12)]
        public string city { get; set; }

        [Id(13)]
        public string state { get; set; }
    }
}

