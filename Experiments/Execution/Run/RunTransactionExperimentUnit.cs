using Common.Snapper.Product;
using CsvHelper;
using Experiments.Execution.Latency;
using Experiments.Execution.Throughput;
using Experiments.ExperimentsModel;
using Experiments.Workload.PropertyMaps;
using Experiments.Workload.TableEntries;
using Marketplace.Grains.TransactionalOrleans.ActorInterfaces;
using Orleans.Transactions;
using System.Diagnostics;
using System.Globalization;

namespace Experiments.Execution.Run
{
    public class RunTransactionExperimentUnit : IRunExperiment
    {
        private readonly string directory;
        private readonly IClusterClient clusterClient;
        private readonly Experiment experiment;
        private ILatencyStrategy? latencyStrategy;
        private IThroughputStrategy? througputStategy;
        public RunTransactionExperimentUnit(Experiment experiment, IClusterClient client,  string dir)
        {
            directory = dir;
            clusterClient = client;
            this.experiment = experiment;
            latencyStrategy = null;
            througputStategy = null;
        }

        public async Task ExecuteCheckoutWorker(int workerId, int globalWorkerId, Barrier barrier)
        {
            //(1) create a connection to the file base using the passed id
            using var reader = new StreamReader(Path.Combine(directory, $"checkouts{workerId}.csv"));
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<CheckoutParameterAccessMap>();
            //This is only loading an iterator over the file.
            //It will be loaded into memory only record per record
            var records = csv.GetRecords<CheckoutParameterAccess>();

            Dictionary<int, IOrderActorTransOrl>? orderPartitions = Enumerable
                .Range(0, experiment.Partitioning.NOrderPartitions)
                .Select(nr => (nr, clusterClient.GetGrain<IOrderActorTransOrl>(nr)))
                .ToDictionary(kp => kp.nr, kp => kp.Item2);
            //This line needs some comments because as simple as it looks, this might potentially 
            //be a bottleneck. Each record is being read one by one to avoid loading the whole file
            //into memory. Thus, the ingestion of transactions to the system is dependent on how fast 
            //the reading process from the hard disk can be performed. 
            //The speed of this reading process here is directly influencing the performance of the 
            //experiments overall.
            List<Task<int>> runningTasks = new();
            //Using this date time following this thread: https://stackoverflow.com/questions/16032451/get-datetime-now-with-milliseconds-precision.
            //However, after some research I found that most system clocks are implemented by refreshing around 10 - 15 ms, if
            //we need this precision than we need to find a different way to implement this accurately
            

            using IEnumerator<CheckoutParameterAccess> recordEnumerator = records.GetEnumerator();
            //We first fill the tasks with 64 tasks and then we start the loop to simply enter a new task anytime that one has finished
            //The order of the check is important here, we do only increment the iterator if we are still within the window of 64 transactions
            var counter = 0;
            //Entry point for Throughput measurement
            barrier.SignalAndWait();
            througputStategy?.AddStartPoint(DateTime.UtcNow);
            while (runningTasks.Count < 100 && recordEnumerator.MoveNext())
            {
                //Console.WriteLine("Found a checkout and added it: " + $"{checkoutsPath}checkouts{checkoutId}.csv");
                runningTasks.Add(latencyWrapper(experiment.Partitioning.NOrderPartitions,
                    experiment.Partitioning.NStockPartitions,
                    recordEnumerator.Current));
                counter++;
            }
            //It can happen that if we perform less than 64 checkouts, that we don't need to enter this
            //loop but this is cool because MoveNext will return false in this case
            while (recordEnumerator.MoveNext())
            {
                //Wait for any of the tasks to complete and insert a new one
                var anyTaskCompleted = await Task.WhenAny(runningTasks);
                await anyTaskCompleted;
                runningTasks.Add(latencyWrapper(experiment.Partitioning.NOrderPartitions,
                    experiment.Partitioning.NStockPartitions,
                    recordEnumerator.Current));
                runningTasks.Remove(anyTaskCompleted);
                counter++;
            }

            await Task.WhenAll(runningTasks);

            async Task<int> latencyWrapper(int nrOrderPartitions, int nrStockPartitions, CheckoutParameterAccess chkParam)
            {
                var orderPartition = Convert.ToInt32(chkParam.ChkParam.OrderId) % nrOrderPartitions;
                var latencyWatch = new Stopwatch();

                var latencyStartPoint = DateTime.UtcNow;

                latencyWatch.Start();
                try
                {
                    await orderPartitions[orderPartition].Checkout(chkParam.ChkParam);
                } catch(OrleansCascadingAbortException)
                {
                    Console.WriteLine($"Cascading abort for checkout transaction for order id: {chkParam.ChkParam.OrderId}");
                }
                catch (System.TimeoutException)
                {
                    Console.WriteLine($"Aborting transaction with product id {chkParam.ChkParam.OrderId} because of timeout.");
                }
                catch (OrleansBrokenTransactionLockException)
                {
                    Console.WriteLine($"A previous transaction lock has been aborted and thus the lock for this transaction has" +
                        $"been abandoned. checkout transaction with id: {chkParam.ChkParam.OrderId}");
                }
                latencyWatch.Stop();
                latencyStrategy?.CollectLatency(latencyWatch);
                througputStategy?.CollectThroughput(DateTime.UtcNow);
                return 0;
            }
        }

