using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSnapperLibrary.Strategies
{
    public interface ICollectIdsStrategy
    {
        IEnumerable<long> CollectIds(FunctionCall f);
    }
}
