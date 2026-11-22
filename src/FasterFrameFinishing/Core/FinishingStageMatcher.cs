using System.Collections.Generic;

namespace FasterFrameFinishing.Core
{
    public enum FinishingStageMatchKind
    {
        /// <summary>Prefab is not a steel-frame variant; ignored silently.</summary>
        NotAFrame,

        /// <summary>Frame variant without a plastic-sheet finishing stage (e.g. iron frame, side/corner pieces).</summary>
        NoFinishingStage,

        /// <summary>A plastic + screwdriver state exists but not where expected; skipped to fail safe.</summary>
        UnexpectedLayout,

        /// <summary>Exactly one finishing stage was found and is safe to modify.</summary>
        Matched,
    }

    public sealed class FinishingStageMatch
    {
        public FinishingStageMatch(FinishingStageMatchKind kind, int stateIndex, string reason)
        {
            Kind = kind;
            StateIndex = stateIndex;
            Reason = reason;
        }

        public FinishingStageMatchKind Kind { get; }

        /// <summary>Index into Structure.BuildStates, or -1 when nothing matched.</summary>
        public int StateIndex { get; }

        public string Reason { get; }
    }

    /// <summary>
    /// Identifies the plastic-sheet finishing stage of steel frames.
    ///
    /// Verified against game version 0.2.6429.27802 (resources.assets): of 736 structure prefabs,
    /// exactly one build state uses ItemScrewdriver + ItemPlasticSheets: StructureFrame.BuildStates[3],
    /// the final of four states. Matching is by tool types and position, not by a hard-coded index
    /// alone, so other structures that use plastic sheets (composite walls, padded walls, doors, ...)
    /// with other tools are never touched.
    /// </summary>
    public static class FinishingStageMatcher
    {
        /// <summary>Steel-frame family prefix. Excludes StructurePictureFrame* and StructureSuitStorageFrame.</summary>
        public const string FramePrefabPrefix = "StructureFrame";

        /// <summary>Index the stage had when this mod was written (fourth state). Informational only.</summary>
        public const int ExpectedStateIndex = 3;

        public static bool IsFrameFamily(string prefabName) =>
            prefabName != null && prefabName.StartsWith(FramePrefabPrefix, System.StringComparison.Ordinal);

        public static bool IsFinishingState(BuildStateDescriptor state) =>
            state != null && state.ToolEntryIsScrewdriver && state.ToolEntry2IsPlasticSheet;

        public static FinishingStageMatch Evaluate(string prefabName, IReadOnlyList<BuildStateDescriptor> states)
        {
            if (!IsFrameFamily(prefabName))
            {
                return new FinishingStageMatch(FinishingStageMatchKind.NotAFrame, -1, "not a StructureFrame* prefab");
            }

            if (states == null || states.Count == 0)
            {
                return new FinishingStageMatch(FinishingStageMatchKind.NoFinishingStage, -1, "no build states");
            }

            int found = -1;
            int count = 0;
            for (int i = 0; i < states.Count; i++)
            {
                if (IsFinishingState(states[i]))
                {
                    found = i;
                    count++;
                }
            }

            if (count == 0)
            {
                return new FinishingStageMatch(FinishingStageMatchKind.NoFinishingStage, -1,
                    $"{states.Count} build state(s), none use screwdriver + plastic sheets");
            }

            if (count > 1)
            {
                return new FinishingStageMatch(FinishingStageMatchKind.UnexpectedLayout, -1,
                    $"{count} screwdriver + plastic-sheet states found; expected exactly one");
            }

            if (found == 0)
            {
                return new FinishingStageMatch(FinishingStageMatchKind.UnexpectedLayout, -1,
                    "screwdriver + plastic-sheet state is the placement state (index 0)");
            }

            if (found != states.Count - 1)
            {
                return new FinishingStageMatch(FinishingStageMatchKind.UnexpectedLayout, -1,
                    $"screwdriver + plastic-sheet state is index {found} but the final state is {states.Count - 1}");
            }

            string note = found == ExpectedStateIndex
                ? "final state, as expected"
                : $"final state (note: expected index {ExpectedStateIndex}, game layout may have changed)";
            return new FinishingStageMatch(FinishingStageMatchKind.Matched, found, note);
        }
    }
}
