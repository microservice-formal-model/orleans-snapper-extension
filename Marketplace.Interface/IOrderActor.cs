using System;
using Orleans;
using System.Threading.Tasks;
using Orleans.Concurrency;
using System.Collections.Generic;
using SnapperLibrary;

namespace Marketplace.Interfaces
{
    /**
     * Order actor does not coordinate with product actors.
     * Order only coordinate with stock actors.
     * This design favors higher useful work per time unit.
     * Since product is a user-facing microservice, most
     * customer requests target the product microservice.
     */
    
    public interface IOrderActor : IGrainWithIntegerKey
    {
        public Task<object> Checkout(TransactionContext tctx, object input);
        public Task<object> UpdateOrderStatus(TransactionContext tctx, object input);

        public Task Init();

        public Task<long> GetNextOrderId();
        //public Task<List<Order>> GetOrders(TransactionContext tctx, long customerId, Predicate<Order> predicate);
    }
}

