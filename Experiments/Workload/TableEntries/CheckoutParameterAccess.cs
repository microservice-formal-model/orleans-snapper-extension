using Common.Snapper.Core;
using Common.Snapper.Order;

namespace Experiments.Workload.TableEntries
{
    public class CheckoutParameterAccess : IEquatable<CheckoutParameterAccess>
    {
        public CheckoutParameter ChkParam { get; set; }

        public Dictionary<ActorID, int> GrainAccesses { get; set; }

        public CheckoutParameterAccess()
        {
            ChkParam = new CheckoutParameter();
            GrainAccesses = new();
        }

        public bool Equals(CheckoutParameterAccess? other)
        {
            return other.ChkParam.Equals(ChkParam) &&
                GrainAccesses.Count == other.GrainAccesses.Count &&
                !GrainAccesses.Except(other.GrainAccesses).Any();
        }

        /// <summary>
        /// Makes a copy that instanciate all objects reshly that need to be instantiated freshly.
        /// Objects that are not being manipulated throughout the transaction process are kept as reference.
        ///</summary>
        /// <returns>Deep copy of <c>ChkParam</c>. Reference copy of <c>GrainAccesses</c>.</returns>
        public CheckoutParameterAccess TransactionCopy()
        {
            return new CheckoutParameterAccess() {
                ChkParam = ChkParam.TransactionCopy(),
                GrainAccesses = GrainAccesses
                };
        }
    }
}
