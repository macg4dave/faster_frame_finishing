using System.Linq;
using FasterFrameFinishing.Core;
using Xunit;
using static FasterFrameFinishing.Tests.VerifiedPrefabData;

namespace FasterFrameFinishing.Tests;

public class FinishingStageMatcherTests
{
    [Fact]
    public void Detects_fourth_stage_of_steel_frame()
    {
        FinishingStageMatch m = FinishingStageMatcher.Evaluate("StructureFrame", StructureFrame);

        Assert.Equal(FinishingStageMatchKind.Matched, m.Kind);
        Assert.Equal(3, m.StateIndex);
        Assert.Equal(FinishingStageMatcher.ExpectedStateIndex, m.StateIndex);
    }

    [Fact]
    public void Exactly_one_stage_matches_across_all_verified_frame_variants()
    {
        var matched = AllFrames
            .Select(kv => (kv.Key, Match: FinishingStageMatcher.Evaluate(kv.Key, kv.Value)))
            .Where(x => x.Match.Kind == FinishingStageMatchKind.Matched)
            .ToList();

        Assert.Single(matched);
        Assert.Equal("StructureFrame", matched[0].Key);
    }

    [Theory]
    [InlineData("StructureFrameIron")]
    [InlineData("StructureFrameSide")]
    [InlineData("StructureFrameCorner")]
    [InlineData("StructureFrameCornerCut")]
    public void Other_frame_variants_have_no_finishing_stage(string prefab)
    {
        Assert.Equal(FinishingStageMatchKind.NoFinishingStage, FinishingStageMatcher.Evaluate(prefab, AllFrames[prefab]).Kind);
    }

    [Theory]
    [InlineData("StructurePictureFrameThinPortraitSmall")]
    [InlineData("StructureSuitStorageFrame")]
    [InlineData("StructureCompositeWall")]
    [InlineData(null)]
    public void Non_frame_prefabs_are_ignored_even_with_identical_states(string prefab)
    {
        Assert.Equal(FinishingStageMatchKind.NotAFrame, FinishingStageMatcher.Evaluate(prefab, StructureFrame).Kind);
    }

    [Fact]
    public void Plastic_sheets_with_a_different_tool_do_not_match()
    {
        // Composite walls etc. use ItemWeldingTorch/ItemCrowbar + ItemPlasticSheets.
        var states = new[] { S(0, "ItemSteelFrames", null, 0, 0.5f), S(1, "ItemWeldingTorch", "ItemPlasticSheets", 1, 2f) };
        Assert.Equal(FinishingStageMatchKind.NoFinishingStage, FinishingStageMatcher.Evaluate("StructureFrameX", states).Kind);
    }

    [Fact]
    public void Finishing_stage_that_is_not_final_is_skipped_for_safety()
    {
        var states = StructureFrame.Concat(new[] { S(4, "ItemWrench", null, 0, 1f) }).ToArray();
        FinishingStageMatch m = FinishingStageMatcher.Evaluate("StructureFrame", states);

        Assert.Equal(FinishingStageMatchKind.UnexpectedLayout, m.Kind);
        Assert.Equal(-1, m.StateIndex);
    }

    [Fact]
    public void Multiple_finishing_stages_are_skipped_for_safety()
    {
        var states = new[]
        {
            S(0, "ItemSteelFrames", null, 0, 0.5f),
            S(1, "ItemScrewdriver", "ItemPlasticSheets", 1, 2f),
            S(2, "ItemScrewdriver", "ItemPlasticSheets", 1, 2f),
        };
        Assert.Equal(FinishingStageMatchKind.UnexpectedLayout, FinishingStageMatcher.Evaluate("StructureFrame", states).Kind);
    }

    [Fact]
    public void Finishing_stage_at_placement_index_is_skipped()
    {
        var states = new[] { S(0, "ItemScrewdriver", "ItemPlasticSheets", 1, 2f) };
        Assert.Equal(FinishingStageMatchKind.UnexpectedLayout, FinishingStageMatcher.Evaluate("StructureFrame", states).Kind);
    }

    [Fact]
    public void Stage_moved_to_a_different_final_index_still_matches_with_a_note()
    {
        var states = StructureFrame.Take(2).Concat(new[] { S(2, "ItemScrewdriver", "ItemPlasticSheets", 1, 2f) }).ToArray();
        FinishingStageMatch m = FinishingStageMatcher.Evaluate("StructureFrame", states);

        Assert.Equal(FinishingStageMatchKind.Matched, m.Kind);
        Assert.Equal(2, m.StateIndex);
        Assert.Contains("expected index 3", m.Reason);
    }

    [Fact]
    public void Null_or_empty_states_are_handled()
    {
        Assert.Equal(FinishingStageMatchKind.NoFinishingStage, FinishingStageMatcher.Evaluate("StructureFrame", null).Kind);
        Assert.Equal(FinishingStageMatchKind.NoFinishingStage, FinishingStageMatcher.Evaluate("StructureFrame", new BuildStateDescriptor[0]).Kind);
        Assert.False(FinishingStageMatcher.IsFinishingState(null));
    }
}
