namespace FasterFrameFinishing.Core
{
    /// <summary>
    /// Computes the new <c>ToolBasic.EntryTime</c> for the finishing stage.
    ///
    /// The game derives the hold duration as <c>EntryTime / clamp(toolSpeed, 0.2, 5)</c>
    /// (Structure.AttackWith), so scaling EntryTime keeps every vanilla tool-speed modifier
    /// (suit, hygiene, mood, respawn stress) working on top of this mod.
    /// </summary>
    public static class FinishingTimeCalculator
    {
        /// <summary>
        /// Smallest EntryTime the mod will write. A duration of exactly 0 makes the client skip the
        /// normal hold-to-construct coroutine (InventoryManager calls UseItemComplete directly), so a
        /// small positive value keeps the vanilla screwdriver interaction, sounds and progress flow.
        /// </summary>
        public const float MinimumEntryTime = 0.05f;

        public static float Compute(float vanillaEntryTime, FinishingSettings settings)
        {
            if (settings == null || !settings.Enabled || !IsUsable(vanillaEntryTime))
            {
                return vanillaEntryTime;
            }

            // Never make an already-shorter vanilla value longer than it was.
            float floor = vanillaEntryTime < MinimumEntryTime ? vanillaEntryTime : MinimumEntryTime;

            if (settings.InstantFinish)
            {
                return floor;
            }

            float scaled = vanillaEntryTime / settings.SpeedMultiplier;
            return scaled < floor ? floor : scaled;
        }

        /// <summary>A vanilla value we are willing to scale: finite and strictly positive.</summary>
        public static bool IsUsable(float entryTime) =>
            !float.IsNaN(entryTime) && !float.IsInfinity(entryTime) && entryTime > 0f;
    }
}
