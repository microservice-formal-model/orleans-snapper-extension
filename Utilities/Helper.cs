namespace Utilities
{
    public class Helper
    {
        public int numCoordPerGroup;
        public int ScheduleBatchSize;

        public Helper(int numCoordPerGroup, int scheduleBatchSize)
        {
            this.numCoordPerGroup = numCoordPerGroup;
            ScheduleBatchSize = scheduleBatchSize;
        }


        /* Randomly select a coordinator ID in the specified group. */
        public long SelectCoordInGroup(int groupID) => groupID * numCoordPerGroup + new Random().Next(0, numCoordPerGroup);

        public int MapCoordIDtoGroupID(long coordID) => (int)(coordID / numCoordPerGroup);
       
        public long MapCoordIDToNeighborID(long coordID)
        {
            var groupID = MapCoordIDtoGroupID(coordID);
            return groupID * numCoordPerGroup + (coordID + 1) % numCoordPerGroup;
        }

        public string ChangePrintFormat(double n, int num) => Math.Round(n, num).ToString().Replace(',', '.');
    }
}