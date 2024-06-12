using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Experiments;
using Experiments.Control;
using Experiments.Execution.Latency;
using Experiments.Execution.Result;
using Experiments.Execution.Run;
using Experiments.Execution.Throughput;
using Experiments.ExperimentsModel;
using Experiments.Plotting.Config;
using Experiments.Utilities;
using Experiments.Workload;
using Newtonsoft.Json;
using System.Diagnostics;


string? esnapper = Environment.GetEnvironmentVariable("esnapper");
if (esnapper == null)
{
    throw new InvalidOperationException("Environment variable esnapper not set.");
}
var dir = Path.Combine(esnapper, "Experiments");
var configPath = Path.Combine(dir, "Properties");
var configFilePath = Path.Combine(configPath, "experiments.json");

var plottingConfigFilePath = Path.Combine(configPath, "plotting.json");

using StreamReader r = new(configFilePath);
string json = r.ReadToEnd();

ExperimentsSet? experimentSet = null;
if (json != null)
{
    var maybeExperiments = JsonConvert.DeserializeObject<ExperimentsSet>(json);
    if (maybeExperiments != null) experimentSet = maybeExperiments;
}

using StreamReader plottingReader = new(plottingConfigFilePath);
string plottingJson = plottingReader.ReadToEnd();

Plotting? plts = null;
if (plottingJson != null)
{
    var maybePlt = JsonConvert.DeserializeObject<Plotting>(plottingJson,new JsonSerializerSettings()
    {
        TypeNameHandling = TypeNameHandling.All,
        NullValueHandling = NullValueHandling.Ignore,
    });
    if (maybePlt != null) { plts = maybePlt; }
}


