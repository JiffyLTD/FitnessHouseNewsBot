using System.ComponentModel.DataAnnotations;

namespace FitnessHouseNewsBot.Options;

public class VkOptions
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Range(1, long.MaxValue)]
    public long PeerId { get; set; }
}