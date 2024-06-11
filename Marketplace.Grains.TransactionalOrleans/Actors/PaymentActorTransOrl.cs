using Common.Entity;
using Common.Snapper.Order;
using Common.Snapper.Stock;
using Marketplace.Grains.Common;
using Marketplace.Grains.Message;
using Marketplace.Grains.TransactionalOrleans.ActorInterfaces;
using Orleans;
using Orleans.Concurrency;
using Orleans.Transactions.Abstractions;
using static Marketplace.Grains.TransactionalOrleans.Actors.PaymentActorTransOrl;

namespace Marketplace.Grains.TransactionalOrleans.Actors
{
    [Reentrant]
    public class PaymentActorTransOrl : Grain, IPaymentActorTransOrl
    {
        private int nStockPartitions;
        private int nOrderPartitions;
        [GenerateSerializer]
        public class Payments
        {
            [Id(0)]
            public readonly Dictionary<long, List<OrderPayment>> payments;
            [Id(1)]
            public readonly Dictionary<long, OrderPaymentCard> cardPayments;

            public Payments()
            {
                payments = new();
                cardPayments = new();
            }
        }
        ITransactionalState<Payments> _payments;

        public PaymentActorTransOrl(
            [TransactionalState(nameof(payments))]
            ITransactionalState<Payments> payments)
        {
            _payments = payments;
        }
        public async Task Init()
        {
            var mgmt = GrainFactory.GetGrain<IMetadataGrain>(0);
            var dict = await mgmt.GetActorSettings(new List<string>() { "StockActor", "OrderActor" });
            this.nStockPartitions = dict["StockActor"];
            this.nOrderPartitions = dict["OrderActor"];
           // Console.WriteLine("Payment grain {0} activated: #stock grains {1} #order grains {2} ", this.paymentActorId, nStockPartitions, nOrderPartitions);
        }

        public async Task ProcessPayment(Invoice invoice)
        {
            Console.WriteLine("Payment process starting for order {0}", invoice.order.id);
            bool approved = true;
            List<Task> tasks = new(invoice.items.Count);
            //_logger.LogInformation($"Payment grain {this.paymentActorId} -" +
            //    $" The external payment check answered to the order payment request: approved: {approved}.");

            if (approved)
            {
                foreach (var item in invoice.items)
                {
                    long partition = (item.product_id % nStockPartitions);
                    var stockActor = GrainFactory.GetGrain<IStockActorTransOrl>(partition);
                    tasks.Add(stockActor.ConfirmOrder(new ConfirmOrderParameter(item.product_id, item.quantity)));
                }
            }

            await Task.WhenAll(tasks);

            tasks.Clear();

            // call order, customer, and shipment
            var orderActor = GrainFactory.GetGrain<IOrderActorTransOrl>(invoice.order.id % nOrderPartitions);
            if (approved)
            {
                 Console.WriteLine("Payment process succeeded for order {0}", invoice.order.id);

                // ?? what is the status processing? should come before or after payment?
                // before is INVOICED, so can only come after. but shipment sets to shipped...
                // I think processing is when the seller must approve or not the order,
                // but here all orders are approved by default. so we dont use processing
                // notify
                tasks.Add(orderActor.UpdateOrderStatus(new UpdateOrderStatusParameter(invoice.order.id, OrderStatus.PAYMENT_PROCESSED)));

                List<OrderPayment> paymentLines = new();
                int seq = 1;

                // create payment tuples
                if (invoice.customer.PaymentType.Equals(PaymentType.CREDIT_CARD.ToString()) ||
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

                   await _payments.PerformUpdate(p => p.cardPayments.Add(invoice.order.id, card));
                }

                if (invoice.customer.PaymentType.Equals(PaymentType.BOLETO.ToString()))
                {
                    paymentLines.Add(new OrderPayment()
                    {
                        order_id = invoice.order.id,
                        payment_sequential = seq,
                        payment_type = invoice.customer.PaymentType,
                        payment_installments = 1,
                        payment_value = invoice.order.total_amount
                    });
                }

                await _payments.PerformUpdate(p => p.payments.Add(invoice.order.id, paymentLines));
            }
            await Task.WhenAll(tasks);
        }
    }
}
