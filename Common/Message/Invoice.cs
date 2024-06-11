using System;
using System.Collections.Generic;
using Common.Entity;
using Orleans;

namespace Marketplace.Grains.Message
{
	/*
     * "An invoice acts as a request for payment for the delivery of goods or services."
	 * Source: https://invoice.2go.com/learn/invoices/invoice-vs-purchase-order/
	 * An invoice data structure contains all necessary info for the payment 
	 * actor to process a payment
	 */
	[GenerateSerializer]
    public record Invoice
	{
        [Id(0)]
        public readonly CustomerCheckout customer;
        [Id(1)]
        public readonly Order order;

        // items
        [Id(2)]
        public readonly IList<OrderItem> items;

		// inconsistencies, if any
		// public List<ProductCheck> inconsistencies;

		public Invoice(CustomerCheckout customerCheckout, Order order, IList<OrderItem> items)
		{
			this.customer = customerCheckout;
			this.order = order;
			this.items = items;
		}
	}
}