if (experimentSet != null)
{
    //Iterate through all experiments in experiments.json and 
    //perform the requested Experiment
    //Every experiment is boilded down to the following steps:
    // (1) Initialize a new Orleans cluster with the config data of the experiment
    // (2) Generate and Ingest the requested data
    //   (2.1) Generate Items and Products
    //   (2.2) Generate Checkouts and Update Products based on (2.1)
    // (3) Start the experiment
    //   (3.1) Read and distribute event data (2.2) across experiment workers
    //   (3.2) Initialize workers
    // (4) Collect and extract data 
    // (5) Cleanup and refresh grains for next experiment

    //List of results, each experiment will contribute to this
    List<IResult> results = new();
    bool firstExperiment = true;

    foreach (Experiment experiment in experimentSet.Experiments)
    {
        var generatedDir = Path.Combine(dir, experiment.GeneratedLocation);
        //Create directories for the experiment if they don't already exist
        Directory.CreateDirectory(generatedDir);
        Directory.CreateDirectory(Path.Combine(dir, experimentSet.ResultLocation));
        //(1) Initialize a new Orleans cluster with the config data of the experiment
        //(1.1) Start a new Server depending on the benchmarktype for a fresh experiment
        IBenchmarkControl? benchmarkControl = null;

        //If we are using Amazon Dynamo Db then we need to delete the created table and
        //create a new one
        if (experiment.UseAmazonDB && !firstExperiment)
        {
            await DeleteDynamoDBInstance();

        }


        Console.WriteLine(experiment.BenchmarkType);
        Console.WriteLine("Press any button to start the next experiment.");
        Console.ReadLine();
        if (experimentSet.BenchmarkControl)
        {
            if (experiment.IsLocal)
            {
                benchmarkControl = new BenchmarkLocalControl("SnapperServer", experiment.BenchmarkType, experiment.NrCores, experiment.UseAmazonDB);
            }
            else
            {
                Console.WriteLine("Attempting to start the server remotly using SSH.");
                if(experimentSet.Sshinfo == null)
                {
                    throw new InvalidOperationException("SSH info is missing in configuration file.");
                }
                BenchmarkSSHControl sshControl = new(
                    experimentSet.Sshinfo.Ip,
                    experimentSet.Sshinfo.Username,
                    experimentSet.Sshinfo.Password,
                    experiment.BenchmarkType,
                    experiment.NrCores,
                    experiment.NrCores);
                sshControl.Connect();
                benchmarkControl = sshControl;
            }
            benchmarkControl.Start();
            //Give the dotnet time to start
            await Task.Delay(TimeSpan.FromSeconds(10));
            Console.WriteLine("Started external snapper server. Waiting for startup.");
        }

        //(1.2) Connect with a fresh Orleans Cluster to this server
        var manager = new OrleansClientManager(
            new SiloConfig(
                experiment.Partitioning.NStockPartitions,
                experiment.Partitioning.NProductPartitions,
                experiment.Partitioning.NOrderPartitions,
                experiment.Partitioning.NPaymentPartitions,
                experiment.BenchmarkType),
            experiment.UseAmazonDB);

        var client = await manager.GetClientWithRetries();
        await manager.LoadMarketplaceGrains(experiment.BenchmarkType);

        if (client == null)
        {
            Console.WriteLine("Unable to initialize a new Orleans Silo. Abort.");
            return;
        }
        //---------------------------------------------------------------------------
        //(2) Generate and Ingest the requested data into the Grains (Products and Items)  
        //   (2.1) Generate Items and Products
        if (experiment.GenerateLoad)
        {
            var productGenerator = new ProductItemsGenerator(experiment, generatedDir, client);
            var products = productGenerator.GenerateProductsAndItems();

            var distr = experiment.Distribution.GetDistribution(experiment.AmountProducts);

            //   (2.2) Generate Checkouts and Update Products based on (2.1)
            var checkoutsGenerator = new CheckoutGenerator(experiment, generatedDir);
            await checkoutsGenerator.CreateCheckoutWorkload(distr,products);

            var updateProductsGenerator = new UpdateProductsGenerator(experiment, generatedDir, client);
            updateProductsGenerator.CreateUpdateProductWorkload(distr, products);
        }
        //Still needs to ingest the products and items for the pregenerated load 
        Stopwatch ingestionTime = Stopwatch.StartNew();
        await ProductItemsIngester.IngestItemsAndProducts(
            client,
            experiment.Partitioning,
            generatedDir,
            experiment.BenchmarkType,
            manager.TransactionClient ?? null);
        ingestionTime.Stop();
        Console.WriteLine($"Ingestion took {ingestionTime.ElapsedMilliseconds} ms.");

        //---------------------------------------------------------------------------
        // (3) Start the experiment
        //   (3.1) Read and distribute event data (2.2) across experiment workers
        //We need to distribute the load when we want to distribute a pre generated load, 
        // or the load needed to be generated in the first place
        //In the case that we have a generated load and it is already distributed
        //we don't need to distribute anymore
        if (experiment.DistributeGeneratedLoad)
        {
            var workloadDistribution = new WorkloadDistribution(experiment, generatedDir);
            workloadDistribution.DistributeWorkload();
        }
        //Start the threads for executing UpdateProducts and Checkout Requests
        //This bit is critical, because ideally we want to create all the tasks and 
        //make them start working at the same time. 
        var stopWatch = new Stopwatch();
        stopWatch.Start();


        IRunExperiment? experimentExecuter = null;
        switch (experiment.BenchmarkType)
        {
            case BenchmarkType.EXTENDED: experimentExecuter = new RunExtendedExperimentUnit(experiment, client, generatedDir); break;
            case BenchmarkType.SNAPPER: experimentExecuter = new RunSnapperExperimentsUnit(experiment, client, generatedDir); break;
            case BenchmarkType.EVENTUAL: experimentExecuter = new RunEventualExperimentUnit(experiment, client, generatedDir); break;
            case BenchmarkType.TRANSACTIONS: experimentExecuter = new RunTransactionExperimentUnit(experiment, client, generatedDir); break;
        }

        if (experimentExecuter != null)
        {
            await experimentExecuter.RunExperiment(new AverageLatencyStrategy(), new AverageThrougputStrategy(experiment.Runtime));

            var latencyResult = await experimentExecuter.ReceiveLatencyResult();
            var throuputResult = await experimentExecuter.ReceiveThroughputResult();

            latencyResult.GetResult(experiment).PrintResult();
            throuputResult.GetResult(experiment).PrintResult();

            //results.Add(latencyResult.GetResult(experiment));
            //results.Add(throuputResult.GetResult(experiment));
        }

        stopWatch.Stop();
        Console.WriteLine($"The experiment finished in: {stopWatch.ElapsedMilliseconds} ms");
        benchmarkControl?.Stop();

        if (!experiment.IsLocal && experimentSet.BenchmarkControl)
        {
            Console.WriteLine("Disconnecting from remote server.");
            (benchmarkControl as BenchmarkSSHControl)?.Disconnect();
        }

        if (firstExperiment) { firstExperiment = false; }

        Console.WriteLine($"Click any button to continue with the next experiment...");
        Console.ReadLine();
    }

    //Delete the last instance of the experiments
    await DeleteDynamoDBInstance();
    
    /*
    if(plts != null)
    {
        //Create the directory for the results
        var plotLocationDir = Path.Combine(dir,plts.Location);
        Directory.CreateDirectory(plotLocationDir);

        foreach (APlot plot in plts.Plots)
        {
            var jsonPlot = plot.GeneratePlot(results);
            //Write the json plot into the file in the specified location
            var fileNameLocation = Path.Combine(plotLocationDir, plot.FileName + ".json");

            using StreamWriter sw = new(fileNameLocation);
            using JsonWriter jw = new JsonTextWriter(sw);
            JsonSerializer serializer = new();
            serializer.Serialize(jw, jsonPlot);
        }
    }
    */
}

static async Task DeleteDynamoDBInstance()
{
    await Task.CompletedTask;
}