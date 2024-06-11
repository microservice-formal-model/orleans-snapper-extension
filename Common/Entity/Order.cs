using Orleans;
using System;
namespace Common.Entity
{
    /*
     * Order is based on two sources:
     * (i) Olist data set (kaggle)
     * (ii) Olist developer API: https://dev.olist.com/docs/retrieving-order-informations
     * The total attribute is also added to sum the value of all products in the order.
     */
    [GenerateSerializer]
    public class Order
    {
        // PK
        [Id(0)]
        public long id { get; set; }

        // FK
        [Id(1)]
        public long customer_id { get; set; }
        [Id(2)]
        public string status { get; set; }
        [Id(3)]
        public DateTime purchase_timestamp { get; set; }

        // public string approved_at { get; set; }
        // added
        [Id(4)]
        public DateTime payment_date { get; set; }
        [Id(5)]
        public DateTime delivered_carrier_date { get; set; }
        [Id(6)]
        public DateTime delivered_customer_date { get; set; }
        [Id(7)]
        public DateTime estimated_delivery_date { get; set; }

        // dev
        [Id(8)]
        public int count_items { get; set; }
        [Id(9)]
        public DateTime created_at { get; set; }
        [Id(10)]
        public DateTime updated_at { get; set; }
        [Id(11)]
        public decimal total_amount { get; set; }
        [Id(12)]
        public decimal total_freight { get; set; }
        [Id(13)]
        public decimal total_incentive { get; set; }
        [Id(14)]
        public decimal total_invoice { get; set; }
        [Id(15)]
        public decimal total_items { get; set; }
        [Id(16)]
        public string data { get; set; }

        public Order()
        {
            //this.status = OrderStatus.CREATED.ToString();
        }

    }
}

