using Orleans;

namespace Common.Snapper.Stock
{
    [GenerateSerializer]
    public class IncreaseStockParameter
    {
        [Id(0)]
        public long productId { get; set; }

        [Id(1)]
        public int quantity { get; set; }

        public IncreaseStockParameter(long productId, int quantity)
        {
            this.productId = productId;
            this.quantity = quantity;
        }
    }
}
