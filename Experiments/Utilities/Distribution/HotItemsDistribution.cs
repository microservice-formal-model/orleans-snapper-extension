using System.Diagnostics;
using static Experiments.ExperimentsModel.Distribution;

namespace Experiments.Utilities.Distribution
{
    public class HotItemsDistribution : IProcessedDistribution
    {
        internal class RightOpenedInterval
        {
            public int lower;

            public int upper;

            public RightOpenedInterval(int lower, int upper)
            {
                this.lower = lower;
                this.upper = upper;
            }

            public bool Contains(int item)
            {
                return item >= lower && item < upper;
            }

            public int Count()
            {
                return (upper - lower) - 1;
            }

            public List<int> ToList()
            {
                List<int> list = new();
                for(int i = lower; i < upper; i++)
                {
                    list.Add(i);
                }
                return list;
            }

            public override string ToString()
            {
                return $"[{lower},{upper})";
            }
        }
        /// <summary>
        /// This stores the probability for choosing a hot item set based 
        /// on their probabilities. for example, if we have two hot items
        /// sets I1,I2 with p(I1)=0.99 and p(I2)=0.01, then we store two
        /// key value pairs (1,[0,9900)),(2,[9900,10000))
        /// </summary>
        private readonly Dictionary<int, RightOpenedInterval> SetIdsProbabilities;

        private readonly Dictionary<int, List<int>> SetIdsMembers;

        public HotItemsDistribution(List<ItemSet> itemSets, List<ProbabilityRef> itemProbabilities)
        {
            if(!(itemProbabilities.Select(i => i.Probability).Aggregate(0.0m,(acc,i) => acc + i) == 1.0m))
            {
                throw new InvalidOperationException("The intended Hot item set distribution probabilities do not sum up to 1.");
            }
            SetIdsMembers = new();
            SetIdsProbabilities = new();
            //For each item, we find the according probability and put it into the according dictionaries
            int currentUpperBound = 0;
            foreach (ItemSet itemSet in itemSets)
            {
                ProbabilityRef? probability = itemProbabilities.Find(p => p.Id == itemSet.Id) ?? 
                    throw new InvalidOperationException($"Probability for set with id:{itemSet.Id} not defined.");
                //To allow probabilities up to 4 digit precision, we multiply by ten-thousand
                //This means, that if we have 0.01 then the value will be 100.
                var convertedProbability = Convert.ToInt32(probability.Probability * 10000.0m);
                RightOpenedInterval roi = new(currentUpperBound, currentUpperBound + convertedProbability);
                SetIdsProbabilities.Add(itemSet.Id, roi);
                //Determine the products that are included into this probability
                if(itemSet.Pick == ItemSet.PickType.RANGE)
                {
                    var lower = itemSet.Items.First();
                    var upper = itemSet.Items.Last();
                    SetIdsMembers.Add(itemSet.Id, Enumerable.Range(lower, (upper - lower) + 1).ToList());
                }
                else
                {
                    SetIdsMembers.Add(itemSet.Id, new());
                    foreach(int id in itemSet.Items)
                    {
                        SetIdsMembers[itemSet.Id].Add(id);
                    }
                }
                currentUpperBound = roi.upper;
            }
        }

        public override string ToString()
        {
            return $"Probabilities: {string.Join(",", SetIdsProbabilities.ToList().Select(kvp => $"set: {kvp.Key}, interval: {kvp.Value}"))}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="without"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public int GetSample(List<int> without)
        {
            var unavailableSets = SetIdsMembers
                .Where(s => s.Value.TrueForAll(i => without.Contains(i)))
                .Select(kvp => kvp.Key);

            var allRelevantProbabilities = SetIdsProbabilities
                .Where(s => !unavailableSets.Contains(s.Key));

            var possibleProbabilities = allRelevantProbabilities
                .Select(sp => sp.Value.ToList())
                .Aggregate(new List<int>(), (acc, v) => acc.Concat(v).ToList());

            var diceRoleIndex = new Random().Next(0, possibleProbabilities.Count);
            var diceRole = possibleProbabilities[diceRoleIndex];

            var set = allRelevantProbabilities.First(rsp => rsp.Value.Contains(diceRole));

            IEnumerable<int> releveantSetMembers = SetIdsMembers[set.Key].Where(sim => !without.Contains(sim)) 
                ?? throw new Exception ();

            var pickIndex = new Random().Next(0, releveantSetMembers.Count());

            return releveantSetMembers.ElementAt(pickIndex);
        }
        
    }
}
