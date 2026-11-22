namespace FasterFrameFinishing.Core
{
    /// <summary>
    /// Immutable, already-validated snapshot of the user's configuration.
    /// Game-independent so it can be unit tested without Stationeers assemblies.
    /// </summary>
    public sealed class FinishingSettings
    {
        public const float DefaultSpeedMultiplier = 5f;
        public const float MinSpeedMultiplier = 0.1f;
        public const float MaxSpeedMultiplier = 100f;

        public FinishingSettings(bool enabled, float speedMultiplier, bool instantFinish, bool debugLogging)
        {
            Enabled = enabled;
            SpeedMultiplier = SanitizeMultiplier(speedMultiplier);
            InstantFinish = instantFinish;
            DebugLogging = debugLogging;
        }

        public bool Enabled { get; }

        /// <summary>1.0 = vanilla, 5.0 = five times faster. Always within [Min, Max].</summary>
        public float SpeedMultiplier { get; }

        public bool InstantFinish { get; }

        public bool DebugLogging { get; }

        public static FinishingSettings Vanilla => new FinishingSettings(false, 1f, false, false);

        /// <summary>
        /// Maps any float (including NaN/Infinity from a hand-edited config) to a usable multiplier.
        /// Non-finite or non-positive input falls back to the default; finite input is clamped.
        /// </summary>
        public static float SanitizeMultiplier(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                return DefaultSpeedMultiplier;
            }

            if (value < MinSpeedMultiplier)
            {
                return MinSpeedMultiplier;
            }

            return value > MaxSpeedMultiplier ? MaxSpeedMultiplier : value;
        }

        public override string ToString() =>
            $"Enabled={Enabled}, SpeedMultiplier={SpeedMultiplier:0.###}, InstantFinish={InstantFinish}, DebugLogging={DebugLogging}";
    }
}
