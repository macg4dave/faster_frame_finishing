using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using BepInEx.Logging;
using FasterFrameFinishing.Core;

namespace FasterFrameFinishing.Game
{
    /// <summary>
    /// Speeds up frame welding (iron/steel sheets) and plastic finishing on registered structure prefabs.
    /// State 0 is never modified, so placement time (BuildPlacementTime) stays vanilla.
    ///
    /// Verified game data path (Assembly-CSharp, game 0.2.6429.27802):
    ///   Structure.AllStructurePrefabs : List&lt;Structure&gt;  (registered prefab copies)
    ///   Structure.BuildStates         : List&lt;BuildState&gt;
    ///   BuildState.Tool               : ToolUse (derives ToolBasic)
    ///   ToolBasic.ToolEntry / ToolEntry2 : Item, ToolBasic.EntryQuantity2 : int
    ///   ToolBasic.EntryTime           : float, read only by Structure.AttackWith
    ///                                   (Duration = EntryTime / toolSpeed) and BuildPlacementTime (state 0).
    /// World instances are cloned from these prefabs, and BuildState/ToolUse are [Serializable]
    /// value-copied classes, so editing the prefab before a world loads affects every frame built
    /// or loaded afterwards, and nothing else.
    /// </summary>
    internal sealed class FramePrefabPatcher
    {
        private readonly FinishingSettings _settings;
        private readonly ManualLogSource _log;
        private readonly OriginalValueStore<ToolUse> _originals = new OriginalValueStore<ToolUse>();

        public FramePrefabPatcher(FinishingSettings settings, ManualLogSource log)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>Number of build stages currently modified.</summary>
        public int ModifiedCount => _originals.Count;

        /// <summary>
        /// Safe entry point for the Prefab.OnPrefabsLoaded event. Never throws: any failure is logged
        /// and every change made so far is rolled back, leaving the game in vanilla state.
        /// </summary>
        public void ApplySafely()
        {
            try
            {
                Apply();
            }
            catch (Exception ex)
            {
                _log.LogError("Failed to modify frame finishing time; reverting to vanilla. " +
                              "A game update may have changed construction internals. " + ex);
                RestoreSafely();
            }
        }

        /// <summary>Restores every value this patcher changed. Never throws.</summary>
        public void RestoreSafely()
        {
            try
            {
                foreach (KeyValuePair<ToolUse, float> entry in _originals.Entries)
                {
                    entry.Key.EntryTime = entry.Value;
                }

                if (_originals.Count > 0)
                {
                    _log.LogInfo($"Restored vanilla finishing time on {_originals.Count} build state(s).");
                }

                _originals.Clear();
            }
            catch (Exception ex)
            {
                _log.LogError("Failed to restore vanilla finishing time: " + ex);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Apply()
        {
            List<Structure> prefabs = Structure.AllStructurePrefabs;
            if (prefabs == null || prefabs.Count == 0)
            {
                _log.LogWarning("No structure prefabs are registered; nothing to modify.");
                return;
            }

            int framesSeen = 0;
            int framesModified = 0;
            int stagesModified = 0;
            int skippedUnexpected = 0;

            foreach (Structure structure in prefabs)
            {
                if (structure == null)
                {
                    continue;
                }

                string prefabName = structure.PrefabName;
                if (!FrameStageMatcher.IsFrameFamily(prefabName))
                {
                    continue;
                }

                framesSeen++;
                List<BuildState> states = structure.BuildStates;
                List<BuildStateDescriptor> descriptors = Describe(states);
                FrameStageMatch match = FrameStageMatcher.Evaluate(prefabName, descriptors);

                if (_settings.DebugLogging)
                {
                    _log.LogInfo($"[debug] {prefabName} ({structure.GetType().Name}): {descriptors.Count} build state(s)");
                    foreach (BuildStateDescriptor d in descriptors)
                    {
                        _log.LogInfo($"[debug]   {d}");
                    }

                    _log.LogInfo($"[debug]   -> {match.Kind}: {match.Reason}");
                }

                switch (match.Kind)
                {
                    case FrameMatchKind.Matched:
                        foreach (MatchedStage stage in match.Stages)
                        {
                            ApplyToState(prefabName, states[stage.Index].Tool, descriptors[stage.Index], stage);
                            stagesModified++;
                        }

                        framesModified++;
                        break;
                    case FrameMatchKind.UnexpectedLayout:
                        skippedUnexpected++;
                        _log.LogWarning($"Skipping {prefabName}: {match.Reason}. Left at vanilla timing.");
                        break;
                }
            }

            if (stagesModified == 0)
            {
                _log.LogWarning(
                    $"No frame welding or finishing stage was found ({framesSeen} StructureFrame* prefab(s) inspected, " +
                    $"{skippedUnexpected} with an unexpected layout). The mod is inactive; the game may have changed.");
            }
            else
            {
                _log.LogInfo($"Modified {stagesModified} build stage(s) on {framesModified} frame prefab(s); " +
                             $"{framesSeen - framesModified} other frame variant(s) left unchanged. Placement and deconstruction are unchanged.");
            }
        }

        private void ApplyToState(string prefabName, ToolUse tool, BuildStateDescriptor descriptor, MatchedStage stage)
        {
            float target = FinishingApplier.NextValue(_originals, tool, tool.EntryTime, _settings, out float vanilla);
            tool.EntryTime = target;

            string mode = _settings.InstantFinish ? "instant" : $"x{_settings.SpeedMultiplier.ToString("0.###", CultureInfo.InvariantCulture)}";
            _log.LogInfo(
                $"{prefabName} build state {descriptor.Index} [{stage.Kind}] ({descriptor.ToolEntryName} + {descriptor.EntryQuantity2}x {descriptor.ToolEntry2Name}): " +
                $"EntryTime {vanilla.ToString("0.###", CultureInfo.InvariantCulture)}s -> {target.ToString("0.###", CultureInfo.InvariantCulture)}s ({mode}).");
        }

        private static List<BuildStateDescriptor> Describe(List<BuildState> states)
        {
            var result = new List<BuildStateDescriptor>(states?.Count ?? 0);
            if (states == null)
            {
                return result;
            }

            for (int i = 0; i < states.Count; i++)
            {
                ToolUse tool = states[i]?.Tool;
                if (tool == null)
                {
                    result.Add(new BuildStateDescriptor(i, null, false, false, null, false, 0, 0f));
                    continue;
                }

                Item entry = tool.ToolEntry;
                Item entry2 = tool.ToolEntry2;
                result.Add(new BuildStateDescriptor(
                    i,
                    entry != null ? entry.PrefabName : null,
                    entry is Screwdriver,
                    entry is WeldingTorch,
                    entry2 != null ? entry2.PrefabName : null,
                    entry2 is PlasticSheet,
                    tool.EntryQuantity2,
                    tool.EntryTime));
            }

            return result;
        }
    }
}
