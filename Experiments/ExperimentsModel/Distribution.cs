using Experiments.Utilities.Distribution;
using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.ExperimentsModel
{
    public class Distribution
    {
        public enum DistributionType
        {
            UNIFORM,
            ZIPFIAN,
            HOTITEMS
        }

        public class ItemSet
        {
            public enum PickType
            {
                INDIVIDUAL,
                RANGE
            }

            public required int Id { get; set; }

            public required PickType Pick { get; set; }

            public required List<int> Items { get; set; }

        }

        public class ProbabilityRef
        {
            public required int Id { get; set; }

            public required decimal Probability { get; set;}
        }

        public DistributionType Type { get; set; }

        public decimal ZipfianConstant { get; set; }   
        
        public List<ItemSet> ItemSets { get; set; }
        
        public List<ProbabilityRef> Probabilities { get; set; }
        
        public IProcessedDistribution GetDistribution(int amountProducts)
        {
            if(this.Type == DistributionType.UNIFORM)
            {
                return new MathNetDistribution(new DiscreteUniform(0, amountProducts - 1),DistributionType.UNIFORM);
            } else if(this.Type == DistributionType.ZIPFIAN)
            {
                return new MathNetDistribution(new Zipf(decimal.ToDouble(ZipfianConstant), amountProducts, new Random()),DistributionType.ZIPFIAN);
            } else
            {
                return new HotItemsDistribution(this.ItemSets, this.Probabilities);
            }           
        }
    }
}
