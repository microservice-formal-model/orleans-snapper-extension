using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.ExperimentsModel
{
    public class CheckoutInformation
    {
        public class CheckoutRange { public int Start { get; set; } public int End { get; set; } }
        public CheckoutRange Size { get; set; }

        public int TotalAmount { get; set; }
    }
}
