using System;
using System.Collections;
using System.Collections.Generic;

using JetBrains.Annotations;

namespace GroboXsd.ReadonlyCollections
{
    public static class ReadonlySet
    {
        [NotNull]
        public static IReadonlySet Create([NotNull] string[] keys)
        {
            return new Impl(ReadonlyHashtable.Create(keys, new int[keys.Length]));
        }

        private class Impl : IReadonlySet
        {
            public Impl([NotNull] IReadonlyHashtable<int> readonlyHashtable)
            {
                this.readonlyHashtable = readonlyHashtable;
            }

            public IEnumerator<string> GetEnumerator()
            {
                foreach(var kvp in readonlyHashtable)
                    yield return kvp.Key;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public bool ContainsKey([NotNull] string key)
            {
                return readonlyHashtable.ContainsKey(key);
            }

            public void ForEach([NotNull] Action<string> action)
            {
                foreach(var kvp in readonlyHashtable)
                    action(kvp.Key);
            }

            private readonly IReadonlyHashtable<int> readonlyHashtable;
        }
    }
}