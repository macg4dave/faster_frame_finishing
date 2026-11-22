using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace FasterFrameFinishing.Tests;

/// <summary>Locates the local Stationeers install and opens its assemblies for read-only inspection.</summary>
internal static class GameInstall
{
    public static string Root
    {
        get
        {
            string fromBuild = AppContext.GetData("FasterFrameFinishing.Tests.StationeersDirectory") as string;
            string fromEnv = Environment.GetEnvironmentVariable("STATIONEERS_DIR");
            return new[] { fromBuild, fromEnv }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .FirstOrDefault(p => File.Exists(Path.Combine(p, "rocketstation_Data", "Managed", "Assembly-CSharp.dll")));
        }
    }

    public static string ManagedDir => Path.Combine(Root, "rocketstation_Data", "Managed");

    public static string BepInExCoreDir => Path.Combine(Root, "BepInEx", "core");

    public static string PluginAssembly =>
        AppContext.GetData("FasterFrameFinishing.Tests.PluginAssembly") as string;

    public static void SkipIfMissing()
    {
        Assert.SkipWhen(Root == null,
            "Stationeers install not found (set StationeersDirectory / STATIONEERS_DIR). Game API checks skipped.");
    }

    public static MetadataLoadContext OpenContext(params string[] extraFiles)
    {
        var paths = new List<string>(Directory.GetFiles(ManagedDir, "*.dll"));
        if (Directory.Exists(BepInExCoreDir))
        {
            paths.AddRange(Directory.GetFiles(BepInExCoreDir, "*.dll"));
        }

        paths.AddRange(extraFiles);

        // Unity ships duplicate simple names in some installs; keep the first of each.
        var unique = paths.GroupBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase).Select(g => g.First());
        return new MetadataLoadContext(new PathAssemblyResolver(unique), "mscorlib");
    }
}
