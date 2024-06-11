using Common.Entity;
using Marketplace.Interfaces;
using Microsoft.Extensions.Logging;
using SnapperLibrary.Actor;
using SnapperLibrary;
using SnapperLibrary.ActorInterface;
using Marketplace.Grains.Message;
using Common.Snapper.Stock;
using Common.Snapper.Order;
using Marketplace.Grains.Common;
using Utilities;

namespace Marketplace.Grains
{

    public class PaymentActor : TransactionActor, IPaymentActor
	{
        private long paymentActorId;
        private int nStockPartitions;
        private int nOrderPartitions;

        private readonly Random random;

        // DB
        // key order_id
        private readonly Dictionary<long, List<OrderPayment>> payments;
        private readonly Dictionary<long, OrderPaymentCard> cardPayments;

        private readonly ILogger<PaymentActor> _logger;

        public PaymentActor(ILogger<PaymentActor> _logger, Helper helper) : base(helper)
		{
            this.random = new Random();
            this.payments = new();
            this.cardPayments = new();
            this._logger = _logger;
        }
        public async Task Init()
        {
            this.paymentActorId = this.GetPrimaryKeyLong();
            var mgmt = GrainFactory.GetGrain<IMetadataGrain>(0);
            var dict = await mgmt.GetActorSettings(new List<string>() { "StockActor", "OrderActor"});
            this.nStockPartitions = dict["StockActor"];
            this.nOrderPartitions = dict["OrderActor"];
            //_logger.LogWarning("Payment grain {0} activated: #stock grains {1} #order grains {2} ", this.paymentActorId, nStockPartitions, nOrderPartitions);
        }

        /**
         * simulate an external request by adding a random delay
         * customer checkout is necessary here to contact external service provider
         */
        public bool ContactESP(CustomerCheckout customer, decimal value)
        {
            //bool approved = true;
            //await Task.Delay(this.random.Next(100, 1001));

            // TODO pick from a distribution
            //if (this.random.Next(1, 11) > 7)
            //{
            //     approved = false;
            //    _logger.LogWarning("Payment grain {0}, order would have failed!", this.paymentActorId);
            //}

            return true;
        }

        public async Task<object> ProcessPayment(TransactionContext tctx, object input)
        {

            var invoice = (Invoice)input;
            //Console.WriteLine("Payment grain {0} -- Payment process starting for order {1}", this.paymentActorId, invoice.order.id);
            //_logger.LogInformation("Payment grain {0} -- Payment process starting for order {1}", this.paymentActorId, invoice.order.id);
            bool approved = true;
            List<Task> tasks = new(invoice.items.Count);
            //_logger.LogInformation($"Payment grain {this.paymentActorId} -" +
            //    $" The external payment check answered to the order payment request: approved: {approved}.");

            if (approved)
            {
                foreach (var item in invoice.items)
                {
                    long partition = (item.product_id % nStockPartitions);
                    var stockActor = GrainFactory.GetGrain<ITransactionActor>(partition,"Marketplace.Grains.StockActor");
                    tasks.Add(stockActor.Execute(tctx,
                        new FunctionCall(
                            "ConfirmOrder",
                            new ConfirmOrderParameter(item.product_id, item.quantity),
                           typeof(StockActor))));
                }
            }

            await Task.WhenAll(tasks);

            tasks.Clear();

            // call order, customer, and shipment
            var orderActor = GrainFactory.GetGrain<ITransactionActor>(invoice.order.id % nOrderPartitions,"Marketplace.Grains.OrderActor");
           if (approved)
           {
                //Console.WriteLine("Payment grain {0} -- Payment process succeeded for order {0}", this.paymentActorId, invoice.order.id);
                // _logger.LogInformation("Payment grain {0} -- Payment process succeeded for order {0}", this.paymentActorId, invoice.order.id);

                // ?? what is the status processing? should come before or after payment?
                // before is INVOICED, so can only come after. but shipment sets to shipped...
                // I think processing is when the seller must approve or not the order,
                // but here all orders are approved by default. so we dont use processing
                // notify
                tasks.Add(orderActor.Execute(
                    tctx
                    ,new FunctionCall(
                        "UpdateOrderStatus",
                        new UpdateOrderStatusParameter(invoice.order.id, OrderStatus.PAYMENT_PROCESSED),
                        typeof(OrderActor))));

                List<OrderPayment> paymentLines = new();
                int seq = 1;

                // create payment tuples
                if (invoice.customer.PaymentType.Equals( PaymentType.CREDIT_CARD.ToString() ) ||
                    invoice.customer.PaymentType.Equals(PaymentType.DEBIT_CARD.ToString()))
                { 
                    paymentLines.Add(new OrderPayment()
                    {
                        order_id = invoice.order.id,
                        payment_sequential = seq,
                        payment_type = invoice.customer.PaymentType,
                        payment_installments = invoice.customer.Installments,
                        payment_value = invoice.order.total_amount
                    });

                    // create an entity for credit card payment details with FK to order payment
                    OrderPaymentCard card = new()
                    {
                        order_id = invoice.order.id,
                        payment_sequential = seq,
                        card_number = invoice.customer.CardNumber,
                        card_holder_name = invoice.customer.CardHolderName,
                        card_expiration = invoice.customer.CardExpiration,
                        // I guess firms don't save this data in this table to avoid leaks...
                        // card_security_number = invoice.customer.CardSecurityNumber,
                        card_brand = invoice.customer.CardBrand
                    };

                    cardPayments.Add(invoice.order.id, card);
                }

                if (invoice.customer.PaymentType.Equals(PaymentType.BOLETO.ToString())){
                    paymentLines.Add(new OrderPayment()
                    {
                        order_id = invoice.order.id,
                        payment_sequential = seq,
                        payment_type = invoice.customer.PaymentType,
                        payment_installments = 1,
                        payment_value = invoice.order.total_amount
                    });
                }

                payments.Add(invoice.order.id, paymentLines);

            }
            else
            {
             //   this._logger.LogWarning("Payment grain {0} -- Payment process failed for order {0}", this.paymentActorId, invoice.order.id);
             //   an event approach would avoid the redundancy of contacting several actors to notify about the same fact
                tasks.Add(orderActor.Execute(tctx, new FunctionCall(
                    "UpdateOrderStatus", 
                    new UpdateOrderStatusParameter(invoice.order.id, OrderStatus.PAYMENT_FAILED),
                    typeof(OrderActor))));
            }

            await Task.WhenAll(tasks);

           // _logger.LogInformation("Payment grain {0} -- Payment process ended for order id {1}",this.paymentActorId,invoice.order.id);
            return new object();
        }


        public async Task ProcessFailedOrder(TransactionContext tctx, long customerId, long orderId)
        {
            var orderActor = GrainFactory.GetGrain<ITransactionActor>(orderId % nOrderPartitions);

            List<Task> tasks = new(4)
            {
                orderActor.Execute(tctx, new FunctionCall(
                "UpdateOrderStatus",
                new UpdateOrderStatusParameter(orderId, OrderStatus.PAYMENT_FAILED),
                typeof(OrderActor)))
            };

            await Task.WhenAll(tasks);
        }
    }
}

