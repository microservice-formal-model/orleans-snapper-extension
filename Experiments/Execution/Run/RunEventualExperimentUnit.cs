using Common.Snapper.Product;
using CsvHelper;
using Experiments.Execution.Latency;
using Experiments.Execution.Throughput;
using Experiments.ExperimentsModel;
using Experiments.Workload.PropertyMaps;
using Experiments.Workload.TableEntries;
using Marketplace.Grains.Orleans.ActorInterfaces;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Experiments.Execution.Run
{
    public class RunEventualExperimentUnit : IRunExperiment
    {
        private readonly string directory;
        private readonly IClusterClient clusterClient;
        private readonly Experiment experiment;
        private ILatencyStrategy? latencyStrategy;
        private IThroughputStrategy? througputStategy;
        private bool endTasks;
        long counter = 0;

        public RunEventualExperimentUnit(Experiment experiment, IClusterClient client, string dir)
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
            Dictionary<int, IOrderActorOrleans>? orderPartitions = Enumerable
                .Range(0, experiment.Partitioning.NOrderPartitions)
                .Select(nr => (nr, clusterClient.GetGrain<IOrderActorOrleans>(nr, "Marketplace.Grains.Orleans.Actors.OrderActorOrleans")))
                .ToDictionary(kp => kp.nr, kp => kp.Item2);

            List<Task> runningTasks = new();

            //Using this date time following this thread: https://stackoverflow.com/questions/16032451/get-datetime-now-with-milliseconds-precision.
            //However, after some research I found that most system clocks are implemented by refreshing around 10 - 15 ms, if
            //we need this precision than we need to find a different way to implement this accurately
            var experimentStartPoint = DateTime.UtcNow;
            int iterationCounter = 0;

            using var reader = new StreamReader(Path.Combine(directory, $"checkouts{workerId}.csv"));
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<CheckoutParameterAccessMap>();
            var records = csv.GetRecords<CheckoutParameterAccess>();
            IEnumerator<CheckoutParameterAccess> recordEnumerator = records.GetEnumerator();

            List<CheckoutParameterAccess> transactionsInMemory = new();

            while (recordEnumerator.MoveNext())
            {
                transactionsInMemory.Add(recordEnumerator.Current);
            }

            //Entry point for Throughput measurement
            barrier.SignalAndWait();
            througputStategy?.AddStartPoint(DateTime.UtcNow);

            //The loop is terminated from the outside when a timer has been set. To safely shut down the experiments
            //we let the current batch of transaction come to an end
            while (!endTasks)
            {
                var transactionEnumerator = transactionsInMemory.GetEnumerator();

                while (runningTasks.Count < experiment.NrActiveTransactions && transactionEnumerator.MoveNext())
                {
                    var recordWithCorrectId = transactionEnumerator.Current.TransactionCopy();
                    // We need to perform a deep copy here, since it could be that the order id
                    //is being read by another transaction being processed
                    recordWithCorrectId.ChkParam.OrderId += (iterationCounter * experiment.CheckoutInformation.TotalAmount);
                    runningTasks.Add(latencyWrapper(experiment.Partitioning.NOrderPartitions,
                        experiment.Partitioning.NStockPartitions,
                        recordWithCorrectId));
                }
                //It can happen that if we perform less than 64 checkouts, that we don't need to enter this
                //loop but this is cool because MoveNext will return false in this case
                while (transactionEnumerator.MoveNext())
                {
                    //Wait for any of the tasks to complete and insert a new one
                    var anyTaskFinished = await Task.WhenAny(runningTasks);
                    await anyTaskFinished;
                    var recordWithCorrectId = transactionEnumerator.Current.TransactionCopy();
                    recordWithCorrectId.ChkParam.OrderId += (iterationCounter * experiment.CheckoutInformation.TotalAmount);
                    runningTasks.Add(latencyWrapper(experiment.Partitioning.NOrderPartitions,
                        experiment.Partitioning.NStockPartitions,
                        recordWithCorrectId));
                    runningTasks.Remove(anyTaskFinished);
                }

                await Task.WhenAll(runningTasks);
                runningTasks.Clear();
                iterationCounter++;
            }
            

            async Task latencyWrapper(int nrOrderPartitions, int nrStockPartitions, CheckoutParameterAccess chkParam)
            {
                var orderPartition = Convert.ToInt32(chkParam.ChkParam.OrderId) % nrOrderPartitions;
                var latencyWatch = new Stopwatch();

                var latencyStartPoint = DateTime.UtcNow;

                latencyWatch.Start();
                await orderPartitions[orderPartition].Checkout(chkParam.ChkParam);
                latencyWatch.Stop();
                latencyStrategy?.CollectLatency(latencyWatch);
                througputStategy?.CollectThroughput(DateTime.UtcNow);
                counter++;
            }
        }

        public async Task ExecuteUpdateProductWorker(int workerId, int globalWorkerId, Barrier barrier)
        {
            if(experiment.BenchmarkType == BenchmarkType.EVENTUAL)
            {

            }
            Dictionary<int, IProductActorOrleans> productPartitions = Enumerable
                .Range(0, experiment.Partitioning.NProductPartitions)
                .Select(nr => (nr, clusterClient.GetGrain<IProductActorOrleans>(nr, "Marketplace.Grains.Orleans.Actors.ProductActorOrleans")))
                .ToDictionary(kp => kp.nr, kp => kp.Item2);

            //This line needs some comments because as simple as it looks, this might potentially 
            //be a bottleneck. Each record is being read one by one to avoid loading the whole file
            //into memory. Thus, the ingestion of transactions to the system is dependent on how fast 
            //the reading process from the hard disk can be performed. 
            //The speed of this reading process here is directly influencing the performance of the 
            //experiments overall.
            List<Task> tasks = new();
            List<UpdateProductParameter> transactionLoadInMemory = new();

            //Load all transactions into local memory for faster transmission
            //This ensures that the IO operations coming with reading from files does not
            //produce a bottleneck
            using var reader = new StreamReader(Path.Combine(directory, $"updateProducts{workerId}.csv"));
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<UpdateProductParameterMap>();

            var records = csv.GetRecords<UpdateProductParameter>();
            IEnumerator<UpdateProductParameter> recordEnumerator = records.GetEnumerator();

            while(recordEnumerator.MoveNext())
            {
                transactionLoadInMemory.Add(recordEnumerator.Current);
            }

            //Entry point for Throughput measurement
            barrier.SignalAndWait();
            througputStategy?.AddStartPoint(DateTime.UtcNow);

            //Here we don't need an iteration counter, because UpdateProducts don't have ids
            while (!endTasks)
            {
                IEnumerator<UpdateProductParameter> transactionEnumerator = transactionLoadInMemory.GetEnumerator();
                while (tasks.Count < experiment.NrActiveTransactions && transactionEnumerator.MoveNext())
                {
                    tasks.Add(latencyWrapper(
                        experiment.Partitioning.NProductPartitions,
                        experiment.Partitioning.NStockPartitions,
                        transactionEnumerator.Current));
                }
                //It can happen that if we perform less than 64 checkouts, that we don't need to enter this
                //loop but this is cool because MoveNext will return false in this case
                while (transactionEnumerator.MoveNext())
                {
                    //Wait for any of the tasks to complete and insert a new one
                    var anyTaskFinished = await Task.WhenAny(tasks);
                    await anyTaskFinished;
                    tasks.Add(latencyWrapper(experiment.Partitioning.NProductPartitions,
                        experiment.Partitioning.NStockPartitions,
                        transactionEnumerator.Current));
                    tasks.Remove(anyTaskFinished);
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

               
                var elapsedStartPoint = DateTime.UtcNow;
                latencyWatch.Start();
                await productPartitions[productPartition].UpdateProduct(updateProduct);
                latencyWatch.Stop();
                latencyStrategy?.CollectLatency(latencyWatch);
                througputStategy?.CollectThroughput(DateTime.UtcNow);
                counter++;
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

        public async Task RunExperiment(
            ILatencyStrategy latencyStrategy,
            IThroughputStrategy throughputStrategy)
        {
            this.latencyStrategy = latencyStrategy;
            througputStategy = throughputStrategy;
            List<Task> tasks = new();
            var range = Enumerable.Range(0, Math.Max(
                            experiment.WorkersInformation.AmountCheckoutWorkers,
                            experiment.WorkersInformation.AmountUpdateProductWorkers));
            Barrier barrier = new(
                experiment.WorkersInformation.AmountCheckoutWorkers +
                            experiment.WorkersInformation.AmountUpdateProductWorkers + 1
                );
            foreach (int workerId in range)
            {
                if (workerId < experiment.WorkersInformation.AmountCheckoutWorkers)
                {
                    tasks.Add(Task.Run(() => ExecuteCheckoutWorker(workerId,0, barrier)));
                }

                if (workerId < experiment.WorkersInformation.AmountUpdateProductWorkers)
                {
                    tasks.Add(Task.Run(() => ExecuteUpdateProductWorker(workerId,0, barrier)));
                }
            }
            barrier.SignalAndWait();
            //Add a timer for the desired runtime of the experiment only after all workers have
            //entered the barrier
            tasks.Add(Task.Delay(TimeSpan.FromSeconds(experiment.Runtime)));
            //We know that the other Tasks are not terminating here, so it has to be
            //the runtime task that terminated
            var timeoutTask = await Task.WhenAny(tasks);
            await timeoutTask;
            endTasks = true;
            await Task.WhenAll(tasks);
            Console.WriteLine($"Total amount of operations: {counter}");
            barrier.Dispose();
        }
    }
}
