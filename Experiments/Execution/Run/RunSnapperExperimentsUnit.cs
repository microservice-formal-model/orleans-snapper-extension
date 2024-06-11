using Common.Snapper.Core;
using Common.Snapper.Product;
using CsvHelper;
using Experiments.Execution.Latency;
using Experiments.Execution.Throughput;
using Experiments.ExperimentsModel;
using Experiments.Workload.PropertyMaps;
using Experiments.Workload.TableEntries;
using Marketplace.Grains;
using SnapperLibrary;
using SnapperLibrary.ActorInterface;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;

namespace Experiments.Execution.Run
{
    public class RunSnapperExperimentsUnit : IRunExperiment
    {
        private readonly string directory;
        private readonly IClusterClient clusterClient;
        private readonly Experiment experiment;
        private ILatencyStrategy? latencyStrategy;
        private IThroughputStrategy? througputStategy;
        bool endTasks;
        public RunSnapperExperimentsUnit(Experiment experiment, IClusterClient client, string dir)
        {
            directory = dir;
            clusterClient = client;
            this.experiment = experiment;
            latencyStrategy = null;
            througputStategy = null;
            endTasks = false;
        }
        public async Task ExecuteCheckoutWorker(int workerId, int globalWorkerId, Barrier barrier)
        {
            Dictionary<int, ITransactionActor>? orderPartitions = Enumerable
                .Range(0, experiment.Partitioning.NOrderPartitions)
                .Select(nr => (nr, clusterClient.GetGrain<ITransactionActor>(nr, "Marketplace.Grains.OrderActor")))
                .ToDictionary(kp => kp.nr, kp => kp.Item2);
            //This line needs some comments because as simple as it looks, this might potentially 
            //be a bottleneck. Each record is being read one by one to avoid loading the whole file
            //into memory. Thus, the ingestion of transactions to the system is dependent on how fast 
            //the reading process from the hard disk can be performed. 
            //The speed of this reading process here is directly influencing the performance of the 
            //experiments overall.
            List<Task> runningTasks = new();

            //(1) create a connection to the file base using the passed id
            using var reader = new StreamReader(Path.Combine(directory, $"checkouts{workerId}.csv"));
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<CheckoutParameterAccessMap>();
            //This is only loading an iterator over the file.
            //It will be loaded into memory only record per record
            var records = csv.GetRecords<CheckoutParameterAccess>();
            using IEnumerator<CheckoutParameterAccess> recordEnumerator = records.GetEnumerator();
            List<CheckoutParameterAccess> transactionsInMemory = new();
            while (recordEnumerator.MoveNext())
            {
                transactionsInMemory.Add(recordEnumerator.Current);
            }
            //Using this date time following this thread: https://stackoverflow.com/questions/16032451/get-datetime-now-with-milliseconds-precision.
            //However, after some research I found that most system clocks are implemented by refreshing around 10 - 15 ms, if
            //we need this precision than we need to find a different way to implement this accurately
            // var experimentStartPoint = DateTime.UtcNow;
            barrier.SignalAndWait();
            //We first fill the tasks with 64 tasks and then we start the loop to simply enter a new task anytime that one has finished
            //The order of the check is important here, we do only increment the iterator if we are still within the window of 64 transactions

            //Entry point for Throughput measurement
            througputStategy?.CollectThroughput(DateTime.UtcNow);
            int counter = 0;

            int iterationCounter = 0;
            while (!endTasks)
            {
                var transactionEnumerator = transactionsInMemory.GetEnumerator();

                while (runningTasks.Count < experiment.NrActiveTransactions && transactionEnumerator.MoveNext())
                {
                    var recordWithCorrectId = transactionEnumerator.Current.TransactionCopy();
                    recordWithCorrectId.ChkParam.OrderId += iterationCounter * experiment.CheckoutInformation.TotalAmount;
                    runningTasks.Add(latencyWrapper(experiment.Partitioning.NOrderPartitions,
                        experiment.Partitioning.NStockPartitions,
                        recordWithCorrectId));
                    counter++;
                }
                //It can happen that if we perform less than 64 checkouts, that we don't need to enter this
                //loop but this is cool because MoveNext will return false in this case
                while (transactionEnumerator.MoveNext())
                {
                    //Wait for any of the tasks to complete and insert a new one
                    var anyTaskFinished = await Task.WhenAny(runningTasks);
                    await anyTaskFinished;
                    var recordWithCorrectId = transactionEnumerator.Current.TransactionCopy();
                    recordWithCorrectId.ChkParam.OrderId += iterationCounter * experiment.CheckoutInformation.TotalAmount;
                    runningTasks.Add(latencyWrapper(experiment.Partitioning.NOrderPartitions,
                        experiment.Partitioning.NStockPartitions,
                        recordWithCorrectId));
                    runningTasks.Remove(anyTaskFinished);
                    counter++;
                }

                await Task.WhenAll(runningTasks);
                iterationCounter++;
                runningTasks.Clear();
            }

            async Task latencyWrapper(int nrOrderPartitions, int nrStockPartitions, CheckoutParameterAccess chkParam)
            {
                var orderPartition = Convert.ToInt32(chkParam.ChkParam.OrderId) % nrOrderPartitions;
                var latencyWatch = new Stopwatch();

                var latencyStartPoint = DateTime.UtcNow;

                latencyWatch.Start();
                try
                {
                   await orderPartitions[orderPartition]
                        .StartTransaction(
                          new SnapperLibrary.FunctionCall("Checkout", chkParam.ChkParam, typeof(OrderActor)), chkParam.GrainAccesses);
                }
                catch (TimeoutException)
                {
                    Console.WriteLine($"ch worker with id: {workerId} timeout for transaction with id: {chkParam.ChkParam.OrderId}");
                    throw;
                }
                catch (ArgumentOutOfRangeException)
                {
                    Console.WriteLine($"ch worker with id: {workerId} failed {chkParam.ChkParam.OrderId} on iteration: {iterationCounter}");
                    throw;
                }
                latencyWatch.Stop();
                latencyStrategy?.CollectLatency(latencyWatch);
                througputStategy?.CollectThroughput(DateTime.UtcNow);
            }
        }

