using Common.Entity;
using System.Text;
using Marketplace.Interfaces;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using SnapperLibrary.Actor;
using SnapperLibrary;
using SnapperLibrary.ActorInterface;
using Marketplace.Grains.Message;
using Common.Snapper.Order;
using Common.Snapper.Stock;
using Marketplace.Grains.Common;
using Utilities;

namespace Marketplace.Grains
{
    [Reentrant]
    public class OrderActor : TransactionActor, IOrderActor
    {
        private int nStockPartitions;
        private int nOrderPartitions;
        private int nPaymentPartitions;
        private long orderActorId;
        // it represents all orders in this partition
        private long nextOrderId;
        private long nextHistoryId;

        // database
        private readonly Dictionary<long, Order> orders;
        private readonly Dictionary<long, List<OrderItem>> items;

        // https://dev.olist.com/docs/retrieving-order-informations
        private readonly SortedList<long, List<OrderHistory>> history;

        private readonly ILogger<OrderActor> _logger;

        //private static readonly decimal[] emptyArray = Array.Empty<decimal>();

        public OrderActor(ILogger<OrderActor> _logger, Helper helper) : base(helper)
        {
            this.nextOrderId = 1;
            this.nextHistoryId = 1;
            this.orders = new();
            this.items = new();
            this.history = new();
            this._logger = _logger;
        }

        public async Task Init()
        {
            this.orderActorId = this.GetPrimaryKeyLong();
            var mgmt = GrainFactory.GetGrain<IMetadataGrain>(0);
            var dict = await mgmt.GetActorSettings(new List<string>() { "StockActor", "OrderActor", "PaymentActor" });
            this.nStockPartitions = dict["StockActor"];
            this.nOrderPartitions = dict["OrderActor"];
            this.nPaymentPartitions = dict["PaymentActor"];
            Console.WriteLine("Order grain {0} activated: #stock grains {1} #order grains {2} #payment grains {3} ", this.orderActorId, nStockPartitions, nOrderPartitions, nPaymentPartitions);
        }

        public Task<long> GetNextOrderId()
        {
            //while(this.nextOrderId % this.nOrderPartitions != this.orderActorId)
            //{
            //    this.nextOrderId++;
            //}
            return  Task.FromResult(this.nextOrderId);
        }

