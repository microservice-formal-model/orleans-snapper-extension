namespace ExtendedSnapperLibrary
{
    [GenerateSerializer]
    public class FunctionCall : IEquatable<FunctionCall>
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

        public bool Equals(FunctionCall? other)
        {
            if(other != null)
            {
                return funcName.Equals(other.funcName) &&
                    funcInput.Equals(other.funcInput) &&
                    className.Equals(other.className);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(funcName, funcInput, className);
        }
    }
}