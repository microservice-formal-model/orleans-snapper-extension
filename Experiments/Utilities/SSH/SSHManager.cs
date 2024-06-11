using Renci.SshNet;

namespace Experiments.Utilities.SSH
{
    public class SSHManager
    {
        readonly SshClient sshClient = new SshClient("13.48.72.63", "Administrator", "QLfzo-urRAa8b=m=)E;2$8=jR=lFstv.");
        readonly ScpClient scpClient = new ScpClient("13.48.72.63", "Administrator", "QLfzo-urRAa8b=m=)E;2$8=jR=lFstv.");
     }
}
