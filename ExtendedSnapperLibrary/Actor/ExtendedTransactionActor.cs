using Utilities;
using Orleans.Concurrency;
using ExtendedSnapperLibrary.ActorInterface;
using Common.Snapper.Core;
using Common.Snapper.Order;
using System.Diagnostics;

namespace ExtendedSnapperLibrary.Actor;

[Reentrant]
public abstract class ExtendedTransactionActor : Grain, IExtendedTransactionActor
{

    long myID;
    Schedule schedule;

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        myID = this.GetPrimaryKeyLong();
        schedule = new Schedule(myID);
        return base.OnActivateAsync(cancellationToken);
    }

    public Task CheckGarbageCollection()
    {
        schedule.CheckGarbageCollection();
        return Task.CompletedTask;
    }

    public async Task<TransactionResult> StartTransaction(FunctionCall firstFunc, int scheduleCoordinatorId, Dictionary<ActorID, int> actorAccessInfo)
    {
        //STEP 1: register the transaction in the current schedule and wait for the batch to be ready for commitment
        //Console.WriteLine($"Contacting schedule coordinator: {scheduleCoordinatorId}");
        var tctx = await GrainFactory
            .GetGrain<IScheduleCoordinator>(scheduleCoordinatorId)
            .ReceiveTransactionContext(firstFunc, actorAccessInfo);
        // STEP 2: start executing this transaction by invoking the first function on the current actor
        var result = await Execute(tctx, firstFunc);
        // STEP 3: wait for corresponding batch to commit
        await schedule.WaitForBatchCommit(tctx.bid);
        // STEP 4: return result
        return new TransactionResult(result);
    }

    public Task ReceiveBatch(Batch batch)
    {
       // Console.WriteLine($"Actor {myID} receive batch: bid = {batch.bid}, lastBid = {batch.lastBid}");
        schedule.RegisterBatch(batch);
        return Task.CompletedTask;
    }

    public async Task<object> Execute(TransactionContext cxt, FunctionCall call)
    {
        
        // STEP 1: wait for turn to execute
        await schedule.WaitForTurn(cxt);
       // if(call.funcInput is CheckoutParameter chkParam)
       // {
       //     if(chkParam.OrderId > 29) 
       //     {
       //         Console.WriteLine("I have started to get a turn for a transaction that is above checkout id 30: " + cxt + 
       //             "orderid: " + chkParam.OrderId);
       //     }
       //     
       // }
        // STEP 2: invoke the function
        var mi = call.className.GetMethod(call.funcName);
        var result = await (Task<object>)mi.Invoke(this, new object[] { cxt, call.funcInput });

        // STEP 3: unblock later transactions
        schedule.FinishFunction(cxt);
        return result;
    }

    public Task BatchCommit(long bid)
    {
        schedule.BatchCommit(bid);
        return Task.CompletedTask;
    }
}