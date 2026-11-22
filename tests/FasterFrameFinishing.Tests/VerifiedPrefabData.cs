using System.Collections.Generic;
using FasterFrameFinishing.Core;

namespace FasterFrameFinishing.Tests;

/// <summary>
/// Build-state data extracted from the shipped prefabs of Stationeers 0.2.6429.27802
/// (rocketstation_Data/resources.assets) with tools/dump_frame_states.py. Values are copied
/// verbatim so tests exercise the matcher against the real layout, not an invented one.
/// </summary>
internal static class VerifiedPrefabData
{
    public static BuildStateDescriptor S(int i, string entry, string entry2, int qty2, float time) =>
        new(i, entry, entry == "ItemScrewdriver", entry == "ItemWeldingTorch", entry2, entry2 == "ItemPlasticSheets", qty2, time);

    /// <summary>A non-frame structure that also uses welder + steel sheets (one of 138 such prefabs).</summary>
    public static readonly IReadOnlyList<BuildStateDescriptor> StructureAirlockLike = new[]
    {
        S(0, "ItemKitAirlock", null, 0, 0.5f),
        S(1, "ItemWeldingTorch", "ItemSteelSheets", 2, 1.0f),
    };

    /// <summary>Steel frame: 4 states; state 3 is the plastic-sheet finishing stage.</summary>
    public static readonly IReadOnlyList<BuildStateDescriptor> StructureFrame = new[]
    {
        S(0, "ItemSteelFrames", null, 0, 0.5f),
        S(1, "ItemWeldingTorch", "ItemSteelSheets", 1, 1.0f),
        S(2, "ItemWeldingTorch", "ItemSteelSheets", 1, 1.0f),
        S(3, "ItemScrewdriver", "ItemPlasticSheets", 1, 2.0f),
    };

    /// <summary>Iron frame: still the older 3-state layout, no finishing stage.</summary>
    public static readonly IReadOnlyList<BuildStateDescriptor> StructureFrameIron = new[]
    {
        S(0, "ItemIronFrames", null, 0, 0.5f),
        S(1, "ItemWeldingTorch", "ItemIronSheets", 1, 1.0f),
        S(2, "ItemWeldingTorch", "ItemIronSheets", 1, 1.0f),
    };

    public static readonly IReadOnlyList<BuildStateDescriptor> StructureFrameSide = new[] { S(0, "ItemSteelFrames", null, 0, 0.5f) };
    public static readonly IReadOnlyList<BuildStateDescriptor> StructureFrameCorner = new[] { S(0, "ItemSteelFrames", null, 0, 0.5f) };
    public static readonly IReadOnlyList<BuildStateDescriptor> StructureFrameCornerCut = new[] { S(0, "ItemSteelFrames", null, 0, 0.5f) };

    /// <summary>All StructureFrame* prefabs present in the verified game version.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<BuildStateDescriptor>> AllFrames =
        new Dictionary<string, IReadOnlyList<BuildStateDescriptor>>
        {
            ["StructureFrame"] = StructureFrame,
            ["StructureFrameIron"] = StructureFrameIron,
            ["StructureFrameSide"] = StructureFrameSide,
            ["StructureFrameCorner"] = StructureFrameCorner,
            ["StructureFrameCornerCut"] = StructureFrameCornerCut,
        };
}