        /**
          * The details about placing an order in olist:
          * https://dev.olist.com/docs/orders-notifications-details 
          * This transaction may not progress to payment
         */
       // public async Task Checkout(TransactionContext tctx, Checkout checkout, long orderId)
        public async Task<object> Checkout(TransactionContext tctx, object input)
        {
            var param = (CheckoutParameter)input;
            var checkout = param.Checkout;
            var orderId = param.OrderId;
            //Console.WriteLine("Checkout Process starting on orderactor with id: {0}", this.orderActorId);
            //Console.WriteLine("Checkout Process starting on orderactor with id: {0}", this.orderActorId);
            //_logger.LogInformation("Checkout Process starting on orderactor with id: {0}", this.orderActorId);

            List<Task<object>> statusResp = new(checkout.Items.Count);

            //the number of calls is dependent on the amount of items
            foreach (var item in checkout.Items)
            {
                long partition = (item.ProductId % nStockPartitions);
                var stockActor = GrainFactory.GetGrain<ITransactionActor>(partition, "Marketplace.Grains.StockActor");
                statusResp.Add(stockActor.Execute(tctx,
                    new FunctionCall(
                        "AttemptReservation",
                        new AttemptReservationParameter(item.ProductId, item.Quantity),
                        typeof(StockActor))));
            }

            await Task.WhenAll(statusResp);

            //Collecting all relevant items indices by aggregating over the list keeping the index that we are currently looking at
            //and the list of relevant indices in the accumulator
            var relevantItemsIndices = statusResp.Aggregate((new List<int>(), 0), (acc, next) =>
            {
                if ((ItemStatus)next.Result != ItemStatus.IN_STOCK)
                    return (acc.Item1, acc.Item2 + 1);
                else
                {
                    acc.Item1.Add(acc.Item2);
                    return (acc.Item1, acc.Item2 + 1);
                }
            }).Item1;

            //Collecting all items by getting them at relevant indices
            var relevantItems = relevantItemsIndices.Aggregate(new List<CartItem>(), (acc, next) =>
            {
                acc.Add(checkout.Items.ElementAt(next));
                return acc;
            });

            List<Task> tasks = new(relevantItems.Count);

            foreach (var item in relevantItems)
            {
                //Console.WriteLine("Attempting to Confirm the Reservation of product with id: {0}", item.ProductId);
                long partition = (item.ProductId % nStockPartitions);
                var stockActor = GrainFactory.GetGrain<ITransactionActor>(partition, "Marketplace.Grains.StockActor");
                //Console.WriteLine("Attempting to Confirm the Reservation of product with id: {0}", item.ProductId);
                tasks.Add(stockActor.Execute(
                    tctx,
                    new FunctionCall(
                        "ConfirmReservation",
                        new ConfirmReservationParameter(item.ProductId, item.Quantity),
                        typeof(StockActor))));
            }

            await Task.WhenAll(tasks);
            //Console.WriteLine("From the initial amount of items: {0}, where {1} items unavailable. We need to perform {1} amount of NoOps"
            //    , statusResp.Count, statusResp.Count - relevantItems.Count);

            //For Snapper we need to perform the amount of No-Operations, because the amount of items that are relevant.
            //are non-deterministically determined during the checkout transactions.
            //To access the correct partition, we need to calculate the difference between relevantItems and checkout.items.
            await SendNoops(tctx, checkout, statusResp, relevantItems);
            // calculate total freight_value
            decimal total_freight = 0;
            foreach (var item in relevantItems)
            {
                total_freight += item.FreightValue;
            }

            decimal total_amount = 0;
            foreach (var item in relevantItems)
            {
                total_amount += (item.UnitPrice * item.Quantity);
            }

            decimal total_items = total_amount;
            Order newOrder = new()
            {
                id = orderId,
                customer_id = checkout.CustomerCheckout.CustomerId,
                // olist have seller acting in the approval process
                // here we approve automatically
                // besides, invoice is a request for payment, so it makes sense to use this status now
                status = OrderStatus.INVOICED.ToString(),
                created_at = System.DateTime.Now,
                purchase_timestamp = checkout.CreatedAt,
                total_amount = total_amount,
                total_items = total_items,
                total_freight = total_freight,
                total_invoice = total_amount + total_freight,
                count_items = relevantItems.Count,

            };
            this.orders.Add(orderId, newOrder);

            List<OrderItem> orderItems = new(relevantItems.Count);
            int id = 0;
            foreach (var item in relevantItems)
            {
                orderItems.Add(
                    new()
                    {
                        order_id = orderId,
                        order_item_id = id,
                        product_id = item.ProductId,
                        seller_id = item.SellerId,
                        unit_price = item.UnitPrice,
                        quantity = item.Quantity,
                        total_items = item.UnitPrice * item.Quantity,
                        total_amount = (item.Quantity * item.FreightValue) + (item.Quantity * item.UnitPrice) // freight value applied per item by default
                    }
                    );
                id++;
            }

            items.Add(orderId, orderItems);

            // initialize order history
            history.Add(orderId, new List<OrderHistory>() { new OrderHistory()
            {
                id = nextHistoryId,
                created_at = newOrder.created_at, // redundant, but it is what it is...
                status = OrderStatus.INVOICED.ToString(),

            } });

            Invoice invoice = new(checkout.CustomerCheckout, newOrder, orderItems);
            
            // increment
            //nextOrderId++;
            nextHistoryId++;
            //For some reason the nPaymentPartition is 0 here ?
            long paymentActorPartition = newOrder.id % nPaymentPartitions;
            var paymentActor = GrainFactory.GetGrain<ITransactionActor>(paymentActorPartition, "Marketplace.Grains.PaymentActor");
            await paymentActor.Execute(tctx, new FunctionCall("ProcessPayment", invoice, typeof(PaymentActor)));
            
            //For Snapper we need to perform the amount of No-Operations, because the amount of items that are relevant.
            //are non-deterministically determined during the checkout transactions.
            //To access the correct partition, we need to calculate the difference between relevantItems and checkout.items.
            await SendNoops(tctx, checkout, statusResp, relevantItems);

            //_logger.LogInformation("Order grain {0} -- Checkout process ended for customer {1} -- Order id is {2}",
             //  this.orderActorId, checkout.customerCheckout.CustomerId, orderId);
            return new object();
        }

