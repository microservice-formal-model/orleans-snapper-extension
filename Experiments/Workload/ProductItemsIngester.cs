using Common.Entity;
using CsvHelper;
using Experiments.ExperimentsModel;
using Experiments.Workload.PropertyMaps;
using Marketplace.Grains.Orleans.ActorInterfaces;
using Marketplace.Grains.TransactionalOrleans.ActorInterfaces;
using Marketplace.Interface.Extended;
using Marketplace.Interfaces;
using System.Diagnostics;
using System.Globalization;

namespace Experiments.Workload
{
    public class ProductItemsIngester
    {
        public static async Task IngestItemsAndProducts(IClusterClient clusterClient,
            Partitioning partitioning,
            string path, BenchmarkType benchmarkType,
            ITransactionClient? transactionClient)
        {
            //For minimal calls to the cluster for ingestion we store each grain in a Map of grain that will be accessed 
            Dictionary<int, IProductActor>? productPartitionMapSnapper = null;
            Dictionary<int, IProductActorExt>? productPartitionMapExtended = null;
            Dictionary<int, IProductActorOrleans>? productPartitionMapEventual = null;
            Dictionary<int, IProductActorTransOrl>? productPartitonMapTransaction = null;

            Dictionary<int, IStockActor>? stockPartitionMapSnapper = null;
            Dictionary<int, IStockActorExt>? stockPartitionMapExtended = null;
            Dictionary<int, IStockActorOrleans>? stockPartitionMapEventual = null;
            Dictionary<int, IStockActorTransOrl>? stockPartitionMapTransaction = null;

            if (benchmarkType == BenchmarkType.SNAPPER)
            {
                productPartitionMapSnapper =
                    Enumerable.Range(0, partitioning.NProductPartitions)
                    .Select(i => (i, clusterClient.GetGrain<IProductActor>(i)))
                    .ToDictionary(kv => kv.i, kv => kv.Item2);

                stockPartitionMapSnapper =
                    Enumerable.Range(0, partitioning.NStockPartitions)
                    .Select(i => (i, clusterClient.GetGrain<IStockActor>(i)))
                    .ToDictionary(kv => kv.i, kv => kv.Item2);
            }
            else if(benchmarkType == BenchmarkType.EXTENDED)
            {
                productPartitionMapExtended = Enumerable.Range(0, partitioning.NProductPartitions)
                    .Select(i => (i, clusterClient.GetGrain<IProductActorExt>(i)))
                    .ToDictionary(kv => kv.i, kv => kv.Item2);

                stockPartitionMapExtended = Enumerable.Range(0, partitioning.NStockPartitions)
                    .Select(i => (i, clusterClient.GetGrain<IStockActorExt>(i)))
                    .ToDictionary(kv => kv.i, kv => kv.Item2);
            }
            else if(benchmarkType == BenchmarkType.EVENTUAL)
            {
                productPartitionMapEventual = Enumerable.Range(0, partitioning.NProductPartitions)
                    .Select(i => (i, clusterClient.GetGrain<IProductActorOrleans>(i)))
                    .ToDictionary(kv => kv.i, kv => kv.Item2);

                stockPartitionMapEventual = Enumerable.Range(0, partitioning.NStockPartitions)
                    .Select(i => (i, clusterClient.GetGrain<IStockActorOrleans>(i)))
                    .ToDictionary(kv => kv.i, kv => kv.Item2);
            }
            else if(benchmarkType == BenchmarkType.TRANSACTIONS)
            {
                productPartitonMapTransaction = Enumerable.Range(0, partitioning.NProductPartitions)
                    .Select(i => (i, clusterClient.GetGrain<IProductActorTransOrl>(i)))
                    .ToDictionary(kv => kv.i, kv => kv.Item2);

                stockPartitionMapTransaction = Enumerable.Range(0, partitioning.NStockPartitions)
                    .Select(i => (i, clusterClient.GetGrain<IStockActorTransOrl>(i)))
                    .ToDictionary(kv => kv.i, kv => kv.Item2);
            }

            using var itReaderStream = new StreamReader(Path.Combine(path,"items.csv"));
            using var prodReaderStream = new StreamReader(Path.Combine(path, "products.csv"));
            using var csvReaderIt = new CsvReader(itReaderStream, CultureInfo.InvariantCulture);
            using var csvReaderProd = new CsvReader(prodReaderStream, CultureInfo.InvariantCulture);
            csvReaderIt.Context.RegisterClassMap<StockItemMap>();
            csvReaderProd.Context.RegisterClassMap<ProductMap>();

            List<Task> tasks = new();
            foreach (var item in csvReaderIt.GetRecords<StockItem>())
            {
                var partition = item.product_id % partitioning.NStockPartitions;
                if(stockPartitionMapSnapper != null)
                {
                    tasks.Add(stockPartitionMapSnapper[Convert.ToInt32(partition)].AddItem(item));
                }
                else if(stockPartitionMapExtended != null)
                {
                    tasks.Add(stockPartitionMapExtended[Convert.ToInt32(partition)].AddItem(item));
                }
                else if(stockPartitionMapEventual != null)
                {
                    tasks.Add(stockPartitionMapEventual[Convert.ToInt32(partition)].AddItem(item));
                }
                else if(stockPartitionMapTransaction != null)
                {
                    if (transactionClient != null)
                    {
                        tasks.Add(stockPartitionMapTransaction[Convert.ToInt32(partition)].AddItem(item));
                    }
                }

            }

            if(benchmarkType == BenchmarkType.TRANSACTIONS)
            {
                await Task.WhenAll(tasks);
                tasks.Clear();
            }

            foreach (var prod in csvReaderProd.GetRecords<Product>())
            {
                var partition = prod.product_id % partitioning.NProductPartitions;
                if(productPartitionMapSnapper != null)
                {
                    tasks.Add(productPartitionMapSnapper[Convert.ToInt32(partition)].AddProduct(prod));
                }
                else if(productPartitionMapExtended != null)
                {
                    tasks.Add(productPartitionMapExtended[Convert.ToInt32(partition)].AddProduct(prod));
                }
                else if(productPartitionMapEventual != null) 
                {
                    tasks.Add(productPartitionMapEventual[Convert.ToInt32(partition)].AddProduct(prod));
                }
                else if(productPartitonMapTransaction != null)
                {
                    if (transactionClient != null)
                    {
                        tasks.Add(productPartitonMapTransaction[Convert.ToInt32(partition)].AddProduct(prod));
                    }
                }
            }
            await Task.WhenAll(tasks);
        }
    }
}
