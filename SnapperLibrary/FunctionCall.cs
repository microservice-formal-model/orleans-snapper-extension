namespace SnapperLibrary
{
    [GenerateSerializer]
    public class FunctionCall
    {
        [Id(0)]
        public readonly string funcName;
        [Id(1)]
        public readonly object funcInput;
        [Id(2)]
        public readonly Type className;

        public FunctionCall(string funcName, object funcInput, Type className)
        {
            this.funcName = funcName;
            this.funcInput = funcInput;
            this.className = className;
        }
    }
}