        public async Task ExecuteUpdateProductWorker(int workerId, int globalWorkerId, Barrier barrier)
        {
            //(1) create a connection to the file base using the passed id
            using var reader = new StreamReader(Path.Combine(directory, $"updateProducts{workerId}.csv"));
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<UpdateProductParameterMap>();
            //This is only loading an iterator over the file.
            //It will be loaded into memory only record per record
            var records = csv.GetRecords<UpdateProductParameter>();

            Dictionary<int, IProductActorTransOrl> productPartitions = Enumerable
                .Range(0, experiment.Partitioning.NProductPartitions)
                .Select(nr => (nr, clusterClient.GetGrain<IProductActorTransOrl>(nr)))
                .ToDictionary(kp => kp.nr, kp => kp.Item2);

            //This line needs some comments because as simple as it looks, this might potentially 
            //be a bottleneck. Each record is being read one by one to avoid loading the whole file
            //into memory. Thus, the ingestion of transactions to the system is dependent on how fast 
            //the reading process from the hard disk can be performed. 
            //The speed of this reading process here is directly influencing the performance of the 
            //experiments overall.
            List<Task> tasks = new();
            using IEnumerator<UpdateProductParameter> recordEnumerator = records.GetEnumerator();
            //We first fill the tasks with 64 tasks and then we start the loop to simply enter a new task anytime that one has finished
            //The order of the check is important here, we do only increment the iterator if we are still within the window of 64 transactions

            //Entry point for Throughput measurement
            barrier.SignalAndWait();
            througputStategy?.AddStartPoint(DateTime.UtcNow);
            int counter = 0;
            while (tasks.Count < 100 && recordEnumerator.MoveNext())
            {
                tasks.Add(latencyWrapper(experiment.Partitioning.NProductPartitions,
                    experiment.Partitioning.NStockPartitions, recordEnumerator.Current));
                counter++;
            }
            //It can happen that if we perform less than 64 checkouts, that we don't need to enter this
            //loop but this is cool because MoveNext will return false in this case
            while (recordEnumerator.MoveNext())
            {
                //Wait for any of the tasks to complete and insert a new one
                Task anyTaskCompleted = await Task.WhenAny(tasks);
                await anyTaskCompleted;
                //counter++;
                //Console.WriteLine($"Transaction finished updateproduct: {counter}");
                tasks.Add(latencyWrapper(experiment.Partitioning.NProductPartitions,
                    experiment.Partitioning.NStockPartitions,
                    recordEnumerator.Current));
                tasks.Remove(anyTaskCompleted);
                counter++;
            }

            await Task.WhenAll(tasks);

            async Task latencyWrapper(int nrProductPartitions,
                int nrStockPartitions,
                UpdateProductParameter updateProduct)
            {
                var productPartition = Convert.ToInt32(updateProduct.Product.product_id) % nrProductPartitions;
                var latencyWatch = new Stopwatch();


                var elapsedStartPoint = DateTime.UtcNow;
                latencyWatch.Start();
                try
                {
                    await productPartitions[productPartition].UpdateProduct(updateProduct);
                } catch (OrleansCascadingAbortException)
                {
                    Console.WriteLine($"Cascading abort for update product with product id {updateProduct.Product.product_id}");
                } catch (System.TimeoutException)
                {
                    Console.WriteLine($"Aborting transaction with product id {updateProduct.Product.product_id} because of timeout.");
                }
                catch (OrleansBrokenTransactionLockException)
                {
                    Console.WriteLine($"A previous transaction lock has been aborted and thus the lock for this transaction has" +
                        $"been abandoned. updateproduct transaction with id: {updateProduct.Product.product_id}");
                }
                latencyWatch.Stop();
                latencyStrategy?.CollectLatency(latencyWatch);
                througputStategy?.CollectThroughput(DateTime.UtcNow);
                //Console.WriteLine($"I have completed updateproduct: {updateProduct.Product.product_id}");
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
            var range = Enumerable.Range(0, Math.Max(
                            experiment.WorkersInformation.AmountCheckoutWorkers,
                            experiment.WorkersInformation.AmountUpdateProductWorkers));
            Barrier barrier = new Barrier(experiment.WorkersInformation.AmountCheckoutWorkers +
                            experiment.WorkersInformation.AmountUpdateProductWorkers);
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
            await Task.WhenAll(tasks);
            barrier.Dispose();
        }
    }
}
