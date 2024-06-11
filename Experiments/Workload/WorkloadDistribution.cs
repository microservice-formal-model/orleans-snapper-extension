using Common.Snapper.Product;
using CsvHelper;
using Experiments.ExperimentsModel;
using Experiments.Workload.PropertyMaps;
using Experiments.Workload.TableEntries;
using System.Globalization;

namespace Experiments.Workload
{
    public class WorkloadDistribution
    {
        private readonly int nrCheckoutsPerCheckoutWorker;
        private readonly int nrUpdateProductsPerWorker;
        private readonly int amountCheckoutWorkers;
        private readonly int amountUpdateWorkers;
        private readonly string preLocationPath;

       
        public WorkloadDistribution(Experiment experiment,string generatedLocation)
        {
            if(experiment.WorkersInformation.AmountUpdateProductWorkers == 0)
            {
                this.nrUpdateProductsPerWorker = 0;
                this.amountUpdateWorkers = 0;
            }
            else
            {
                this.nrUpdateProductsPerWorker = experiment.UpdateProductInformation.TotalAmount / experiment.WorkersInformation.AmountUpdateProductWorkers;
                this.amountUpdateWorkers = experiment.WorkersInformation.AmountUpdateProductWorkers;
            }
            if(experiment.WorkersInformation.AmountCheckoutWorkers == 0)
            {
                this.amountCheckoutWorkers = 0;
                this.nrCheckoutsPerCheckoutWorker = 0;
            }
            else
            {
                this.nrCheckoutsPerCheckoutWorker = experiment.CheckoutInformation.TotalAmount / experiment.WorkersInformation.AmountCheckoutWorkers;
                this.amountCheckoutWorkers = experiment.WorkersInformation.AmountCheckoutWorkers;
            }
            this.preLocationPath = generatedLocation;
        }

        public void DistributeWorkload()
        {
            var chBaseLocation = Path.Combine(preLocationPath,"checkouts.csv");
            var upBaseLocation = Path.Combine(preLocationPath,"updateProducts.csv"); 
            var chLocation = (int nr) => { return Path.Combine(preLocationPath,$"checkouts{nr}.csv");};
            var upLocation = (int nr) => { return Path.Combine(preLocationPath,$"updateProducts{nr}.csv");};
            //If the load can be evenly distributed
            //Example: 4 workers, 102 products, 102 % 4 = 2, 102 / 4 = 25, that means 3 workers get 25 checkouts, 1 worker gets 25 + 2 = 27 checkouts
            //Initialize a map of writers for checkouts
            Dictionary<int, CsvWriter> checkoutWriters = Enumerable.Range(0, amountCheckoutWorkers)
                .Select(nr =>
                {
                    var stream = File.Open(chLocation(nr), FileMode.Create);
                    var csv = new CsvWriter(new StreamWriter(stream), CultureInfo.InvariantCulture);
                    csv.Context.RegisterClassMap<CheckoutParameterAccessMap>();
                    csv.WriteHeader<CheckoutParameterAccess>();
                    csv.NextRecord();
                    return (nr, csv);
                })
                .ToDictionary(tp => tp.nr, tp => tp.csv);
            //Initialize a map of writers for update products
            Dictionary<int, CsvWriter> updateProductsWriters = Enumerable.Range(0, amountUpdateWorkers)
                .Select(nr =>
                {
                    var stream = File.Open(upLocation(nr), FileMode.Create);
                    var csv = new CsvWriter(new StreamWriter(stream), CultureInfo.InvariantCulture);
                    csv.Context.RegisterClassMap<UpdateProductParameterMap>();
                    csv.WriteHeader<UpdateProductParameter>();
                    csv.NextRecord();
                    return (nr, csv);
                })
                .ToDictionary(tp => tp.nr, tp => tp.csv);


            using (var reader = new StreamReader(chBaseLocation))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<CheckoutParameterAccessMap>();
                var records = csv.GetRecords<CheckoutParameterAccess>();

                //Write all the checkouts into their distribution
                foreach (CheckoutParameterAccess record in records)
                {
                    WriteToDistribution(
                            Convert.ToInt32(record.ChkParam.OrderId),
                            amountCheckoutWorkers,
                            nrCheckoutsPerCheckoutWorker,
                            checkoutWriters,
                            record);
                }
                //Write everything to the file by flushing it
                FlushAllAndCloseStreams(checkoutWriters);
                //Then close the stream properly

            }

            using (var reader = new StreamReader(upBaseLocation))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<UpdateProductParameterMap>();
                var records = csv.GetRecords<UpdateProductParameter>();
                var counter = 0;
                foreach (UpdateProductParameter record in records)
                {
                    WriteToDistribution(
                        counter,
                        amountUpdateWorkers,
                        nrUpdateProductsPerWorker,
                        updateProductsWriters,
                        record
                        );
                    counter++;
                }

                FlushAllAndCloseStreams(updateProductsWriters);
            }
        }

        private static void FlushAllAndCloseStreams(Dictionary<int, CsvWriter> writers)
        {
            foreach (var writer in writers.Values)
            {
                writer.Flush();
                writer.Dispose();
            }
        }
        private static void WriteToDistribution<T>(int counter, int workerAmount, int nrEventsPerWorker, Dictionary<int, CsvWriter> writer, T record)
        {
            //If we are in the last distribution and the amount is not even just append it
            var distributionNumber = () => {
                //The distribution is calculated using the id (monotonically increasing) relative to the number of checkouts per woker
                //For example if we are receiving checkout request with id 99 and we have 25 checkouts per worker, this will go to the fourth worker (99/25 = 3)
                //because we start at worker 0.
                int distributionToWriteTo = Convert.ToInt32(counter) / nrEventsPerWorker;
                //However, if the distribution number is exceeding the amount of workers, we have to append it to the last worker
                //For example: If we have 4 workers, and 25 checkouts per worker, and we are looking at checkout with id 113, then (113/25) = 4.
                //But the maximum amount of worker is 0 - 3. So we need to send it to worker 3 (4 - 1).
                if (distributionToWriteTo > (workerAmount - 1)) return workerAmount - 1; else return distributionToWriteTo;
            };
            var distributionNumberInvoked = distributionNumber.Invoke();
            writer[distributionNumberInvoked].WriteRecord(record);
            writer[distributionNumberInvoked].NextRecord();
        }


    }
}
