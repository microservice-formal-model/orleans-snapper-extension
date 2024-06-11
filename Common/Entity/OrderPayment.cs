using Orleans;
using System;
namespace Common.Entity
{
    [GenerateSerializer]
    public class OrderPayment
	{
        [Id(0)]
        public long order_id { get; set; }

        // 1 - coupon, 2 - coupon, 3 - credit card
        [Id(1)]
        public int payment_sequential { get; set; }

        // coupon, credit card
        [Id(2)]
        public string payment_type { get; set; }

        // number of times the credit card is charged (usually once a month)
        [Id(3)]
        public int payment_installments { get; set; }

        // respective to this line (ie. coupon)
        [Id(4)]
        public decimal payment_value { get; set; }
    }
}

