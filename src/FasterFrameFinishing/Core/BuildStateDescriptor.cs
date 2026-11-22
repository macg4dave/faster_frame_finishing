namespace FasterFrameFinishing.Core
{
    /// <summary>
    /// Game-independent view of one <c>Assets.Scripts.Objects.BuildState</c>, produced by the game
    /// adapter. Lets matching logic be tested without Unity or Stationeers assemblies.
    /// </summary>
    public sealed class BuildStateDescriptor
    {
        public BuildStateDescriptor(
            int index,
            string toolEntryName,
            bool toolEntryIsScrewdriver,
            bool toolEntryIsWeldingTorch,
            string toolEntry2Name,
            bool toolEntry2IsPlasticSheet,
            int entryQuantity2,
            float entryTime)
        {
            Index = index;
            ToolEntryName = toolEntryName;
            ToolEntryIsScrewdriver = toolEntryIsScrewdriver;
            ToolEntryIsWeldingTorch = toolEntryIsWeldingTorch;
            ToolEntry2Name = toolEntry2Name;
            ToolEntry2IsPlasticSheet = toolEntry2IsPlasticSheet;
            EntryQuantity2 = entryQuantity2;
            EntryTime = entryTime;
        }

        public int Index { get; }

        /// <summary>Prefab name of <c>Tool.ToolEntry</c> (held in the active hand), or null.</summary>
        public string ToolEntryName { get; }

        public bool ToolEntryIsScrewdriver { get; }

        /// <summary><c>Tool.ToolEntry is Assets.Scripts.Objects.Items.WeldingTorch</c>.</summary>
        public bool ToolEntryIsWeldingTorch { get; }

        /// <summary>Prefab name of <c>Tool.ToolEntry2</c> (off-hand material), or null.</summary>
        public string ToolEntry2Name { get; }

        public bool ToolEntry2IsPlasticSheet { get; }

        public int EntryQuantity2 { get; }

        public float EntryTime { get; }

        public override string ToString() =>
            $"state {Index}: {ToolEntryName ?? "-"} + {EntryQuantity2}x {ToolEntry2Name ?? "-"}, EntryTime={EntryTime:0.###}s";
    }
}
