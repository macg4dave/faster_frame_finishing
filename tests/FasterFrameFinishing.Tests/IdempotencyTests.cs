using System.Linq;
using FasterFrameFinishing.Core;
using Xunit;

namespace FasterFrameFinishing.Tests;

/// <summary>
/// Simulates the patcher's apply/restore cycle on a stand-in for ToolUse
/// (a mutable reference type with an EntryTime field), exactly as FramePrefabPatcher does.
/// </summary>
public class IdempotencyTests
{
    private sealed class FakeToolUse
    {
        public float EntryTime;
    }

    private static readonly FinishingSettings Five = new(true, 5f, false, false);

    private static void Apply(OriginalValueStore<FakeToolUse> store, FakeToolUse tool, FinishingSettings s) =>
        tool.EntryTime = FinishingApplier.NextValue(store, tool, tool.EntryTime, s, out _);

    private static void Restore(OriginalValueStore<FakeToolUse> store)
    {
        foreach (var e in store.Entries.ToList())
        {
            e.Key.EntryTime = e.Value;
        }

        store.Clear();
    }

    [Fact]
    public void Repeated_application_does_not_compound()
    {
        var store = new OriginalValueStore<FakeToolUse>();
        var tool = new FakeToolUse { EntryTime = 2f };

        for (int i = 0; i < 10; i++)
        {
            Apply(store, tool, Five);
        }

        Assert.Equal(0.4f, tool.EntryTime, 1e-5f);
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void Vanilla_value_is_preserved_and_reported()
    {
        var store = new OriginalValueStore<FakeToolUse>();
        var tool = new FakeToolUse { EntryTime = 2f };

        Apply(store, tool, Five);
        FinishingApplier.NextValue(store, tool, tool.EntryTime, Five, out float vanilla);

        Assert.Equal(2f, vanilla);
        Assert.True(store.TryGetOriginal(tool, out float original));
        Assert.Equal(2f, original);
    }

    [Fact]
    public void Restore_returns_exact_vanilla_value()
    {
        var store = new OriginalValueStore<FakeToolUse>();
        var tool = new FakeToolUse { EntryTime = 2f };

        Apply(store, tool, Five);
        Apply(store, tool, Five);
        Restore(store);

        Assert.Equal(2f, tool.EntryTime);
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public void Fresh_prefab_copies_are_tracked_independently()
    {
        // If the game ever re-instantiated prefabs, new ToolUse objects arrive with vanilla values.
        var store = new OriginalValueStore<FakeToolUse>();
        var first = new FakeToolUse { EntryTime = 2f };
        var second = new FakeToolUse { EntryTime = 2f };

        Apply(store, first, Five);
        Apply(store, second, Five);
        Apply(store, first, Five);

        Assert.Equal(0.4f, first.EntryTime, 1e-5f);
        Assert.Equal(0.4f, second.EntryTime, 1e-5f);
        Assert.Equal(2, store.Count);
    }

    [Fact]
    public void Keys_use_reference_identity()
    {
        var store = new OriginalValueStore<string>();
        string a = new string('x', 3);
        string b = new string('x', 3); // equal by value, different object

        store.GetOrRecordOriginal(a, 1f);
        store.GetOrRecordOriginal(b, 2f);

        Assert.Equal(2, store.Count);
    }

    [Fact]
    public void Disabled_settings_leave_value_unchanged()
    {
        var store = new OriginalValueStore<FakeToolUse>();
        var tool = new FakeToolUse { EntryTime = 2f };

        Apply(store, tool, FinishingSettings.Vanilla);

        Assert.Equal(2f, tool.EntryTime);
    }
}
