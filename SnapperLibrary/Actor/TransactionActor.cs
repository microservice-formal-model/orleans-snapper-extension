using Common.Snapper.Core;
using Orleans.Concurrency;
using SnapperLibrary.ActorInterface;
using System.Diagnostics;
using System.Reflection;
using Utilities;

namespace SnapperLibrary.Actor;

[Reentrant]
public abstract class TransactionActor : Grain, ITransactionActor
{
    int n;

    long myID;
    string myName;
    int scheduleID = 0;
    Dictionary<int, Schedule> schedules;    // <schedule ID, schedule info>

    private readonly Helper helper;

    public TransactionActor(Helper helper)
    {
        this.helper = helper;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        myID = this.GetPrimaryKeyLong();
        myName = this.GetPrimaryKeyString();
        schedules = new Dictionary<int, Schedule>();
        Console.WriteLine($"Actor {myID} initialized. ");
        return base.OnActivateAsync(cancellationToken);
    }

    public Task CheckGarbageCollection()
    {
        foreach (var schedule in schedules) schedule.Value.CheckGarbageCollection();
        return Task.CompletedTask;
    }

    public async Task<TransactionResult> StartTransaction(FunctionCall firstFunc, Dictionary<ActorID, int> actorAccessInfo)
    {
        // STEP 2: register this transaction at Snapper
        var coordID = new Random().Next(helper.numCoordPerGroup);
        var coordinator = GrainFactory.GetGrain<ICoordinator>(coordID);
        var cxt = await coordinator.NewTransaction(actorAccessInfo);
        
        // STEP 3: start executing this transaction by invoking the first function on the current actor
       
        var result = await Execute(cxt, firstFunc);
        
        // STEP 4: wait for corresponding batch to commit
        
        await schedules[cxt.scheduleID].WaitForBatchCommit(cxt.bid);
        
        // STEP 5: return result
        return new TransactionResult(result);
    }

    public Task ReceiveBatch(Batch batch)
    {
        if (schedules.ContainsKey(batch.scheduleID) == false) schedules.Add(batch.scheduleID, new Schedule(myID, myName));
        schedules[batch.scheduleID].RegisterBatch(batch);
        return Task.CompletedTask;
    }

    public async Task<object> Execute(TransactionContext cxt, FunctionCall call)
    {
        // STEP 1: wait for turn to execute
        if (schedules.ContainsKey(cxt.scheduleID) == false) schedules.Add(cxt.scheduleID, new Schedule(myID, myName));
        await schedules[cxt.scheduleID].WaitForTurn(cxt);

        // STEP 2: invoke the function
        var mi = call.className.GetMethod(call.funcName);
        try
        {
            var result = await (Task<object?>)mi.Invoke(this, new object[] { cxt, call.funcInput });

            // STEP 3: unblock later transactions
            schedules[cxt.scheduleID].FinishFunction(cxt);

            return result;
        } catch (TargetParameterCountException tex)
        {
            Console.WriteLine("------Snapper-------");
            Console.WriteLine($"Trying to invoke a function: ${call.funcName}");
            Console.WriteLine($"With parameter object: ${call.funcInput}");
            Console.WriteLine($"On Actorclass: ${call.className}");
            Console.WriteLine("But the expected amount of Parameters mismatches the send amount of parameter.");
            Console.WriteLine($"The message of the exception is: ${tex.Message}");
            Console.WriteLine(tex.StackTrace);
            throw tex;
        }
    }

    public Task BatchCommit(long bid)
    {
        Debug.Assert(schedules.ContainsKey(scheduleID));
        schedules[scheduleID].BatchCommit(bid);

        return Task.CompletedTask;
    }
}