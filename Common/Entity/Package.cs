using Orleans;
using System;
namespace Common.Entity
{
    [GenerateSerializer]
    public class Package
	{
        // PK
        [Id(0)]
        public long shipment_id;
        [Id(1)]
        public int package_id;

        // FK
        // product identification
        [Id(2)]
        public long seller_id;
        [Id(3)]
        public long product_id;
        [Id(4)]
        public decimal freight_value;

        // date the shipment has actually been performed
        [Id(5)]
        public DateTime shipping_date;

        // delivery date
        [Id(6)]
        public DateTime delivery_date;

        // public long estimated_delivery_date;

        // delivery to carrier date
        // seller must deliver to carrier
        // public long delivered_carrier_date;

        [Id(7)]
        public int quantity;

        [Id(8)]
        public string status { get; set; }
    }
}

