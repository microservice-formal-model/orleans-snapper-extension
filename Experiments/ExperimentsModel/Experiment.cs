using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.ExperimentsModel
{
    public class Experiment
    {
        public required int Id { get; set; }

        public required int NrCores { get; set; }
        public required bool GenerateLoad { get; set; }

        public required int AmountScheduleCoordinators { get; set; }

        public required int Runtime { get; set; }
        public required bool DistributeGeneratedLoad { get; set; }

        public required BenchmarkType BenchmarkType { get; set; }

        public required string GeneratedLocation { get; set; }
        public required bool IsLocal { get; set; }

        public required bool UseAmazonDB { get; set; }

        public required int NrActiveTransactions { get; set; }
        public required int AmountProducts { get; set; }
        public required Partitioning Partitioning { get; set; }

        public required Distribution Distribution { get; set; }

        public required CheckoutInformation CheckoutInformation { get; set; }

        public required UpdateProductInformation UpdateProductInformation { get; set; }

        public required WorkersInformation WorkersInformation { get; set; }


    }
}
