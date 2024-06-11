using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.ExperimentsModel
{
    public class SSHInfo
    {
        public required string Ip { get; set; }

        public required string Password { get; set; }

        public required string Username { get; set; }
    }
}
