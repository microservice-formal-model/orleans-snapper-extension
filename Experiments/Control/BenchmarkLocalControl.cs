using Experiments.ExperimentsModel;
using System.Diagnostics;

namespace Experiments.Control
{
    internal class BenchmarkLocalControl : IBenchmarkControl
    {
        private Process ? dotnet;
        private readonly ProcessStartInfo processStartInfo;
        private bool running;

        public BenchmarkLocalControl(string projectName,
            BenchmarkType typeOfBenchmark,
            int numberOfCpus,
            bool useAmazonDB)
        {
            var esnapperPath = Environment.GetEnvironmentVariable("esnapper");
            if( esnapperPath == null)
            {
                throw new ArgumentException("Environment variable esnapper is not set.");
            }
            string amazon = "";
            if (!useAmazonDB)
            {
                amazon = "-l";
            }
            string arguments;
            if (typeOfBenchmark == BenchmarkType.EXTENDED)
            {
                arguments = $"{amazon} --extended --cpu {numberOfCpus} --numCoord {numberOfCpus}";
            }
            else if (typeOfBenchmark == BenchmarkType.SNAPPER)
            {
                arguments = $"{amazon} --snapper --cpu {numberOfCpus}  --numCoord {numberOfCpus}";
            }
            else if (typeOfBenchmark == BenchmarkType.EVENTUAL)
            {
                arguments = $"{amazon} --eventual --cpu {numberOfCpus}";
            }
            else
            {
                arguments = $"{amazon} --orleansTrans --cpu {numberOfCpus}";
            }
            processStartInfo = new ProcessStartInfo()
            {
                FileName = @"dotnet",
                WorkingDirectory = esnapperPath,
                UseShellExecute = true,
                Arguments = $"run --project {projectName} {arguments}"
            };
            running = false;
        }

        public void Start()
        {
            if (!running)
            {
                dotnet = new Process()
                {
                    StartInfo = processStartInfo
                };
                dotnet.Start();
                running = true;
            }
            else
            {
                throw new InvalidOperationException("Instance of ExtendedSnapperServer already running." +
                    " Did you try to restart the server ? Use Restart instead.");
            }
        }

        public void Stop()
        {
            if(running && dotnet != null)
            {
                dotnet.Kill();
                dotnet.Close();
                running = false;
            }
            else
            {
                throw new InvalidOperationException("Cannot stop server without having started it.");
            }
        }
    }
}
