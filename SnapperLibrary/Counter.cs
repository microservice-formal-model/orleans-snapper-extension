namespace SnapperLibrary
{
    [GenerateSerializer]
    internal class Counter
    {
        [Id(0)]
        internal int num;

        internal Counter(int num) => this.num = num;

        internal bool Decrement()
        {
            if (--num == 0) return true;
            return false;
        }
    }
}