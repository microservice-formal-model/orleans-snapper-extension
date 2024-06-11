using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.ExperimentsModel
{
    public class ExperimentsSet
    {
        public required string ResultLocation { get; set; }
        public SSHInfo? Sshinfo { get; set; }

        public required bool BenchmarkControl { get; set; }
        public required Experiment[] Experiments { get; set; }
    }
}
