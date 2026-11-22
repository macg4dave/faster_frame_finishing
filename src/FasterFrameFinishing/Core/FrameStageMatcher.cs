using System;
using System.Collections.Generic;

namespace FasterFrameFinishing.Core
{
    public enum FrameStageKind
    {
        /// <summary>Welding torch + iron or steel sheets (adds a sheet layer to the frame).</summary>
        Welding,

        /// <summary>Screwdriver + plastic sheets (the final cosmetic finish on steel frames).</summary>
        Finishing,
    }

    public enum FrameMatchKind
    {
        /// <summary>Prefab is not a frame variant; ignored silently.</summary>
        NotAFrame,

        /// <summary>Frame variant with no welding or finishing stage (e.g. side/corner pieces).</summary>
        NoMatchingStages,

        /// <summary>Stages exist but not in the expected layout; the whole prefab is skipped to fail safe.</summary>
        UnexpectedLayout,

        /// <summary>One or more stages matched and are safe to modify.</summary>
        Matched,
    }

    public sealed class MatchedStage
    {
        public MatchedStage(int index, FrameStageKind kind)
        {
            Index = index;
            Kind = kind;
        }

        public int Index { get; }

        public FrameStageKind Kind { get; }

        public override string ToString() => $"{Kind} state {Index}";
    }

    public sealed class FrameStageMatch
    {
        public FrameStageMatch(FrameMatchKind kind, IReadOnlyList<MatchedStage> stages, string reason)
        {
            Kind = kind;
            Stages = stages ?? Array.Empty<MatchedStage>();
            Reason = reason;
        }

        public FrameMatchKind Kind { get; }

        /// <summary>Stages to modify, in build order. Empty unless <see cref="Kind"/> is Matched.</summary>
        public IReadOnlyList<MatchedStage> Stages { get; }

        public string Reason { get; }
    }

    /// <summary>
    /// Identifies the frame build stages this mod speeds up: welding iron/steel sheets and the
    /// plastic-sheet finish. Placement (state 0) and every non-frame structure are never matched.
    ///
    /// Verified against game version 0.2.6429.27802 (resources.assets, 736 structure prefabs):
    ///  - StructureFrame:     0 place | 1 weld steel sheet | 2 weld steel sheet | 3 screwdriver + plastic sheet
    ///  - StructureFrameIron: 0 place | 1 weld iron sheet  | 2 weld iron sheet
    ///  - StructureFrameSide/Corner/CornerCut: 0 place only
    /// Welding torch + iron/steel sheets is also used by 138 non-frame structures (airlocks, batteries,
    /// furnaces, ...), so matching is restricted to the StructureFrame* family.
    /// </summary>
    public static class FrameStageMatcher
    {
        /// <summary>Frame family prefix. Excludes StructurePictureFrame* and StructureSuitStorageFrame.</summary>
        public const string FramePrefabPrefix = "StructureFrame";

        /// <summary>Index the finishing stage had when this mod was written (fourth state). Informational only.</summary>
        public const int ExpectedFinishingIndex = 3;

        /// <summary>Sheet materials welded onto frames (verified prefab names; there is no iron-sheet class).</summary>
        public static readonly IReadOnlyCollection<string> FrameSheetPrefabs = new[] { "ItemSteelSheets", "ItemIronSheets" };

        public static bool IsFrameFamily(string prefabName) =>
            prefabName != null && prefabName.StartsWith(FramePrefabPrefix, StringComparison.Ordinal);

        public static bool IsFinishingState(BuildStateDescriptor state) =>
            state != null && state.ToolEntryIsScrewdriver && state.ToolEntry2IsPlasticSheet;

        public static bool IsWeldingState(BuildStateDescriptor state) =>
            state != null && state.ToolEntryIsWeldingTorch && IsFrameSheet(state.ToolEntry2Name);

        private static bool IsFrameSheet(string prefabName)
        {
            foreach (string sheet in FrameSheetPrefabs)
            {
                if (string.Equals(sheet, prefabName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static FrameStageMatch Evaluate(string prefabName, IReadOnlyList<BuildStateDescriptor> states)
        {
            if (!IsFrameFamily(prefabName))
            {
                return new FrameStageMatch(FrameMatchKind.NotAFrame, null, "not a StructureFrame* prefab");
            }

            if (states == null || states.Count == 0)
            {
                return new FrameStageMatch(FrameMatchKind.NoMatchingStages, null, "no build states");
            }

            var matched = new List<MatchedStage>();
            int finishing = -1;
            int finishingCount = 0;

            for (int i = 0; i < states.Count; i++)
            {
                bool weld = IsWeldingState(states[i]);
                bool finish = IsFinishingState(states[i]);

                if ((weld || finish) && i == 0)
                {
                    return Unexpected($"state 0 (placement) looks like a {(weld ? "welding" : "finishing")} stage");
                }

                if (weld)
                {
                    if (finishing >= 0)
                    {
                        return Unexpected($"welding state {i} comes after the finishing state {finishing}");
                    }

                    matched.Add(new MatchedStage(i, FrameStageKind.Welding));
                }
                else if (finish)
                {
                    finishing = i;
                    finishingCount++;
                    matched.Add(new MatchedStage(i, FrameStageKind.Finishing));
                }
            }

            if (finishingCount > 1)
            {
                return Unexpected($"{finishingCount} screwdriver + plastic-sheet states found; expected at most one");
            }

            if (finishing >= 0 && finishing != states.Count - 1)
            {
                return Unexpected($"finishing state is index {finishing} but the final state is {states.Count - 1}");
            }

            if (matched.Count == 0)
            {
                return new FrameStageMatch(FrameMatchKind.NoMatchingStages, null,
                    $"{states.Count} build state(s), none are welding or finishing stages");
            }

            string note = finishing < 0 || finishing == ExpectedFinishingIndex
                ? "layout as expected"
                : $"note: finishing expected at index {ExpectedFinishingIndex}, game layout may have changed";
            return new FrameStageMatch(FrameMatchKind.Matched, matched, note);
        }

        private static FrameStageMatch Unexpected(string reason) =>
            new FrameStageMatch(FrameMatchKind.UnexpectedLayout, null, reason);
    }
}
