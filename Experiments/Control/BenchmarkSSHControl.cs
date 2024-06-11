using Experiments.ExperimentsModel;
using Renci.SshNet;

namespace Experiments.Control
{
    internal class BenchmarkSSHControl : IBenchmarkControl
    {
        private readonly string ip;
        private readonly string username;
        private readonly string password;
        private readonly BenchmarkType bType;
        private readonly int numCpu;
        private readonly int numCoord;

        private SshClient? sshClient;

        private SshCommand? startServer;
        private SshCommand? stopServer;

        public BenchmarkSSHControl(string ip, string username, string password, BenchmarkType bType, int numCpu, int numCoord)
        {
            this.ip = ip;
            this.username = username;
            this.password = password;
            this.bType = bType;
            this.numCpu = numCpu;
            this.numCoord = numCoord;
        }

        public void Connect()
        {
            sshClient?.Disconnect();
            sshClient?.Dispose();
            sshClient = new SshClient(ip,username,password);
            sshClient.Connect();
            Console.WriteLine("Successfully established a connection to remote server.");
            
        }

        public void Start()
        {
            if(sshClient == null)
            {
                throw new InvalidOperationException("No connection established. Start FAILED.");
            }
            string bType = GetBTypeString();
            startServer = sshClient
                .CreateCommand($"cmd.exe /c \"%esnapper%\\bin\\Release\\net7.0\\SnapperServer" +
                $" --{bType} --cpu {numCpu} --numCoord {numCoord}\"");
            Console.WriteLine($"Booting remote server with num cpu: {numCpu} and number coord: {numCoord}.");
            startServer.BeginExecute();
        }


        private string GetBTypeString()
        {
            switch (bType)
            {
                case BenchmarkType.EXTENDED: return "extended";
                case BenchmarkType.SNAPPER: return "snapper";
                case BenchmarkType.EVENTUAL: return "eventual";
                case BenchmarkType.TRANSACTIONS: return "orlTrans";
            }
            return "";
        }

        public void Stop()
        {
            if(startServer == null)
            {
                throw new InvalidOperationException("No SnapperServer isntance started. Stop FAILED.");
            }
            if(sshClient == null)
            {
                throw new InvalidOperationException("No SSH Server started. Stop FAILED.");
            }
            startServer = null;
            stopServer = sshClient.CreateCommand("cmd.exe /c \"%esnapper%\\snapperServClose.bat\"");
            stopServer.Execute();
            startServer?.Dispose();
        }

        public void Disconnect()
        {
            Console.WriteLine("Disconnecting SSH connection.");
            sshClient?.Disconnect();
            sshClient?.Dispose();
        }
    }
}
