using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FasterFrameFinishing.Core
{
    /// <summary>
    /// Remembers the vanilla value for each modified object (by reference identity) so that
    /// re-applying the modification always starts from vanilla and never compounds, and so the
    /// change can be reverted exactly.
    /// </summary>
    public sealed class OriginalValueStore<TKey> where TKey : class
    {
        private readonly Dictionary<TKey, float> _originals = new Dictionary<TKey, float>(ReferenceComparer.Instance);

        public int Count => _originals.Count;

        /// <summary>
        /// Returns the recorded original for <paramref name="key"/>; on first sight records
        /// <paramref name="currentValue"/> as the original and returns it.
        /// </summary>
        public float GetOrRecordOriginal(TKey key, float currentValue)
        {
            if (_originals.TryGetValue(key, out float original))
            {
                return original;
            }

            _originals[key] = currentValue;
            return currentValue;
        }

        public bool TryGetOriginal(TKey key, out float original) => _originals.TryGetValue(key, out original);

        public IEnumerable<KeyValuePair<TKey, float>> Entries => _originals;

        public void Clear() => _originals.Clear();

        private sealed class ReferenceComparer : IEqualityComparer<TKey>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public bool Equals(TKey x, TKey y) => ReferenceEquals(x, y);

            public int GetHashCode(TKey obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
