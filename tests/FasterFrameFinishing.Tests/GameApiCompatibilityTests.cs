using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace FasterFrameFinishing.Tests;

/// <summary>
/// Verifies, against the *installed* Assembly-CSharp.dll, every game member the mod depends on.
/// Run after each game update: a failure here means the mod's assumptions no longer hold
/// (at runtime the mod would log an error and leave the game unmodified).
/// Assemblies are inspected as metadata only and never executed.
/// </summary>
public class GameApiCompatibilityTests
{
    private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    private static Assembly Game(MetadataLoadContext ctx) =>
        ctx.LoadFromAssemblyPath(Path.Combine(GameInstall.ManagedDir, "Assembly-CSharp.dll"));

    private static Type T(Assembly game, string name) =>
        game.GetType(name) ?? throw new Xunit.Sdk.XunitException($"Type {name} not found in installed Assembly-CSharp.");

    private static FieldInfo F(Type t, string name, string expectedType, bool isStatic = false)
    {
        FieldInfo f = t.GetField(name, Any);
        Assert.True(f != null, $"{t.FullName}.{name} is missing.");
        Assert.Equal(expectedType, f.FieldType.ToString());
        Assert.Equal(isStatic, f.IsStatic);
        Assert.True(f.IsPublic, $"{t.FullName}.{name} is no longer public.");
        return f;
    }

    [Fact]
    public void Structure_prefab_and_build_state_members_exist()
    {
        GameInstall.SkipIfMissing();
        using var ctx = GameInstall.OpenContext();
        Assembly game = Game(ctx);

        Type structure = T(game, "Assets.Scripts.Objects.Structure");
        F(structure, "AllStructurePrefabs", "System.Collections.Generic.List`1[Assets.Scripts.Objects.Structure]", isStatic: true);
        F(structure, "BuildStates", "System.Collections.Generic.List`1[Assets.Scripts.Objects.BuildState]");

        Type buildState = T(game, "Assets.Scripts.Objects.BuildState");
        F(buildState, "Tool", "Assets.Scripts.Objects.ToolUse");

        Type toolUse = T(game, "Assets.Scripts.Objects.ToolUse");
        Assert.Equal("Assets.Scripts.Objects.ToolBasic", toolUse.BaseType?.FullName);

        Type toolBasic = T(game, "Assets.Scripts.Objects.ToolBasic");
        F(toolBasic, "EntryTime", "System.Single");
        F(toolBasic, "ToolEntry", "Assets.Scripts.Objects.Item");
        F(toolBasic, "ToolEntry2", "Assets.Scripts.Objects.Item");
        F(toolBasic, "EntryQuantity2", "System.Int32");

        F(T(game, "Assets.Scripts.Objects.Thing"), "PrefabName", "System.String");
    }

    [Fact]
    public void Prefab_lifecycle_members_exist()
    {
        GameInstall.SkipIfMissing();
        using var ctx = GameInstall.OpenContext();
        Type prefab = T(Game(ctx), "Assets.Scripts.Objects.Prefab");

        EventInfo loaded = prefab.GetEvent("OnPrefabsLoaded", Any);
        Assert.NotNull(loaded);
        Assert.Equal("System.Action", loaded.EventHandlerType?.FullName);
        Assert.True(loaded.AddMethod!.IsStatic && loaded.AddMethod.IsPublic);

        PropertyInfo go = prefab.GetProperty("PrefabsGameObject", Any);
        Assert.NotNull(go);
        Assert.True(go.GetMethod!.IsStatic && go.GetMethod.IsPublic);
    }

    [Fact]
    public void Screwdriver_welding_torch_and_plastic_sheet_types_exist_and_are_items()
    {
        GameInstall.SkipIfMissing();
        using var ctx = GameInstall.OpenContext();
        Assembly game = Game(ctx);
        Type item = T(game, "Assets.Scripts.Objects.Item");

        Assert.True(T(game, "Assets.Scripts.Objects.Items.Screwdriver").IsSubclassOf(item));
        Assert.True(T(game, "Assets.Scripts.Objects.Items.PlasticSheet").IsSubclassOf(item));
        Assert.True(T(game, "Assets.Scripts.Objects.Items.WeldingTorch").IsSubclassOf(item));
    }

