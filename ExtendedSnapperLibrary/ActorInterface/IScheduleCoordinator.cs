using Common.Snapper.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSnapperLibrary.ActorInterface
{
    //<payload information, external transaction id, access information>
    using SnapperTransaction = Tuple<FunctionCall, long, Dictionary<ActorID, int>>;
    public interface IScheduleCoordinator : IGrainWithIntegerKey
    {
        public Task<TransactionContext> ReceiveTransactionContext(FunctionCall f, Dictionary<ActorID, int> actorAccessInformation);

        public Task Init();
    }
}
