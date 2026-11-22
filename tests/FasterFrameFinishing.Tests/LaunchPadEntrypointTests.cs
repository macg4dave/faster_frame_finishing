using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Xunit;

namespace FasterFrameFinishing.Tests;

/// <summary>
/// Checks the built mod against StationeersLaunchPad's entrypoint rules (Entrypoints/Default.cs and
/// Entrypoints/BepInEx.cs, verified in the installed StationeersLaunchPad 1.2.1):
///  - Default entrypoint: a non-abstract MonoBehaviour with an instance method named "OnLoaded" whose
///    parameters are distinct types from { ConfigFile, List&lt;GameObject&gt;, List&lt;Assembly&gt;, ModData }.
///  - Every BaseUnityPlugin is registered as a separate BepInEx entrypoint, so the mod must contain none
///    (otherwise it would be initialised twice).
/// </summary>
public class LaunchPadEntrypointTests
{
    private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    private static readonly HashSet<string> AllowedOnLoadedParameterTypes = new()
    {
        "BepInEx.Configuration.ConfigFile",
        "System.Collections.Generic.List`1[UnityEngine.GameObject]",
        "System.Collections.Generic.List`1[System.Reflection.Assembly]",
        "StationeersMods.Shared.ModData",
    };

    private static string AboutXmlPath =>
        AppContext.GetData("FasterFrameFinishing.Tests.AboutXml") as string;

    private static Assembly LoadPlugin(MetadataLoadContext ctx, string plugin) => ctx.LoadFromAssemblyPath(plugin);

    private static string RequirePlugin()
    {
        GameInstall.SkipIfMissing();
        string plugin = GameInstall.PluginAssembly;
        Assert.SkipWhen(plugin == null || !File.Exists(plugin), $"Mod not built yet ({plugin}).");
        return plugin;
    }

    private static bool Derives(Type t, string baseFullName)
    {
        for (Type b = t.BaseType; b != null; b = b.BaseType)
        {
            if (b.FullName == baseFullName)
            {
                return true;
            }
        }

        return false;
    }

    [Fact]
    public void Mod_exposes_exactly_one_LaunchPad_default_entrypoint_taking_ConfigFile()
    {
        string plugin = RequirePlugin();
        using var ctx = GameInstall.OpenContext(plugin);
        Assembly asm = LoadPlugin(ctx, plugin);

        var entries = asm.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && Derives(t, "UnityEngine.MonoBehaviour"))
            .Select(t => (Type: t, Method: t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == "OnLoaded" && m.GetParameters().Length > 0)))
            .Where(x => x.Method != null)
            .ToList();

        var entry = Assert.Single(entries);
        Assert.Equal("FasterFrameFinishing.FasterFrameFinishingMod", entry.Type.FullName);

        string[] ps = entry.Method.GetParameters().Select(p => p.ParameterType.ToString()).ToArray();
        Assert.All(ps, p => Assert.Contains(p, AllowedOnLoadedParameterTypes));
        Assert.Equal(ps.Length, ps.Distinct().Count());
        Assert.Contains("BepInEx.Configuration.ConfigFile", ps);
    }

    [Fact]
    public void Mod_has_no_BepInEx_plugin_and_no_unstable_or_unneeded_references()
    {
        string plugin = RequirePlugin();
        using var ctx = GameInstall.OpenContext(plugin);
        Assembly asm = LoadPlugin(ctx, plugin);

        Assert.DoesNotContain(asm.GetTypes(), t => Derives(t, "BepInEx.BaseUnityPlugin"));

        string[] refs = asm.GetReferencedAssemblies().Select(r => r.Name).ToArray();
        Assert.DoesNotContain("StationeersLaunchPad", refs); // not a stable API per its README
        Assert.DoesNotContain("0Harmony", refs);
        Assert.Superset(new HashSet<string> { "Assembly-CSharp", "BepInEx", "UnityEngine.CoreModule" }, refs.ToHashSet());
    }

    [Fact]
    public void Installed_LaunchPad_still_uses_the_OnLoaded_default_entrypoint_convention()
    {
        GameInstall.SkipIfMissing();
        string slp = Path.Combine(GameInstall.Root, "BepInEx", "plugins", "StationeersLaunchPad.dll");
        Assert.SkipWhen(!File.Exists(slp), "StationeersLaunchPad is not installed in BepInEx/plugins.");

        string pluginsDir = Path.GetDirectoryName(slp)!;
        using var ctx = GameInstall.OpenContext(Directory.GetFiles(pluginsDir, "*.dll"));
        Assembly launchPad = ctx.LoadFromAssemblyPath(slp);

        Type defaultEntry = launchPad.GetType("StationeersLaunchPad.Entrypoints.DefaultEntrypoint");
        Assert.True(defaultEntry != null, "StationeersLaunchPad no longer has DefaultEntrypoint.");
        Assert.Equal("OnLoaded", defaultEntry.GetField("DEFAULT_METHOD_NAME", Any)?.GetRawConstantValue());
        Assert.NotNull(defaultEntry.GetNestedType("ConfigParam", Any));
    }

    [Fact]
    public void About_xml_matches_code_constants_and_declares_client_side()
    {
        string plugin = RequirePlugin();
        Assert.SkipWhen(AboutXmlPath == null || !File.Exists(AboutXmlPath), "About.xml not found.");

        XElement about = XDocument.Load(AboutXmlPath).Root!;
        Assert.Equal("ModMetadata", about.Name.LocalName);

        using var ctx = GameInstall.OpenContext(plugin);
        Type info = LoadPlugin(ctx, plugin).GetType("FasterFrameFinishing.PluginInfo")!;

        // LaunchPad names the injected config file after ModID.
        Assert.Equal(info.GetField("ModId", Any)!.GetRawConstantValue(), about.Element("ModID")?.Value);
        Assert.Equal(info.GetField("Name", Any)!.GetRawConstantValue(), about.Element("Name")?.Value);
        Assert.Equal(info.GetField("Version", Any)!.GetRawConstantValue(), about.Element("Version")?.Value);
        Assert.Equal("Client", about.Element("ModSide")?.Value);
    }
}
