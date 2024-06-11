using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Snapper.Stock
{
    [GenerateSerializer]
    public class DeleteItemParameter
    {
        [Id(0)]
        public long productId { get; set; }

        public DeleteItemParameter(long productId)
        {
            this.productId = productId;
        }
    }
}
