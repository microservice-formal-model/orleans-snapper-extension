using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.Control
{
    internal interface IBenchmarkControl
    {
        void Start();

        void Stop();
    }
}