    /// <summary>
    /// The construction hold duration must still be derived from ToolBasic.EntryTime inside
    /// Structure.AttackWith. If the game moves the timing elsewhere, editing EntryTime would
    /// silently do nothing; this test catches that.
    /// </summary>
    [Fact]
    public void AttackWith_still_reads_EntryTime_and_tool_speed()
    {
        GameInstall.SkipIfMissing();
        using var ctx = GameInstall.OpenContext();
        Assembly game = Game(ctx);

        Type structure = T(game, "Assets.Scripts.Objects.Structure");
        MethodInfo attackWith = structure.GetMethods(Any)
            .Single(m => m.Name == "AttackWith" && m.DeclaringType == structure && m.GetParameters().Length == 2);
        byte[] il = attackWith.GetMethodBody()!.GetILAsByteArray()!;

        int entryTimeToken = T(game, "Assets.Scripts.Objects.ToolBasic").GetField("EntryTime")!.MetadataToken;
        int toolSpeedToken = T(game, "Assets.Scripts.Objects.Items.Tool").GetMethod("getToolSpeed", Any)!.MetadataToken;

        Assert.True(ContainsOperand(il, 0x7B /* ldfld */, entryTimeToken), "Structure.AttackWith no longer reads ToolBasic.EntryTime.");
        Assert.True(ContainsOperand(il, 0x6F /* callvirt */, toolSpeedToken), "Structure.AttackWith no longer calls Tool.getToolSpeed().");
    }

    /// <summary>
    /// Walks the built plugin's MemberRef table and resolves every field/method it references in
    /// Assembly-CSharp against the installed game. This is exactly the set of members whose absence
    /// would throw MissingMemberException at runtime.
    /// </summary>
    [Fact]
    public void Every_game_member_the_plugin_references_exists_in_installed_game()
    {
        GameInstall.SkipIfMissing();
        string plugin = GameInstall.PluginAssembly;
        Assert.SkipWhen(plugin == null || !File.Exists(plugin), $"Plugin not built yet ({plugin}).");

        using var ctx = GameInstall.OpenContext();
        Assembly game = Game(ctx);

        using FileStream fs = File.OpenRead(plugin);
        using var pe = new System.Reflection.PortableExecutable.PEReader(fs);
        System.Reflection.Metadata.MetadataReader md = System.Reflection.Metadata.PEReaderExtensions.GetMetadataReader(pe);

        var checkedMembers = new System.Collections.Generic.List<string>();
        foreach (var handle in md.MemberReferences)
        {
            var mr = md.GetMemberReference(handle);
            if (mr.Parent.Kind != System.Reflection.Metadata.HandleKind.TypeReference)
            {
                continue;
            }

            var tr = md.GetTypeReference((System.Reflection.Metadata.TypeReferenceHandle)mr.Parent);
            if (tr.ResolutionScope.Kind != System.Reflection.Metadata.HandleKind.AssemblyReference)
            {
                continue;
            }

            var asmRef = md.GetAssemblyReference((System.Reflection.Metadata.AssemblyReferenceHandle)tr.ResolutionScope);
            if (md.GetString(asmRef.Name) != "Assembly-CSharp")
            {
                continue;
            }

            string ns = md.GetString(tr.Namespace);
            string typeName = string.IsNullOrEmpty(ns) ? md.GetString(tr.Name) : ns + "." + md.GetString(tr.Name);
            string member = md.GetString(mr.Name);
            Type type = T(game, typeName);

            bool isField = mr.GetKind() == System.Reflection.Metadata.MemberReferenceKind.Field;
            bool exists = isField
                ? type.GetField(member, Any) != null
                : member == ".ctor"
                    ? type.GetConstructors(Any).Length > 0
                    : type.GetMethods(Any).Any(m => m.Name == member);

            Assert.True(exists, $"Plugin references {typeName}::{member} ({(isField ? "field" : "method")}), which is missing from the installed game.");
            checkedMembers.Add($"{typeName}::{member}");
        }

        // Sanity: the adapter's core dependencies must have been part of the scan.
        Assert.Contains("Assets.Scripts.Objects.ToolBasic::EntryTime", checkedMembers);
        Assert.Contains("Assets.Scripts.Objects.Structure::AllStructurePrefabs", checkedMembers);
        Assert.Contains("Assets.Scripts.Objects.Prefab::add_OnPrefabsLoaded", checkedMembers);
    }

    private static bool ContainsOperand(byte[] il, byte opcode, int token)
    {
        byte[] t = BitConverter.GetBytes(token);
        for (int i = 0; i + 4 < il.Length; i++)
        {
            if (il[i] == opcode && il[i + 1] == t[0] && il[i + 2] == t[1] && il[i + 3] == t[2] && il[i + 4] == t[3])
            {
                return true;
            }
        }

        return false;
    }
}
