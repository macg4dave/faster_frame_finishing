namespace FasterFrameFinishing.Core
{
    /// <summary>
    /// The only place where "current value -> value to write" is decided. Always computes from
    /// the recorded vanilla value, so calling it any number of times yields the same result.
    /// </summary>
    public static class FinishingApplier
    {
        public static float NextValue<TKey>(
            OriginalValueStore<TKey> store,
            TKey key,
            float currentValue,
            FinishingSettings settings,
            out float vanillaValue)
            where TKey : class
        {
            vanillaValue = store.GetOrRecordOriginal(key, currentValue);
            return FinishingTimeCalculator.Compute(vanillaValue, settings);
        }
    }
}
