using Common.Entity;
using Common.Snapper.Order;
using Common.Snapper.Stock;
using Marketplace.Grains.Common;
using Marketplace.Grains.Message;
using Marketplace.Grains.TransactionalOrleans.ActorInterfaces;
using Orleans.Concurrency;
using Orleans.Transactions.Abstractions;
using System.Text;

namespace Marketplace.Grains.TransactionalOrleans.Actors
{
    [Reentrant]
    public class OrderActorTransOrl : Grain, IOrderActorTransOrl
    {
        private int nStockPartitions;
        private int nOrderPartitions;
        private int nPaymentPartitions;
        private long orderActorId;
        // it represents all orders in this partition
        [GenerateSerializer]
        public class OrdersAndItemsState
        {
            [Id(0)]
            public Dictionary<long, Order> orders;
            [Id(1)]
            public Dictionary<long, List<OrderItem>> items;
            [Id(2)]
            public SortedList<long, List<OrderHistory>> history;
            [Id(3)]
            public int nextHistoryId;

            public OrdersAndItemsState() 
            {
                orders = new();
                items = new();
                history = new();
                nextHistoryId = 1;
            }
        }

        private readonly ITransactionalState<OrdersAndItemsState> _ordersAndItems;

        public OrderActorTransOrl(
            [TransactionalState(nameof(ordersAndItems))]
            ITransactionalState<OrdersAndItemsState> ordersAndItems)
        {
            _ordersAndItems = ordersAndItems ?? throw new ArgumentNullException(nameof(ordersAndItems));
        }

        public async Task Checkout(CheckoutParameter cp)
        {
            var checkout = cp.Checkout;
            var orderId = cp.OrderId;

            //Console.WriteLine("Checkout Process starting on orderactor with id: {0}", this.orderActorId);
            // _logger.LogInformation("Checkout Process starting on orderactor with id: {0}", this.orderActorId);
            Console.WriteLine($"Beginning to process Checkout with ids: {string.Join(",", cp.Checkout.Items.Select(i => i.ProductId))}");
            List<Task<ItemStatus>> statusResp = new(checkout.Items.Count);
            //the number of calls is dependent on the amount of items
            foreach (var item in checkout.Items)
            {
                long partition = (item.ProductId % nStockPartitions);
                var stockActor = GrainFactory.GetGrain<IStockActorTransOrl>(partition);
                statusResp.Add(stockActor.AttemptReservation(
                    new AttemptReservationParameter(item.ProductId, item.Quantity)));
            }
            await Task.WhenAll(statusResp);

            //Console.WriteLine("Here");

            //Collecting all relevant items indices by aggregating over the list keeping the index that we are currently looking at
            //and the list of relevant indices in the accumulator
            var relevantItemsIndices = statusResp.Aggregate((new List<int>(), 0), (acc, next) =>
            {
                if (next.Result != ItemStatus.IN_STOCK)
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
                long partition = (item.ProductId % nStockPartitions);
                var stockActor = GrainFactory.GetGrain<IStockActorTransOrl>(partition);
                Console.WriteLine("Attempting to Confirm the Reservation of product with id: {0}", item.ProductId);
                tasks.Add(stockActor.ConfirmReservation(new ConfirmReservationParameter(item.ProductId, item.Quantity)));
            }

            await Task.WhenAll(tasks);
             Console.WriteLine("From the initial amount of items: {0}, where {1} items unavailable. We need to perform {1} amount of NoOps"
                , statusResp.Count, statusResp.Count - relevantItems.Count);


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
            await _ordersAndItems.PerformUpdate(oi => oi.orders.Add(orderId, newOrder));

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

            await _ordersAndItems.PerformUpdate(oi => oi.items.Add(orderId, orderItems));

            // initialize order history
            await _ordersAndItems.PerformUpdate(oi => oi.history.Add(orderId, new List<OrderHistory>() { new OrderHistory()
            {
                id = oi.nextHistoryId,
                created_at = newOrder.created_at, // redundant, but it is what it is...
                status = OrderStatus.INVOICED.ToString(),

            }}));

            Invoice invoice = new(checkout.CustomerCheckout, newOrder, orderItems);

            // increment
            //nextOrderId++;
            await _ordersAndItems.PerformUpdate(oi => oi.nextHistoryId++);
            //For some reason the nPaymentPartition is 0 here ?
            long paymentActorPartition = newOrder.id % nPaymentPartitions;
            var paymentActor = GrainFactory.GetGrain<IPaymentActorTransOrl>(paymentActorPartition);
            await paymentActor.ProcessPayment(invoice);
            Console.WriteLine($"I have successfully finished checkout for ids: {string.Join(",", cp.Checkout.Items.Select(i => i.ProductId))}");
        }

        public async Task Init()
        {
            this.orderActorId = this.GetPrimaryKeyLong();
            var mgmt = GrainFactory.GetGrain<IMetadataGrain>(0);
            var dict = await mgmt.GetActorSettings(new List<string>() { "StockActor", "OrderActor", "PaymentActor" });
            this.nStockPartitions = dict["StockActor"];
            this.nOrderPartitions = dict["OrderActor"];
            this.nPaymentPartitions = dict["PaymentActor"];
        }

        public async Task UpdateOrderStatus(UpdateOrderStatusParameter uosp)
        {
            Console.WriteLine($"Attempting to update order for order id {uosp.orderId}");
            var orderId = uosp.orderId;
            var status = uosp.orderStatus;

            if (await _ordersAndItems.PerformUpdate(oi => !oi.orders.ContainsKey(orderId)))
            {
                string str = new StringBuilder().Append("Order ").Append(orderId)
                    .Append(" cannot be found to update to status ").Append(status.ToString()).ToString();
                throw new Exception(str);
            }

            var now = DateTime.Now;

            // on every update, update the field updated_at in the order
            await _ordersAndItems.PerformUpdate(oi =>
            {
                oi.orders[orderId].updated_at = now;
                string oldStatus = oi.orders[orderId].status;
                oi.orders[orderId].status = status.ToString();


                // on shipped status, update delivered_carrier_date and estimated_delivery_date. add the entry
                if (status == OrderStatus.SHIPPED)
                {
                    oi.orders[orderId].delivered_carrier_date = now;
                    oi.orders[orderId].estimated_delivery_date = now;
                }

                // on payment failure or success, update payment_date and add the respective entry
                if (status == OrderStatus.PAYMENT_PROCESSED || status == OrderStatus.PAYMENT_FAILED)
                {
                    oi.orders[orderId].payment_date = now;
                }

                // on first delivery, update delivered customer date
                // dont need the second check since the shipment is supposed to keep track
                if (status == OrderStatus.DELIVERED)
                {
                    oi.orders[orderId].delivered_customer_date = now;
                }

                OrderHistory orderHistory = new()
                {
                    id = oi.nextHistoryId,
                    created_at = now,
                    status = status.ToString()
                };

                oi.history[orderId].Add(orderHistory);

                oi.nextHistoryId++;
            });

            //Console.WriteLine(
            //    "Order grain {0} -- Updated order status of order id {1} from {2} to {3}",
            //    this.orderActorId, orderId, oldStatus, this.orders[orderId].status);
        }
    }
}
