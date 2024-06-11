using Common.Snapper.Core;
using Common.Snapper.Product;
using CsvHelper;
using Experiments.Execution.Latency;
using Experiments.Execution.Throughput;
using Experiments.ExperimentsModel;
using Experiments.Workload.PropertyMaps;
using Experiments.Workload.TableEntries;
using ExtendedSnapperLibrary.ActorInterface;
using Marketplace.Grains.Extended;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;

namespace Experiments.Execution.Run
{
    public class RunExtendedExperimentUnit : IRunExperiment
    {
        private readonly string directory;
        private readonly IClusterClient clusterClient;
        private readonly Experiment experiment;
        private ILatencyStrategy? latencyStrategy;
        private IThroughputStrategy? througputStategy;
        private bool endTasks;
        public RunExtendedExperimentUnit(Experiment experiment, IClusterClient client, string dir)
        {
            directory = dir;
            clusterClient = client;
            this.experiment = experiment;
            latencyStrategy = null;
            througputStategy = null;
            endTasks = false;
        }

        public async Task RunExperiment(ILatencyStrategy latencyStrategy, IThroughputStrategy throughputStrategy)
        {
            this.latencyStrategy = latencyStrategy;
            this.througputStategy = throughputStrategy;
            List<Task> tasks = new();
            Console.WriteLine($"amount schedule coordinatorss: {experiment.AmountScheduleCoordinators}");
            Barrier barrier = new(
                experiment.WorkersInformation.AmountCheckoutWorkers +
                experiment.WorkersInformation.AmountUpdateProductWorkers + 1);
            int myGlobId = 0;
            foreach (int workerId in Enumerable.Range(0,experiment.WorkersInformation.AmountCheckoutWorkers))
            {
                int myLocallySavedGlobalId = myGlobId;
                Console.WriteLine($"Adding a checkoutworker with id: {myLocallySavedGlobalId},workerid: {workerId}");
                tasks.Add(Task.Run(() => ExecuteCheckoutWorker(workerId, myLocallySavedGlobalId, barrier)));
                myGlobId += 1;
            }
            foreach(int workerId in Enumerable.Range(0,experiment.WorkersInformation.AmountUpdateProductWorkers))
            {
                int myLocallySavedGlobalId = myGlobId;
                Console.WriteLine($"Adding a updateproductworker with id: {myLocallySavedGlobalId},workerid: {workerId}");
                tasks.Add(Task.Run(() => ExecuteUpdateProductWorker(workerId, myLocallySavedGlobalId, barrier)));
                myGlobId += 1;
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

        public async Task ExecuteCheckoutWorker(int workerId, int myGlobId, Barrier barrier)
        {
            Console.WriteLine($"Started a checkout worker with globalId {myGlobId}, local: {workerId}");
            Dictionary<int, IExtendedTransactionActor>? orderPartitions = Enumerable
                .Range(0, experiment.Partitioning.NOrderPartitions)
                .Select(nr => (nr, clusterClient.GetGrain<IExtendedTransactionActor>(nr, "Marketplace.Grains.Extended.OrderActorExt")))
                .ToDictionary(kp => kp.nr, kp => kp.Item2);
            //This line needs some comments because as simple as it looks, this might potentially 
            //be a bottleneck. Each record is being read one by one to avoid loading the whole file
            //into memory. Thus, the ingestion of transactions to the system is dependent on how fast 
            //the reading process from the hard disk can be performed. 
            //The speed of this reading process here is directly influencing the performance of the 
            //experiments overall.
            List<Task> runningTasks = new();

            using var reader = new StreamReader(Path.Combine(directory, $"checkouts{workerId}.csv"));
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<CheckoutParameterAccessMap>();
            var records = csv.GetRecords<CheckoutParameterAccess>();
            using IEnumerator<CheckoutParameterAccess> recordEnumerator = records.GetEnumerator();
            List<CheckoutParameterAccess> transactionsInMemory = new();
            while (recordEnumerator.MoveNext())
            {
                transactionsInMemory.Add(recordEnumerator.Current);
            }
            //We first fill the tasks with 64 tasks and then we start the loop to simply enter a new task anytime that one has finished
            //The order of the check is important here, we do only increment the iterator if we are still within the window of 64 transactions
            barrier.SignalAndWait();
            //Console.WriteLine($"checkout worker with workerId: {workerId} has started.");
            //Entry point for Throughput measurement
            througputStategy?.AddStartPoint(DateTime.UtcNow);

            var experimentStartPoint = DateTime.UtcNow;
            int iterationCounter = 0;
            while (!endTasks)
            {
                
                using IEnumerator<CheckoutParameterAccess> transactionEnumerator = transactionsInMemory.GetEnumerator();

                while (runningTasks.Count <= 200 && transactionEnumerator.MoveNext())
                {
                    var recordWithCorrectId = transactionEnumerator.Current.TransactionCopy();
                    //Console.WriteLine("Found a checkout and added it: " + $"{checkoutsPath}checkouts{checkoutId}.csv");
                    recordWithCorrectId.ChkParam.OrderId += iterationCounter * experiment.CheckoutInformation.TotalAmount;
                    runningTasks.Add(latencyWrapper(experiment.Partitioning.NOrderPartitions,
                        experiment.Partitioning.NStockPartitions,
                        recordWithCorrectId));
                }
                //It can happen that if we perform less than 64 checkouts, that we don't need to enter this
                //loop but this is cool because MoveNext will return false in this case
                while (transactionEnumerator.MoveNext())
                {
                    //Wait for any of the tasks to complete and insert a new one
                    var anyTaskFinishes = await Task.WhenAny(runningTasks);
                    await anyTaskFinishes;
                    //For every iteration this is a new object since we create a new recordEnumerator 
                    //This ensures that we indeed get a new deep copy on every iteration
                    var recordWithCorrectId = transactionEnumerator.Current.TransactionCopy();
                    recordWithCorrectId.ChkParam.OrderId += (iterationCounter * experiment.CheckoutInformation.TotalAmount);
                    runningTasks.Add(latencyWrapper(experiment.Partitioning.NOrderPartitions,
                        experiment.Partitioning.NStockPartitions,
                        recordWithCorrectId));
                    runningTasks.Remove(anyTaskFinishes);
                }
                //Console.WriteLine($"ch worker id: {workerId}, trans: {counter}");
                await Task.WhenAll(runningTasks);
                runningTasks.Clear();
                iterationCounter++;
            }

            async Task latencyWrapper(int nrOrderPartitions, int nrStockPartitions, CheckoutParameterAccess chkParam)
            {

                var orderPartition = Convert.ToInt32(chkParam.ChkParam.OrderId) % nrOrderPartitions;
                //Console.WriteLine($"amount of order partitions: {nrOrderPartitions}, my calculated orderpatition: {orderPartition}");
                var latencyWatch = new Stopwatch();

                var latencyStartPoint = DateTime.UtcNow;

                latencyWatch.Start();
                try
                {
                    await orderPartitions[orderPartition]
                        .StartTransaction(
                          new ExtendedSnapperLibrary.FunctionCall("Checkout", chkParam.ChkParam, typeof(OrderActorExt)),
                          myGlobId % experiment.AmountScheduleCoordinators,
                          chkParam.GrainAccesses);
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


        public async Task ExecuteUpdateProductWorker(int workerId, int myGlobId, Barrier barrier)
        {
            Console.WriteLine($"Started an updateproduct worker with globalId {myGlobId}, local: {workerId}");
            Dictionary<int, IExtendedTransactionActor> productPartitions = Enumerable
                .Range(0, experiment.Partitioning.NProductPartitions)
                .Select(nr => (nr, clusterClient.GetGrain<IExtendedTransactionActor>(nr, "Marketplace.Grains.Extended.ProductActorExt")))
                .ToDictionary(kp => kp.nr, kp => kp.Item2);

            //This line needs some comments because as simple as it looks, this might potentially 
            //be a bottleneck. Each record is being read one by one to avoid loading the whole file
            //into memory. Thus, the ingestion of transactions to the system is dependent on how fast 
            //the reading process from the hard disk can be performed. 
            //The speed of this reading process here is directly influencing the performance of the 
            //experiments overall.
            List<Task> tasks = new();
            DateTime startPoint = DateTime.UtcNow;

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
            //Console.WriteLine($"update product worker with workerId: {workerId} has started.");
            //Entry point for Throughput measurement
            througputStategy?.AddStartPoint(DateTime.UtcNow);
            int iterationCounter = 0;
            while (!endTasks)
            {
                var transactionEnumerator = transactionsInMemory.GetEnumerator();
                while (tasks.Count <= 200 && transactionEnumerator.MoveNext())
                {
                    tasks.Add(latencyWrapper(experiment.Partitioning.NProductPartitions, experiment.Partitioning.NStockPartitions, transactionEnumerator.Current));
                }
                //It can happen that if we perform less than 64 checkouts, that we don't need to enter this
                //loop but this is cool because MoveNext will return false in this case
                while (transactionEnumerator.MoveNext())
                {

                    //Wait for any of the tasks to complete and insert a new one
                    var anyTaskFinishes = await Task.WhenAny(tasks);
                    await anyTaskFinishes;
                    tasks.Add(latencyWrapper(experiment.Partitioning.NProductPartitions, experiment.Partitioning.NStockPartitions, transactionEnumerator.Current));
                    tasks.Remove(anyTaskFinishes);
                }
                await Task.WhenAll(tasks);
                tasks.Clear();
                iterationCounter++;
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
                    {new ActorID(productPartition,"Marketplace.Grains.Extended.StockActorExt"),one_or_two()},
                    {new ActorID(productPartition,"Marketplace.Grains.Extended.ProductActorExt"),1 }
                };
                var elapsedStartPoint = DateTime.UtcNow;
                latencyWatch.Start();
                try
                {
                    await productPartitions[productPartition]
                        .StartTransaction(
                          new ExtendedSnapperLibrary.FunctionCall("UpdateProduct", updateProduct, typeof(ProductActorExt)),
                          myGlobId % experiment.AmountScheduleCoordinators,
                          dict);
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
    }
}
