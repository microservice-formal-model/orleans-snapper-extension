namespace ExtendedSnapperLibrary
{
    [GenerateSerializer]
    public class TransactionResult
    {
        [Id(0)]
        public readonly object resultObj;

        public TransactionResult(object resultObj) { this.resultObj = resultObj; }
    }
}