        public async Task ExecuteUpdateProductWorker(int workerId, int globalWorkerId, Barrier barrier)
        {

            Dictionary<int, ITransactionActor> productPartitions = Enumerable
                .Range(0, experiment.Partitioning.NProductPartitions)
                .Select(nr => (nr, clusterClient.GetGrain<ITransactionActor>(nr, "Marketplace.Grains.ProductActor")))
                .ToDictionary(kp => kp.nr, kp => kp.Item2);

            //This line needs some comments because as simple as it looks, this might potentially 
            //be a bottleneck. Each record is being read one by one to avoid loading the whole file
            //into memory. Thus, the ingestion of transactions to the system is dependent on how fast 
            //the reading process from the hard disk can be performed. 
            //The speed of this reading process here is directly influencing the performance of the 
            //experiments overall.
            List<Task> tasks = new();

            //(1) create a connection to the file base using the passed id
            using var reader = new StreamReader(Path.Combine(directory, $"updateProducts{workerId}.csv"));
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<UpdateProductParameterMap>();
            //This is only loading an iterator over the file.
            //It will be loaded into memory only record per record
            var records = csv.GetRecords<UpdateProductParameter>();
            using IEnumerator<UpdateProductParameter> recordEnumerator = records.GetEnumerator();
            List<UpdateProductParameter> transactionsInMemory = new();
            while (recordEnumerator.MoveNext())
            {
                transactionsInMemory.Add(recordEnumerator.Current);
            }
            //We first fill the tasks with 64 tasks and then we start the loop to simply enter a new task anytime that one has finished
            //The order of the check is important here, we do only increment the iterator if we are still within the window of 64 transactions
            barrier.SignalAndWait();
            //Entry point for Throughput measurement
            througputStategy?.AddStartPoint(DateTime.UtcNow);
            int counter = 0;
            while (!endTasks)
            {
                var transactionEnumerator = transactionsInMemory.GetEnumerator();
                while (tasks.Count < experiment.NrActiveTransactions && transactionEnumerator.MoveNext())
                {
                    tasks.Add(latencyWrapper(experiment.Partitioning.NProductPartitions, experiment.Partitioning.NStockPartitions, transactionEnumerator.Current));
                    counter++;
                }
                //It can happen that if we perform less than 64 checkouts, that we don't need to enter this
                //loop but this is cool because MoveNext will return false in this case
                while (transactionEnumerator.MoveNext())
                {
                    //Wait for any of the tasks to complete and insert a new one
                    var anyTaskFinished = await Task.WhenAny(tasks);
                    await anyTaskFinished;
                    tasks.Add(latencyWrapper(experiment.Partitioning.NProductPartitions, experiment.Partitioning.NStockPartitions, transactionEnumerator.Current));
                    tasks.Remove(anyTaskFinished);
                    counter++;
                }

                await Task.WhenAll(tasks);
                tasks.Clear();
            }

            async Task latencyWrapper(int nrProductPartitions,
                int nrStockPartitions,
                UpdateProductParameter updateProduct)
            {
                var productPartition = Convert.ToInt32(updateProduct.Product.product_id) % nrProductPartitions;
                var latencyWatch = new Stopwatch();

                int one_or_two() { if (updateProduct.Quantity != 0) { return 2; } else { return 1; } }

                var dict = new Dictionary<ActorID, int>
                {
                    {new ActorID(productPartition,"Marketplace.Grains.StockActor"),one_or_two()},
                    {new ActorID(productPartition,"Marketplace.Grains.ProductActor"),1 }
                };
                var elapsedStartPoint = DateTime.UtcNow;
                latencyWatch.Start();
                try
                {
                     await productPartitions[productPartition]
                        .StartTransaction(
                          new SnapperLibrary.FunctionCall("UpdateProduct", updateProduct, typeof(ProductActor)), dict);
                }
                catch (TimeoutException)
                {
                    Console.WriteLine($"up worker with id {workerId} has been timeouted on transaction with id: {updateProduct.Product.product_id}");
                    throw;
                }
                latencyWatch.Stop();
                latencyStrategy?.CollectLatency(latencyWatch);
                througputStategy?.CollectThroughput(DateTime.UtcNow);
            }
        }

