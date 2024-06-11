using Orleans;
using System;

namespace Common.Snapper.Core
{
    [GenerateSerializer]
    public class ActorID
    {
        [Id(0)]
        public readonly long id;
        [Id(1)]
        public readonly string className;

        public ActorID(long id, string className)
        {
            this.id = id;
            this.className = className;
        }

        public override bool Equals(object obj)
        {
            var actorID = obj as ActorID;
            if (actorID == null) return false;
            return actorID.id == id && actorID.className == className;
        }

        public override int GetHashCode() => HashCode.Combine(id, className);

        public override string ToString()
        {
            return $"Actor: {id}, with classname: {className}!";
        }
    }
}