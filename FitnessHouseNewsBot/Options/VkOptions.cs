using System.ComponentModel.DataAnnotations;

namespace FitnessHouseNewsBot.Options;

public class VkOptions
{
    public const string SectionName = "Vk";

    [Required]
    public string Token { get; set; } = string.Empty;

    [Range(1, long.MaxValue)]
    public long PeerId { get; set; }
}