        public Task<ILatencyStrategy?> ReceiveLatencyResult()
        {
            return Task.FromResult(this.latencyStrategy);
        }

        public Task<IThroughputStrategy?> ReceiveThroughputResult()
        {
            return Task.FromResult(this.througputStategy);
        }

        public async Task RunExperiment(ILatencyStrategy latencyStrategy, IThroughputStrategy throughputStrategy)
        {
            this.latencyStrategy = latencyStrategy;
            througputStategy = throughputStrategy;
            List<Task> tasks = new();
            Barrier barrier = new(
                experiment.WorkersInformation.AmountCheckoutWorkers +
                experiment.WorkersInformation.AmountUpdateProductWorkers + 1
                );
            int gWorkerId = 0;
            foreach (int workerId in Enumerable.Range(0, experiment.WorkersInformation.AmountCheckoutWorkers))
            {
                tasks.Add(Task.Run(() => ExecuteCheckoutWorker(workerId, gWorkerId, barrier)));
                gWorkerId++;
            }
            foreach (int workerId in Enumerable.Range(0, experiment.WorkersInformation.AmountUpdateProductWorkers))
            {
                tasks.Add(Task.Run(() => ExecuteUpdateProductWorker(workerId, gWorkerId, barrier)));
                gWorkerId++;
            }
            barrier.SignalAndWait();
            //Add a timer for the desired runtime of the experiment
            tasks.Add(Task.Delay(TimeSpan.FromSeconds(experiment.Runtime)));
            //We know that the other Tasks are not terminating here, so it has to be
            //the runtime task that terminated
            var timeoutTask = await Task.WhenAny(tasks);
            await timeoutTask;
            endTasks = true;
            await Task.WhenAll(tasks);
            barrier.Dispose();
        }
    }
}
