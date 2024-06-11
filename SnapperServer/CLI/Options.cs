using CommandLine;
using System.Text.Json.Serialization;

namespace SnapperServer.CLI
{
    internal class Options
    {
        [Option('l',"local",Required = false, Default = false, HelpText = "Start server local or in the cloud. Default: local.")]
        public bool IsLocal { get; set; }

        [Option("extended",Required = false, Default = false, HelpText = "Start extended version of Snapper. Default: not extended.")]
        public bool IsExtended { get; set; }

        [Option("eventual",Required = false, Default = false, HelpText = "Start eventual consistent Orleans Server. Default: not set.")]
        public bool IsEventual { get; set; }

        [Option("snapper",Required=false,Default =false,HelpText = "Start normal version of Snapper. Default: not set.")]
        public bool IsSnapper { get; set; }

        [Option("orleansTrans",Required =false,Default = false, HelpText = "Start Benchmark implemented with Orleans Transactions. Default: not set.")]
        public bool IsOrleans { get; set; }

        [Option("cpu",Required =false,Default = 4,HelpText = "Sets the amount of cores used. Default: 4.")]
        public int NumCpu { get; set; }

        [Option("numCoord", Required = false, Default = 8, HelpText = "Sets the number of Snapper coordinators if used with extended or snapper benchmark.")]
        public int NumberCoord { get; set; }

        [Option("batchSize", Required = false, Default = 100, HelpText = "Sets the size of batches processed by the scheduling engine.")]
        public int BatchSize { get; set; }

        [Option("numScheduleCoord", Required = false, Default = 1, HelpText = "Sets the amount of Schedule Coordinators used when using extended benchmark.")]
        public int NumScheduleCoord { get; set; }
    }
}
