using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace FasterFrameFinishing.Tests;

/// <summary>
/// Checks the files Stationeers uploads to the Steam Workshop (WorkshopMenu.PublishMod):
/// About/About.xml Description (Steam BBCode) and About/Preview.png. Needs only the repository.
/// </summary>
public class WorkshopReadinessTests
{
    private const string GitHubPlaceholder = "YOUR-GITHUB-NAME";

    private static string AboutXml =>
        AppContext.GetData("FasterFrameFinishing.Tests.AboutXml") as string;

    private static XElement About()
    {
        Assert.SkipWhen(AboutXml == null || !File.Exists(AboutXml), "About.xml not found.");
        return XDocument.Load(AboutXml).Root!;
    }

    [Fact]
    public void Workshop_description_fits_steam_limit_and_has_balanced_bbcode()
    {
        string d = About().Element("Description")!.Value;

        Assert.InRange(d.Length, 200, 8000); // Steam Workshop description limit

        var stack = new Stack<string>();
        foreach (Match m in Regex.Matches(d, @"\[(/?)(\w+)(?:=[^\]]*)?\]"))
        {
            string name = m.Groups[2].Value;
            if (m.Groups[1].Value.Length == 0)
            {
                stack.Push(name);
            }
            else
            {
                Assert.True(stack.Count > 0 && stack.Peek() == name, $"Unbalanced BBCode closing tag [/{name}].");
                stack.Pop();
            }
        }

        Assert.Empty(stack);
    }

    [Fact]
    public void Workshop_description_explains_install_and_requirements()
    {
        string d = About().Element("Description")!.Value;

        Assert.Contains("How to install", d);
        Assert.Contains("StationeersLaunchPad", d);
        Assert.Contains("Subscribe", d);
        Assert.Contains("github.com", d);
    }

    [Fact]
    public void In_game_description_uses_only_tags_the_game_ui_renders()
    {
        string g = About().Element("InGameDescription")!.Value;

        Assert.False(string.IsNullOrWhiteSpace(g));
        Assert.DoesNotMatch(@"\[(list|olist|table|\*)\]", g); // BBCode the in-game TextMeshPro text would show raw
    }

    private static (int Width, int Height, long Bytes) Png(string fileName)
    {
        About();
        string path = Path.Combine(Path.GetDirectoryName(AboutXml)!, fileName);
        Assert.True(File.Exists(path), $"About/{fileName} is missing.");
        byte[] bytes = File.ReadAllBytes(path);
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, bytes.Take(4).ToArray());
        int width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
        int height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
        return (width, height, bytes.Length);
    }

    /// <summary>
    /// thumb.png is what WorkshopMenu.PublishMod uploads as the Steam preview. StationeersLaunchPad 1.2.1
    /// (Steam.ValidateForWorkshop) requires it and rejects files over 1024 KiB; when that check fails it
    /// also skips showing InGameDescription, so the raw BBCode appears in the Workshop panel.
    /// </summary>
    [Fact]
    public void Thumb_png_exists_is_16_by_9_and_within_LaunchPad_size_limit()
    {
        var (w, h, size) = Png("thumb.png");
        Assert.True(size <= 1024 * 1024, $"thumb.png is {size / 1024} KiB; LaunchPad and Steam reject more than 1024 KiB.");
        Assert.InRange(w / (double)h, 1.70, 1.80);
    }

    /// <summary>Preview.png fills the in-game Workshop panel's 16:9 image box, which stretches to fit.</summary>
    [Fact]
    public void Preview_png_is_16_by_9_so_it_is_not_stretched_in_game()
    {
        var (w, h, _) = Png("Preview.png");
        Assert.InRange(w / (double)h, 1.70, 1.80);
        Assert.InRange(h, 360, 1080);
    }

    /// <summary>Mirrors the other StationeersLaunchPad 1.2.1 Steam.ValidateForWorkshop limits.</summary>
    [Fact]
    public void About_fields_are_within_LaunchPad_workshop_limits()
    {
        XElement about = About();
        Assert.InRange(about.Element("Name")!.Value.Length, 1, 128);
        Assert.InRange(about.Element("Description")!.Value.Length, 1, 8000);
        Assert.InRange(about.Element("ChangeLog")?.Value.Length ?? 0, 0, 8000);
    }

    /// <summary>Shows up as a skipped test (a reminder), not a failure, until the real URL is filled in.</summary>
    [Fact]
    public void GitHub_link_is_not_a_placeholder()
    {
        string d = About().Element("Description")!.Value;
        Assert.SkipWhen(d.Contains(GitHubPlaceholder),
            $"REMINDER: replace '{GitHubPlaceholder}' in About/About.xml with your GitHub URL before publishing.");
    }
}
