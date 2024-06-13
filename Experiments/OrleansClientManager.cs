using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Microsoft.Extensions.Hosting;
using Orleans.Runtime;
using Experiments.Utilities;
using System.Diagnostics;
using Marketplace.Interfaces;
using Marketplace.Grains.Common;
using Experiments.ExperimentsModel;
using Marketplace.Interface.Extended;
using Marketplace.Grains.Orleans.ActorInterfaces;
using Marketplace.Grains.TransactionalOrleans.ActorInterfaces;

namespace Experiments
{
    public class OrleansClientManager
    {
        private readonly SiloConfig siloConfig;
        private readonly bool useAmazonDB;

        public IClusterClient client;
        public ITransactionClient? TransactionClient { get; set; }

        public OrleansClientManager(SiloConfig siloConfig, bool useAmazonDB)
        {
            this.siloConfig = siloConfig;
            this.useAmazonDB = useAmazonDB;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="initializeAttemptsBeforeFailing"></param>
        /// <returns></returns>
        public async Task<IClusterClient> GetClientWithRetries(int initializeAttemptsBeforeFailing = 5)
        {
            var attemptSucceded = false;
            var count = 0;
            SiloUnavailableException excep = new SiloUnavailableException();
            while(!attemptSucceded && count < initializeAttemptsBeforeFailing)
            {
                try
                {
                    IHost RetrieveHost()
                    {
                        if (useAmazonDB)
                        {
                            Console.WriteLine("using the amazon db configuration to access.");
                            Action<DynamoDBGatewayOptions> dynamoOptions = options =>
                            {
                            };
                            return new HostBuilder()
                                .UseOrleansClient(
                                    client =>
                                    {
                                        client
                                            .UseDynamoDBClustering(dynamoOptions);
                                            
                                    })
                            //.ConfigureLogging(logging => logging.AddConsole())
                            .Build();

                        }
                        else
                        {
                            return new HostBuilder().UseOrleansClient(
                                client =>
                                {
                                    client.UseLocalhostClustering();
                                    if (siloConfig.BenchmarkType == BenchmarkType.TRANSACTIONS)
                                    {
                                        client.UseTransactions();
                                    }
                                })
                            .Build();
                        }
                    }
                    var host = RetrieveHost();
                    await host.StartAsync();
                    Console.WriteLine("Client successfully connected to silo host \n");
                    attemptSucceded = true;
                    client = host.Services.GetRequiredService<IClusterClient>();
                    Console.WriteLine(siloConfig.BenchmarkType);
                    if (siloConfig.BenchmarkType == BenchmarkType.TRANSACTIONS)
                    {
                        Console.WriteLine("Hier");
                        TransactionClient = host.Services.GetRequiredService<ITransactionClient>();
                    }
                    return client;
                } catch(SiloUnavailableException s)
                {
                    count++;
                    excep = s;
                    Console.WriteLine($"Attempt {count + 1} of {initializeAttemptsBeforeFailing} failed to initialize the Orleans client.");
                    Console.WriteLine("Waiting 4 seconds before new attempt.");
                    await Task.Delay(TimeSpan.FromSeconds(4));
                }
                catch (OrleansException)
                {
                    count++;
                    Console.WriteLine($"Attempt {count + 1} of {initializeAttemptsBeforeFailing} failed to initialize the Orleans client.");
                    Console.WriteLine("Waiting 4 seconds before new attempt.");
                    await Task.Delay(TimeSpan.FromSeconds(4));
                }
                catch (InvalidOperationException)
                {
                    //This happens because the deletion of the tabel has not been completed
                    Console.WriteLine("Attempted to connect with DB too early, wait 4 seconds.");
                    await Task.Delay(TimeSpan.FromSeconds(4));
                }
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
            throw excep;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task LoadMarketplaceGrains(BenchmarkType benchmarkType)
        {
            //Checking wether the client is properly initialized
            Debug.Assert(client != null);
            var tasks = new List<Task>();
            //Intialize the Metadata grain first
            //-----------------------------------------------------
            var metadataGrain = client.GetGrain<IMetadataGrain>(0);
            var actrSettings = new ActorSettings
            {
                numProductPartitions = siloConfig.NProductPartitons,
                numStockPartitions = siloConfig.NStockPartitions,
                numOrderPartitions = siloConfig.NOrderPartitions,
                numPaymentPartitions = siloConfig.NPaymentPartitions
            };
            await metadataGrain.Init(actrSettings);
            //-----------------------------------------------------
            //Initialize the amount of ProductGrains
            Console.WriteLine($"I have {actrSettings.numProductPartitions} product partitions, {actrSettings.numPaymentPartitions} payment partitions," +
                $"{actrSettings.numOrderPartitions} order partitions and, {actrSettings.numStockPartitions} stock partitions.");
            for (int i = 0; i < siloConfig.NProductPartitons; i++)
            {
                switch (benchmarkType)
                {
                    case BenchmarkType.SNAPPER: tasks.Add(client.GetGrain<IProductActor>(i).Init()); break;
                    case BenchmarkType.EXTENDED: tasks.Add(client.GetGrain<IProductActorExt>(i).Init()); break;
                    case BenchmarkType.EVENTUAL: tasks.Add(client.GetGrain<IProductActorOrleans>(i).Init()); break;
                    case BenchmarkType.TRANSACTIONS: tasks.Add(client.GetGrain<IProductActorTransOrl>(i).Init()); break;
                }
             }

            for(int i = 0; i < siloConfig.NStockPartitions; i++)
            {
                switch (benchmarkType)
                {
                    case BenchmarkType.SNAPPER: tasks.Add(client.GetGrain<IStockActor>(i).Init()); break;
                    case BenchmarkType.EXTENDED: tasks.Add(client.GetGrain<IStockActorExt>(i).Init()); break;
                }
            }

            for (int i = 0; i < siloConfig.NOrderPartitions; i++)
            {
                switch (benchmarkType)
                {
                    case BenchmarkType.SNAPPER: tasks.Add(client.GetGrain<IOrderActor>(i).Init()); break;
                    case BenchmarkType.EXTENDED: tasks.Add(client.GetGrain<IOrderActorExt>(i).Init()); break;
                    case BenchmarkType.EVENTUAL: tasks.Add(client.GetGrain<IOrderActorOrleans>(i).Init());break;
                    case BenchmarkType.TRANSACTIONS: tasks.Add(client.GetGrain<IOrderActorTransOrl>(i).Init()); break;
                }
            }

            for(int i = 0; i < siloConfig.NPaymentPartitions; i++)
            {
                switch (benchmarkType)
                {
                    case BenchmarkType.SNAPPER: tasks.Add(client.GetGrain<IPaymentActor>(i).Init()); break;
                    case BenchmarkType.EXTENDED: tasks.Add(client.GetGrain<IPaymentActorExt>(i).Init()); break;
                    case BenchmarkType.EVENTUAL: tasks.Add(client.GetGrain<IPaymentActorOrleans>(i).Init());break;
                    case BenchmarkType.TRANSACTIONS: tasks.Add(client.GetGrain<IPaymentActorTransOrl>(i).Init()); break;
                }
            }

            await Task.WhenAll(tasks);
            tasks.Clear();

            Console.WriteLine("Finished loading grains.");
        }
    }
}
