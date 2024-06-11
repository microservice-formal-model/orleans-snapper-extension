using Orleans;
using System;
namespace Marketplace.Grains.Common
{
    [GenerateSerializer]
	public class ActorSettings
	{
        [Id(0)]
		public int numOrderPartitions { get; set; }
        [Id(1)]
        public int numPaymentPartitions { get; set; }
        [Id(2)]
        public int numShipmentPartitions { get; set; }
        [Id(3)]
        public int numProductPartitions { get; set; }
        [Id(4)]
        public int numStockPartitions { get; set; }
        [Id(5)]
        public int numCustomerPartitions { get; set; }

        public ActorSettings() { }

        public static ActorSettings GetDefault()
        {
            return new()
            {
                numCustomerPartitions = 1,
                numOrderPartitions = 1,
                numPaymentPartitions = 1,
                numProductPartitions = 1,
                numShipmentPartitions = 1,
                numStockPartitions = 1
            };
        }

    }
}

