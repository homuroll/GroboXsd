using System;
using System.Collections.Generic;

using JetBrains.Annotations;

namespace GroboXsd.ReadonlyCollections
{
    public interface IReadonlyHashtable<T> : IEnumerable<KeyValuePair<string, T>>
    {
        [CanBeNull]
        T this[[NotNull] string key] { get; }

        bool TryGetValue([NotNull] string key, out T value);
        bool TryUpdateValue([NotNull] string key, T value);
        bool TryUpdateValue([NotNull] string key, [NotNull] Func<T, T> updateFactory);
        bool TryUpdateValue([NotNull] string key, [CanBeNull] Func<T, bool> updateFilter, [NotNull] Func<T> valueFactory, out T value);
        void ForEach([NotNull] Action<T> action);
        void ForEach([NotNull] Action<string, T> action);
        void Clear();
        bool ContainsKey([NotNull] string key);

        [NotNull]
        IReadonlyHashtable<TTo> Clone<TTo>([NotNull] Func<T, TTo> selector);
    }
}