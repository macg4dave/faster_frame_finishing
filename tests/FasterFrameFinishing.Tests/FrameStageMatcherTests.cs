using System.Linq;
using FasterFrameFinishing.Core;
using Xunit;
using static FasterFrameFinishing.Tests.VerifiedPrefabData;

namespace FasterFrameFinishing.Tests;

public class FrameStageMatcherTests
{
    private static string Layout(FrameStageMatch m) => string.Join(",", m.Stages.Select(s => $"{s.Index}:{s.Kind}"));

    [Fact]
    public void Steel_frame_welding_stages_and_finishing_stage_match()
    {
        FrameStageMatch m = FrameStageMatcher.Evaluate("StructureFrame", StructureFrame);

        Assert.Equal(FrameMatchKind.Matched, m.Kind);
        Assert.Equal("1:Welding,2:Welding,3:Finishing", Layout(m));
        Assert.Equal("layout as expected", m.Reason);
    }

    [Fact]
    public void Iron_frame_welding_stages_match()
    {
        FrameStageMatch m = FrameStageMatcher.Evaluate("StructureFrameIron", StructureFrameIron);

        Assert.Equal(FrameMatchKind.Matched, m.Kind);
        Assert.Equal("1:Welding,2:Welding", Layout(m));
    }

    [Fact]
    public void Placement_state_is_never_matched()
    {
        foreach (var frame in AllFrames)
        {
            Assert.DoesNotContain(FrameStageMatcher.Evaluate(frame.Key, frame.Value).Stages, s => s.Index == 0);
        }
    }

    [Fact]
    public void Across_all_verified_frames_exactly_five_stages_match()
    {
        var all = AllFrames.SelectMany(kv => FrameStageMatcher.Evaluate(kv.Key, kv.Value).Stages.Select(s => (kv.Key, s))).ToList();

        Assert.Equal(5, all.Count);
        Assert.Equal(4, all.Count(x => x.s.Kind == FrameStageKind.Welding));
        Assert.Single(all, x => x.s.Kind == FrameStageKind.Finishing && x.Key == "StructureFrame" && x.s.Index == 3);
    }

    [Theory]
    [InlineData("StructureFrameSide")]
    [InlineData("StructureFrameCorner")]
    [InlineData("StructureFrameCornerCut")]
    public void Single_state_frame_pieces_have_nothing_to_speed_up(string prefab)
    {
        Assert.Equal(FrameMatchKind.NoMatchingStages, FrameStageMatcher.Evaluate(prefab, AllFrames[prefab]).Kind);
    }

    [Theory]
    [InlineData("StructureAirlock")]
    [InlineData("StructureBattery")]
    [InlineData("StructurePictureFrameThinPortraitSmall")]
    [InlineData("StructureSuitStorageFrame")]
    [InlineData(null)]
    public void Non_frame_structures_using_welder_and_sheets_are_ignored(string prefab)
    {
        Assert.Equal(FrameMatchKind.NotAFrame, FrameStageMatcher.Evaluate(prefab, StructureAirlockLike).Kind);
        Assert.Equal(FrameMatchKind.NotAFrame, FrameStageMatcher.Evaluate(prefab, StructureFrame).Kind);
    }

    [Fact]
    public void Welder_with_other_materials_is_not_a_welding_stage()
    {
        var states = new[]
        {
            S(0, "ItemSteelFrames", null, 0, 0.5f),
            S(1, "ItemWeldingTorch", "ItemPlasticSheets", 1, 2f),
            S(2, "ItemWeldingTorch", "ItemGlassSheets", 1, 2f),
            S(3, "ItemCrowbar", "ItemSteelSheets", 1, 2f),
        };
        Assert.Equal(FrameMatchKind.NoMatchingStages, FrameStageMatcher.Evaluate("StructureFrameX", states).Kind);
    }

    [Fact]
    public void Finishing_stage_that_is_not_final_skips_the_whole_frame()
    {
        var states = StructureFrame.Concat(new[] { S(4, "ItemWrench", null, 0, 1f) }).ToArray();
        FrameStageMatch m = FrameStageMatcher.Evaluate("StructureFrame", states);

        Assert.Equal(FrameMatchKind.UnexpectedLayout, m.Kind);
        Assert.Empty(m.Stages);
    }

    [Fact]
    public void Welding_after_finishing_skips_the_whole_frame()
    {
        var states = new[]
        {
            S(0, "ItemSteelFrames", null, 0, 0.5f),
            S(1, "ItemScrewdriver", "ItemPlasticSheets", 1, 2f),
            S(2, "ItemWeldingTorch", "ItemSteelSheets", 1, 1f),
        };
        Assert.Equal(FrameMatchKind.UnexpectedLayout, FrameStageMatcher.Evaluate("StructureFrame", states).Kind);
    }

    [Fact]
    public void Multiple_finishing_stages_skip_the_whole_frame()
    {
        var states = new[]
        {
            S(0, "ItemSteelFrames", null, 0, 0.5f),
            S(1, "ItemScrewdriver", "ItemPlasticSheets", 1, 2f),
            S(2, "ItemScrewdriver", "ItemPlasticSheets", 1, 2f),
        };
        Assert.Equal(FrameMatchKind.UnexpectedLayout, FrameStageMatcher.Evaluate("StructureFrame", states).Kind);
    }

    [Theory]
    [InlineData("ItemScrewdriver", "ItemPlasticSheets")]
    [InlineData("ItemWeldingTorch", "ItemSteelSheets")]
    public void A_matching_stage_at_placement_index_skips_the_whole_frame(string tool, string material)
    {
        var states = new[] { S(0, tool, material, 1, 2f), S(1, "ItemWeldingTorch", "ItemSteelSheets", 1, 1f) };
        Assert.Equal(FrameMatchKind.UnexpectedLayout, FrameStageMatcher.Evaluate("StructureFrame", states).Kind);
    }

    [Fact]
    public void Finishing_moved_to_a_different_final_index_still_matches_with_a_note()
    {
        var states = StructureFrame.Take(2).Concat(new[] { S(2, "ItemScrewdriver", "ItemPlasticSheets", 1, 2f) }).ToArray();
        FrameStageMatch m = FrameStageMatcher.Evaluate("StructureFrame", states);

        Assert.Equal(FrameMatchKind.Matched, m.Kind);
        Assert.Equal("1:Welding,2:Finishing", Layout(m));
        Assert.Contains("expected at index 3", m.Reason);
    }

    [Fact]
    public void Null_or_empty_states_are_handled()
    {
        Assert.Equal(FrameMatchKind.NoMatchingStages, FrameStageMatcher.Evaluate("StructureFrame", null).Kind);
        Assert.Equal(FrameMatchKind.NoMatchingStages, FrameStageMatcher.Evaluate("StructureFrame", new BuildStateDescriptor[0]).Kind);
        Assert.False(FrameStageMatcher.IsFinishingState(null));
        Assert.False(FrameStageMatcher.IsWeldingState(null));
    }
}
