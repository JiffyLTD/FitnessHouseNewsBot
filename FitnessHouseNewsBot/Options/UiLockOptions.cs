using System.ComponentModel.DataAnnotations;

namespace FitnessHouseNewsBot.Options;

public class UiLockOptions
{
    public const string SectionName = "UiLock";

    [Required]
    public string Password { get; set; } = string.Empty;
}
