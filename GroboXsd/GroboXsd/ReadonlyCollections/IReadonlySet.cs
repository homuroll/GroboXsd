using System;
using System.Collections.Generic;

using JetBrains.Annotations;

namespace GroboXsd.ReadonlyCollections
{
    public interface IReadonlySet : IEnumerable<string>
    {
        void ForEach([NotNull] Action<string> action);
        bool ContainsKey([NotNull] string key);
    }
}