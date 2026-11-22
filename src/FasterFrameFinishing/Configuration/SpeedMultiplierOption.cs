using System.ComponentModel.DataAnnotations;

namespace FasterFrameFinishing.Configuration
{
    /// <summary>
    /// Choices for the finishing speed. StationeersLaunchPad draws enum settings as a drop-down and
    /// labels each entry with <see cref="DisplayAttribute.Name"/> (SLP 1.2.1 EnumInfo), so players see
    /// "2x", "3x", ... The numeric value is the multiplier, so an old config line such as
    /// "SpeedMultiplier = 5" still parses (Enum.Parse accepts numbers) and maps to <see cref="x5"/>.
    /// </summary>
    public enum SpeedMultiplierOption
    {
        [Display(Name = "2x", ShortName = "2x")]
        x2 = 2,

        [Display(Name = "3x", ShortName = "3x")]
        x3 = 3,

        [Display(Name = "4x", ShortName = "4x")]
        x4 = 4,

        [Display(Name = "5x", ShortName = "5x")]
        x5 = 5,

        [Display(Name = "10x", ShortName = "10x")]
        x10 = 10,
    }
}
