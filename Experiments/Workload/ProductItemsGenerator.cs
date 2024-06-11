using Bogus;
using Common.Entity;
using CsvHelper;
using Experiments.ExperimentsModel;
using Experiments.Workload.PropertyMaps;
using System.Globalization;

namespace Experiments.Workload
{
    public class ProductItemsGenerator
    {
        private readonly int amountProducts;
        private static int MINPRICE = 1;
        private static int MAXPRICE = 10000;
        private static int MINFREIGHTVALUE = 0;
        private static int MAXFREIGHTVALUE = 5;
        private static int MAXQUANTITY = 1000;
        private static int MINQUANTITY = 10;
        private readonly IClusterClient clusterClient;
        private readonly Partitioning Partitioning;
        private readonly string Location;
        public ProductItemsGenerator(Experiment experiment,string generatedLocation, IClusterClient client)
        {
            this.amountProducts = experiment.AmountProducts;
            clusterClient = client;
            Partitioning = experiment.Partitioning;
            Location = generatedLocation;
        }

        public Dictionary<long,Product> GenerateProductsAndItems()
        {
            var productId = 0;
            var productFaker = new Faker<Product>()
                .StrictMode(true)
                .RuleFor(o => o.product_id, f => productId++)
                .RuleFor(o => o.price, f => f.Random.Number(MINPRICE, MAXPRICE))
                .RuleFor(o => o.sku, f => f.Lorem.Sentence(1))
                .RuleFor(o => o.name, f => f.Commerce.ProductName())
                .RuleFor(o => o.category_name, f => string.Join("", f.Commerce.Categories(f.Random.Number(1, 5))))
                .RuleFor(o => o.freight_value, f => f.Random.Number(MINFREIGHTVALUE, MAXFREIGHTVALUE))
                .RuleFor(o => o.active, f => true)
                .RuleFor(o => o.created_at, f => f.Date.PastDateOnly().ToString())
                .RuleFor(o => o.updated_at, f => f.Date.Recent().ToString())
                .RuleFor(o => o.description, f => f.Commerce.ProductDescription())
                //Seller does not matter for our experiment since we don't include a seller service
                .RuleFor(o => o.seller_id, f => 0)
                .RuleFor(o => o.status, f => f.Commerce.ProductAdjective());

            var itemFaker = (Product prod) => new Faker<StockItem>()
                .StrictMode(true)
                .RuleFor(i => i.created_at, f => DateTime.Now)
                .RuleFor(i => i.updated_at, f => DateTime.Now)
                .RuleFor(i => i.product_id, f => prod.product_id)
                .RuleFor(i => i.active, true)
                .RuleFor(i => i.seller_id, f => prod.seller_id)
                .RuleFor(i => i.data, f => f.Lorem.Sentence())
                .RuleFor(i => i.qty_available, f => f.Random.Number(MINQUANTITY, MAXQUANTITY))
                .RuleFor(i => i.qty_reserved, f => 0)
                .RuleFor(i => i.order_count, f => 0)
                .RuleFor(i => i.ytd, f => 0);

            //For minimal calls to the cluster for ingestion we store each grain in a Map of grain that will be accessed 
            Dictionary<long, Product> products = new();


            //List<Task> tasks = new();
            //for each product, we generate the product and an according item and let them be 
            //inserted asynchronously
            using (var fsProducts = new FileStream(Path.Combine(Location,"products.csv"), FileMode.Create))
            using (var fsItems = new FileStream(Path.Combine(Location,"items.csv"), FileMode.Create))
            using (var productsWriter = new StreamWriter(fsProducts))
            using (var itemsWriter = new StreamWriter(fsItems))
            using (var csvItems = new CsvWriter(itemsWriter, CultureInfo.InvariantCulture))
            using (var csvProducts = new CsvWriter(productsWriter, CultureInfo.InvariantCulture))
            {
                csvProducts.Context.RegisterClassMap<ProductMap>();
                csvProducts.WriteHeader<Product>();
                csvProducts.NextRecord();

                csvItems.Context.RegisterClassMap<StockItemMap>();
                csvItems.WriteHeader<StockItem>();
                csvItems.NextRecord();

                for (var prod_id = 0; prod_id < amountProducts; prod_id++)
                {
                    var product = productFaker.Generate();
                    products.TryAdd(product.product_id, product);
                    csvProducts.WriteRecord(product);
                    csvProducts.NextRecord();

                    GetProductAndInsert(itemFaker,product,csvItems);
                    //Console.WriteLine(JsonConvert.SerializeObject(product));
                    //tasks.Add(productPartitionMap[Convert.ToInt32(product.product_id) % Partitioning.NProductPartitions].AddProduct(product));
                    //tasks.Add(GetProductAndInsert(itemFaker, product, stockPartitionMap, csvItems));
                }
            }

            return products;

            //await Task.WhenAll(tasks);
            //tasks.Clear();
        }

        public void GetProductAndInsert(Func<Product, Faker<StockItem>> faker,
            Product prod,
            CsvWriter csvWriter)
        {
            var item = faker.Invoke(prod).Generate();
            //Console.WriteLine("generating item");
            csvWriter.WriteRecord(item);
            csvWriter.NextRecord();
            //await stockPartitionMap[Convert.ToInt32(prod.product_id) % stockPartitionMap.Count].AddItem(item);

        }
    }
}
