using CommandLine;
using Common.Entity;
using ExtendedSnapperLibrary.ActorInterface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Orleans.Configuration;
using Orleans.Serialization;
using SnapperLibrary.ActorInterface;
using SnapperServer;
using SnapperServer.CLI;
using System.Diagnostics;
using System.Net;
using Utilities;

var options = Parser.Default
    .ParseArguments<Options>(args)
    .WithParsed(options =>
    {
        Debug.Assert(options.IsOrleans || options.IsSnapper || options.IsExtended || options.IsEventual);
    })
    .Value;

Action<DynamoDBClusteringOptions> dynamoOptions = options =>
{
};

var hostBuilder = () =>
{
    if (!options.IsLocal)
    {
        var getLocalIPAddress = () =>
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Couldn't find the IPv4 address for this node.");
        };
        var serverConfigPath = Path.Combine(Environment.GetEnvironmentVariable("esnapper"),"bin","Release","net7.0","Properties","server-config.json");
        ServerConfiguration? serverConfig = null;
        if (File.Exists(serverConfigPath))
        {
            Console.WriteLine("Here");
            using StreamReader streamReader = new StreamReader(serverConfigPath);
            var json = streamReader.ReadToEnd();
            serverConfig = JsonConvert.DeserializeObject<ServerConfiguration>(json);
        }
        return new HostBuilder()
        .UseOrleans(builder =>
        {
            builder.UseDynamoDBClustering(dynamoOptions);
            builder.ConfigureEndpoints(getLocalIPAddress(), 11111,30000);
            //We need to bind our 
            builder.Configure<EndpointOptions>(options =>
            {
                Console.WriteLine(options.AdvertisedIPAddress);
                Console.WriteLine(options.GatewayListeningEndpoint);
                Console.WriteLine(options.SiloListeningEndpoint);
               // options.GatewayListeningEndpoint = new IPEndPoint(IPAddress.Parse(getLocalIPAddress()), 30_000);
               // options.SiloListeningEndpoint = new IPEndPoint(IPAddress.Parse(getLocalIPAddress()), 11_111);
            });
            if (options.IsOrleans)
            {
                builder.UseTransactions();
                builder.AddMemoryGrainStorageAsDefault();
            }
        })
        .ConfigureServices(services =>
        {
            if ( options.IsSnapper || options.IsExtended)
            {
                //Dependency Inject the Helper class to configure how many snapper
                //cooridnators should be used
                services.AddSingleton(new Helper(options.NumberCoord,options.BatchSize));
            }
            if (options.IsExtended)
            {
                //Dependency inject the shared batch id between the schedule coordinators
                services.AddSingleton(new ConcurrentBid());
            }
        })
        .Build();
    }
    else
    {
        return new HostBuilder()
        .UseOrleans(
            (context, siloBuilder) =>
            {
                siloBuilder.UseLocalhostClustering();
                if (options.IsOrleans)
                {
                    siloBuilder.UseTransactions();
                    siloBuilder.AddMemoryGrainStorageAsDefault();
                }
            })
        .ConfigureServices( services =>
        {
            if (options.IsExtended || options.IsSnapper)
            {
                //Dependency Inject the Helper class to configure how many snapper
                //cooridnators should be used
                services.AddSingleton(new Helper(options.NumCpu,options.BatchSize));
                
            }
            if (options.IsExtended)
            {
                //Dependency inject the shared batch id between the schedule coordinators
                services.AddSingleton(new ConcurrentBid());
            }
        })
        .Build();
    }
};

var esnapper = Environment.GetEnvironmentVariable("esnapper") ?? throw new InvalidOperationException("Environment variable esnapper not set.");

Directory.CreateDirectory(Path.Combine(esnapper,"SnapperServer", "log"));
var logPath = Path.Combine(esnapper, "SnapperServer", "log", "processorAffinity.log");
if(!File.Exists(logPath))
{
   File.Create(Path.Combine(esnapper, "SnapperServer", "log", "processorAffinity.log")).Dispose();
}
using FileStream fs = new(logPath, FileMode.Append, FileAccess.Write);
using StreamWriter log = new(fs);

SetCPU(options.NumCpu);
log.Flush();
using var host = hostBuilder.Invoke();

await host.StartAsync();
Console.WriteLine("Sever for Orleans is started.");

var client = host.Services.GetService<IClusterClient>();
if (client != null)
{
    var grainFactory = host.Services.GetRequiredService<IGrainFactory>();

    if (!options.IsEventual)
    {
        if (options.IsExtended)
        {
            //start of extended snapper
            //Only initialize one to pass the token
            await grainFactory.GetGrain<IExtendedCoordinator>(0).Init();
            //We instantiate a fixed amount of schedule workers
            for(int i = 0; i < options.NumScheduleCoord; i++)
            {
                await grainFactory.GetGrain<IScheduleCoordinator>(i).Init();
            }
        }
        else if (options.IsSnapper)
        {
            //Start of usual snapper
            await grainFactory.GetGrain<ICoordinator>(0).Init();
        }
    }
    Console.WriteLine("Press any button to stop the project...");
    Console.ReadLine();
    await host.StopAsync();
}

void SetCPU(int numCPU)
{
    Console.WriteLine($"---{DateTime.UtcNow}--- Start of Server");
    log.WriteLine($"VERSION-NOT FUNCTIONAL");
    log.WriteLine($"---{DateTime.UtcNow}--- Start of Server");
    Console.WriteLine($"---Number of CPU's: {numCPU}");
    log.WriteLine($"---Number of CPU's: {numCPU}");
    var currentProcess = Process.GetCurrentProcess();
    log.WriteLine($"---Process Name: {currentProcess.ProcessName}");
    log.WriteLine($"---Process meta info: id {currentProcess.Id}");
    var str = GetProcessorAffinityString(numCPU);
    log.WriteLine($"affinity bitstring created: {str}");
    Console.WriteLine($"affinity bitstring created: {str}");
    var serverProcessorAffinity = Convert.ToInt64(str, 2);     // server uses the highest n bits
    log.WriteLine($"Converted to Base 10: {serverProcessorAffinity}");
    Console.WriteLine($"affinity bitstring created: {str}");
    currentProcess.ProcessorAffinity = (IntPtr)serverProcessorAffinity;
    var currentProcessAfter = Process.GetCurrentProcess();
    log.WriteLine($"Affinity after fetching the process a second time: {currentProcessAfter.ProcessorAffinity}");
    log.WriteLine("----------------------------------------");
    Console.WriteLine("----------------------------------------");
}

static string GetProcessorAffinityString(int numCPU)
{
    var str = "";
    if(numCPU > Environment.ProcessorCount)
    {
        throw new InvalidOperationException($"Number of processors on this machine is not sufficient to set for {numCPU}," +
            $"maximum is {Environment.ProcessorCount}.");
    }
    for (int i = 0; i < Environment.ProcessorCount; i++)
    {
        if (i < numCPU) str += "1";
        else str += "0";
    }
    //Reverse to follow C# guideline
    char[] charArray = str.ToCharArray();
    Array.Reverse(charArray);
    return new string(charArray);
}
