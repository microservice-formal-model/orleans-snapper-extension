using Common.Entity;
using CsvHelper.Configuration;
using CsvHelper;
using Experiments.ExperimentsModel;
using Marketplace.Interfaces;
using System.Diagnostics;
using System.Globalization;
using Bogus;
using Experiments.Workload.PropertyMaps;
using Common.Snapper.Product;

namespace Experiments.Workload
{
    public class UpdateProductsGenerator
    {
        private readonly Partitioning Partitioning;
        private readonly UpdateProductInformation UpdateProductInformation;
        private readonly IClusterClient ClusterClient;
        private readonly string Location;
        public UpdateProductsGenerator(Experiment experiment, string generatedLocation, IClusterClient client)
        {
            this.Partitioning = experiment.Partitioning;
            this.UpdateProductInformation = experiment.UpdateProductInformation;
            this.ClusterClient = client;
            this.Location = generatedLocation;
        }

        public void CreateUpdateProductWorkload(Utilities.Distribution.IProcessedDistribution sDistribution, Dictionary<long, Product> products)
        {
            var watch = new Stopwatch();
            watch.Start();

            //Using a Faker instance to fake our UpdateProduct event
            var updateProductFaker = (Product product) => new Faker<UpdateProductParameter>()
                .StrictMode(true)
                .RuleFor(up => up.Product, f => product)
                .RuleFor(up => up.Quantity, f => f.Random.Number(this.UpdateProductInformation.MinimumReplenish, this.UpdateProductInformation.MaximumReplenish));

            //To create a CSV file with a proper header.
            CsvConfiguration config = new(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            };

            using (var fs = new FileStream(Path.Combine(Location, "updateProducts.csv"), FileMode.Create))
            using (var sw = new StreamWriter(fs))
            using (var csvWriter = new CsvWriter(sw, config))
            {
                csvWriter.Context.RegisterClassMap<UpdateProductParameterMap>();
                csvWriter.WriteHeader<UpdateProductParameter>();
                csvWriter.NextRecord();

                for (int i = 0; i < UpdateProductInformation.TotalAmount; i++)
                {
                    var productId = sDistribution.GetSample(new());

                    //awaiting each product iteratively, because writing to the csv file cannot be processed in paralell
                    var product = products[productId];

                    if (product != null)
                    {
                        var updateProductParameter = updateProductFaker.Invoke(product).Generate();
                        csvWriter.WriteRecord(updateProductParameter);
                        csvWriter.NextRecord();
                    }
                }

                watch.Stop();
                Console.WriteLine($"Successfully loaded Update Product Commands. It took {watch.Elapsed} time.");
            }
        }
    }
}
