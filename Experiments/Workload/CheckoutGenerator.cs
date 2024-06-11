using Bogus;
using Common.Entity;
using Common.Snapper.Core;
using Common.Snapper.Order;
using CsvHelper;
using CsvHelper.Configuration;
using Experiments.ExperimentsModel;
using Experiments.Workload.PropertyMaps;
using Experiments.Workload.TableEntries;
using Marketplace.Grains.Message;
using Marketplace.Interfaces;
using MathNet.Numerics.Distributions;
using Orleans;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.Workload
{
    public class CheckoutGenerator
    {
        private readonly Partitioning partitioning;

        private readonly CheckoutInformation checkoutInformation;
        private readonly string location;
        private readonly BenchmarkType benchmarkType;

        public CheckoutGenerator(Experiment experiment,string generatedLocation)
        {
            this.checkoutInformation = experiment.CheckoutInformation;
            this.partitioning = experiment.Partitioning;
            location = generatedLocation;
            benchmarkType = experiment.BenchmarkType;
        }
        public async Task CreateCheckoutWorkload(Utilities.Distribution.IProcessedDistribution sDistribution, Dictionary<long,Product> products)
        {
            var watch = new Stopwatch();
            watch.Start();

            //Creating the distribution based on the number of products.
            //Important: This assumes a growing integer id in the range of the amount of pro

            //We need the following information about checkout transactions:
            //(1) How many products should we checkout ? 
            var checkoutSizeUniformDistribution = new DiscreteUniform(
                checkoutInformation.Size.Start,
                checkoutInformation.Size.End);

            int orderId = 0;
            int cstId = 0;

            var cstCheckoutFaker = new Faker<CustomerCheckout>()
                .RuleFor(cst => cst.CardBrand, f => f.Finance.AccountName())
                .RuleFor(cst => cst.CardExpiration, f => f.Date.FutureDateOnly().ToString())
                .RuleFor(cst => cst.CardHolderName, f => f.Name.FullName())
                .RuleFor(cst => cst.CardNumber, f => f.Finance.CreditCardNumber())
                .RuleFor(cst => cst.CardSecurityNumber, f => f.Finance.CreditCardCvv())
                .RuleFor(cst => cst.City, f => f.Address.City())
                .RuleFor(cst => cst.Complement, f => f.Address.Country())
                .RuleFor(cst => cst.CustomerId, f => cstId++)
                .RuleFor(cst => cst.Installments, f => 0)
                .RuleFor(cst => cst.FirstName, f => f.Name.FirstName())
                .RuleFor(cst => cst.LastName, f => f.Name.LastName())
                .RuleFor(cst => cst.PaymentType, f => "CREDIT_CARD")
                .RuleFor(cst => cst.State, f => f.Address.State())
                .RuleFor(cst => cst.Street, f => f.Address.StreetAddress())
                .RuleFor(cst => cst.ZipCode, f => f.Address.ZipCode());

            var checkoutFaker = (List<CartItem> items) => new Faker<Checkout>()
                .StrictMode(true)
                .RuleFor(ch => ch.CreatedAt, DateTime.Now)
                .RuleFor(ch => ch.CustomerCheckout, f => cstCheckoutFaker.Generate())
                .RuleFor(ch => ch.Items, items);

            var checkoutParamFaker = (List<CartItem> items) => new Faker<CheckoutParameter>()
                .StrictMode(true)
                .RuleFor(ch => ch.OrderId, f => orderId++)
                .RuleFor(ch => ch.Checkout, f => checkoutFaker.Invoke(items).Generate());

            var itemFaker = (Product product) => new Faker<CartItem>()
                .StrictMode(true)
                .RuleFor(it => it.ProductId, f => product.product_id)
                .RuleFor(it => it.UnitPrice, f => product.price)
                .RuleFor(it => it.FreightValue, f => product.freight_value)
                .RuleFor(it => it.ProductName, f => product.name)
                .RuleFor(it => it.Quantity, f => f.Random.Number(1, 10))
                .RuleFor(it => it.SellerId, f => 0);

            List<Task> checkouts = new();
            CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            };  
            //Iterationg over the total amount of Checkout Request for this experiment
            using (var fs = new FileStream(Path.Combine(location,"checkouts.csv"), FileMode.Create))
            using (var sw = new StreamWriter(fs))
            using (var csvWriter = new CsvWriter(sw, config))
            {
                csvWriter.Context.RegisterClassMap<CheckoutParameterAccessMap>();
                csvWriter.WriteHeader<CheckoutParameterAccess>();
                csvWriter.NextRecord();
                for (int i = 0; i < checkoutInformation.TotalAmount; i++)
                {
                    //We have to await these calls incrementally because CSV writing is not concurrent safe
                    GenerateOneCheckout(sDistribution, checkoutSizeUniformDistribution, checkoutParamFaker, itemFaker, products, csvWriter);
                }
            }
            //For all checkouts that have been generated collect the db insertion commands that have been generated
            //this includes a collection of checkouts and a collection of items with composite key of order and item id
            //because every item is unique for each order id, but not for itself.

            watch.Stop();
            Console.WriteLine($"Successfully inserted checkouts into DB, it took: {watch.Elapsed} time.");
        }

        private void GenerateOneCheckout(
            Utilities.Distribution.IProcessedDistribution sDistribution,
            DiscreteUniform checkoutSizeUniformDistribution,
            Func<List<CartItem>, Faker<CheckoutParameter>> checkoutParamFaker,
            Func<Product, Faker<CartItem>> itemFaker,
            Dictionary<long, Product> products,
            CsvWriter writer)
        {
            List<Task<Product>> productReceiveTasks = new();
            List<int> accessedProducts = new();

            //Iterating over a distributed size of the amount of products used in this checkout transaction
            for (int j = 0; j < checkoutSizeUniformDistribution.Sample(); j++)
            {
                //We need to store products that are accessed to avoid duplicate access
                //However, the config should guarantee, that we have more products in Stock then requested through the checkout
                //otherwise, this routine will fail
                var productFound = false;
                int productToAccess;
                //This loop terminates when we find a fresh product
                //However, it will never terminate if the amount of products is not sufficiently big for the checkout size
                while (!productFound)
                {
                    productToAccess = sDistribution.GetSample(accessedProducts);
                    if (!accessedProducts.Contains(productToAccess))
                    {
                        productFound = true;
                        accessedProducts.Add(productToAccess);
                    }
                }
            }
            List<CartItem> cartItems = new();

            foreach (var productId in accessedProducts)
            {
                var product = products[productId];
                cartItems.Add(itemFaker.Invoke(product).Generate());
            }

            var checkout = checkoutParamFaker.Invoke(cartItems).Generate();
            var accessDict = CalculateGrainAccesses(checkout);
            var paramAccess = new CheckoutParameterAccess
            {
                ChkParam = checkout,
                GrainAccesses = accessDict
            };

            writer.WriteRecord(paramAccess);
            writer.NextRecord();
        }

        private Dictionary<ActorID,int> CalculateGrainAccesses(CheckoutParameter chkParam)
        {
            var orderPartition = Convert.ToInt32(chkParam.OrderId) % partitioning.NOrderPartitions;
            var nameSpace = benchmarkType == BenchmarkType.EXTENDED ? "Marketplace.Grains.Extended." : "Marketplace.Grains.";
            var stockActor = benchmarkType == BenchmarkType.SNAPPER ? "StockActor" : "StockActorExt";
            var orderActor = benchmarkType == BenchmarkType.SNAPPER ? "OrderActor" : "OrderActorExt";
            var paymentActor = benchmarkType == BenchmarkType.SNAPPER ? "PaymentActor" : "PaymentActorExt";

            var itemAccessesColl = chkParam.Checkout.Items.Aggregate(new List<KeyValuePair<ActorID, int>>(),
                (acc, next) =>
                {
                    var partitionToInvoke = next.ProductId % partitioning.NStockPartitions;
                    acc.Add(KeyValuePair.Create(new ActorID(partitionToInvoke, nameSpace + stockActor), 3));
                    return acc;
                })
                .Aggregate(new List<KeyValuePair<ActorID, int>>(),
                (acc, next) =>
                {
                    //If the key does already exist we need to add the amount of access
                    if (acc.Exists(kv => kv.Key.Equals(next.Key)))
                    {
                        //Find the index of this element
                        var index = acc.FindIndex(kv => kv.Key.Equals(next.Key));
                        //Merge the two entries because they affect exactly the same actor
                        acc[index] = KeyValuePair.Create(acc[index].Key, acc[index].Value + next.Value);
                        return acc;
                    }
                    else
                    {
                        acc.Add(next);
                        return acc;
                    }
                });

            var dict = itemAccessesColl.ToDictionary(vk => vk.Key, vk => vk.Value);

            dict.Add(new ActorID(chkParam.OrderId % partitioning.NOrderPartitions, nameSpace + orderActor), 2);
            dict.Add(new ActorID(chkParam.OrderId % partitioning.NPaymentPartitions, nameSpace + paymentActor), 1);

            return dict;
        }
    }
}
