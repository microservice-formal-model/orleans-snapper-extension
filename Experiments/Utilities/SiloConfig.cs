using Experiments.ExperimentsModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.Utilities
{
    public class SiloConfig
    {
        public int NStockPartitions { get; set; }
        public int NProductPartitons { set; get; }

        public int NOrderPartitions { set; get; }

        public int NPaymentPartitions { set; get; }

        public BenchmarkType BenchmarkType { get; set; }

        public SiloConfig(int nStockPartitions, int nProductPartitons, int nOrderPartitions, int nPaymentPartitions, BenchmarkType benchmarkType)
        {
            this.NStockPartitions = nStockPartitions;
            this.NProductPartitons = nProductPartitons;
            this.NOrderPartitions = nOrderPartitions;
            this.NPaymentPartitions = nPaymentPartitions;
            this.BenchmarkType = benchmarkType;
        }
    }
}
