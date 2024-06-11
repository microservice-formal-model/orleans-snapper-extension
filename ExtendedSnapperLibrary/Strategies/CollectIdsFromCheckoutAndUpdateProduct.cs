using Common.Snapper.Order;
using Common.Snapper.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSnapperLibrary.Strategies
{
    internal class CollectIdsFromCheckoutAndUpdateProduct : ICollectIdsStrategy
    {
        public IEnumerable<long> CollectIds(FunctionCall f)
        {
            IEnumerable<long> ids = new List<long>();
            if (f.funcInput is CheckoutParameter chkParam)
            {
                ids = chkParam.Checkout.Items.Select(i => i.ProductId);
            }
            else if (f.funcInput is UpdateProductParameter upParam)
            {
                ids = new List<long>() { upParam.Product.product_id };
            }
            return ids;
        }
    }
}
