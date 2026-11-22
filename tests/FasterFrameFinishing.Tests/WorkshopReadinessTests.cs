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

    [Fact]
    public void Preview_image_is_a_square_png_under_one_megabyte()
    {
        About();
        string preview = Path.Combine(Path.GetDirectoryName(AboutXml)!, "Preview.png");
        Assert.True(File.Exists(preview), "About/Preview.png is missing (used as the Workshop preview image).");

        byte[] bytes = File.ReadAllBytes(preview);
        Assert.True(bytes.Length < 1024 * 1024, "Steam rejects preview images of 1 MB or more.");
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, bytes.Take(4).ToArray());

        int width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
        int height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
        Assert.Equal(width, height);
        Assert.InRange(width, 256, 1024);
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
