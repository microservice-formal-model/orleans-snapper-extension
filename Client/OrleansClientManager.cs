using System.Net.NetworkInformation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Utilities;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;

namespace Client
{
    public static class OrleansClientManager
    {
        public static async Task<IClusterClient> GetClient()
        {
            var host = new HostBuilder()
                .UseOrleansClient(
                client => { 
                    client.UseLocalhostClustering();
                    client.Configure<ClientMessagingOptions>(options =>
                    {
                        options.ResponseTimeout = TimeSpan.FromMinutes(30);
                    });
                })
                .ConfigureLogging(logging => logging.AddConsole())
                .Build();
               
            await host.StartAsync();

            Console.WriteLine("Client successfully connected to silo host \n");

            return host.Services.GetRequiredService<IClusterClient>();
        }
    }
}