        private async Task SendNoops(TransactionContext tctx, Checkout checkout, List<Task<object>> statusResp, List<CartItem> relevantItems)
        {
            if (statusResp.Count - relevantItems.Count > 0)
            {
                var differenceItems = checkout.Items.Except(relevantItems).ToList();
                List<Task> noopsTasks = new(differenceItems.Count);

               // _logger.LogWarning("Order grain {0} -- The following requested items do not exist: {1}",this.orderActorId,
               //     string.Join(", ", differenceItems.Select(i => $"id: {i.ProductId}")));
                differenceItems.ForEach(x => Console.WriteLine(x.ProductId));
                foreach (var item in differenceItems)
                {
                    var partition = (item.ProductId % nStockPartitions);
                    var stockActor = GrainFactory.GetGrain<ITransactionActor>(partition, "Marketplace.Grains.StockActor");
                    noopsTasks.Add(stockActor.Execute(
                        tctx, new FunctionCall("Noop", null, typeof(StockActor))));
                }
                await Task.WhenAll(noopsTasks);
            }
        }

        /**
         * Olist prescribes that order status is "delivered" if at least one order item has been delivered
         * Based on https://dev.olist.com/docs/orders
         */
        public Task<object> UpdateOrderStatus(TransactionContext tctx, object input)
        {
            //Console.WriteLine("Attempting to update order");
            var updateOrderParam = (UpdateOrderStatusParameter)input;
            var orderId = updateOrderParam.orderId;
            var status = updateOrderParam.orderStatus;

            if (!this.orders.ContainsKey(orderId))
            {
                string str = new StringBuilder().Append("Order ").Append(orderId)
                    .Append(" cannot be found to update to status ").Append(status.ToString()).ToString();
                throw new Exception(str);
            }

            var now = DateTime.Now;

            OrderHistory orderHistory = null;

            // on every update, update the field updated_at in the order
            this.orders[orderId].updated_at = now;
            string oldStatus = this.orders[orderId].status;
            this.orders[orderId].status = status.ToString();

            // on shipped status, update delivered_carrier_date and estimated_delivery_date. add the entry
            if (status == OrderStatus.SHIPPED)
            {
                this.orders[orderId].delivered_carrier_date = now;
                this.orders[orderId].estimated_delivery_date = now;
            }

            // on payment failure or success, update payment_date and add the respective entry
            if (status == OrderStatus.PAYMENT_PROCESSED || status == OrderStatus.PAYMENT_FAILED)
            {
                this.orders[orderId].payment_date = now; 
            }

            // on first delivery, update delivered customer date
            // dont need the second check since the shipment is supposed to keep track
            if(status == OrderStatus.DELIVERED)
            {
                this.orders[orderId].delivered_customer_date = now;
            }

            orderHistory = new()
            {
                id = this.nextHistoryId,
                created_at = now,
                status = status.ToString()
            };

            this.history[orderId].Add(orderHistory);

            this.nextHistoryId++;

            //Console.WriteLine(
            //     "Order grain {0} -- Updated order status of order id {1} from {2} to {3}",
            //     this.orderActorId, orderId, oldStatus, this.orders[orderId].status);

            return Task.FromResult((object)"success");
        }

        public Task<List<Order>> GetOrders(TransactionContext tctx, long customerId, Predicate<Order> predicate = null)
        {
            List<Order> res;
            if (predicate is not null)
                res = this.orders.Select(o => o.Value).Where(p => p.customer_id == customerId && predicate.Invoke(p)).ToList();
            else
                res = this.orders.Select(o => o.Value).Where(p => p.customer_id == customerId).ToList();
            return Task.FromResult(res);
        }
    }
}

