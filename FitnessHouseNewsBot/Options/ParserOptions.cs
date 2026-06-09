using System.ComponentModel.DataAnnotations;

namespace FitnessHouseNewsBot.Options;

public class ParserOptions
{
    public const string SectionName = "Parser";

    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int IntervalMinutes { get; set; }
    
    public List<string> ClubKeywords { get; set; } = [];

    public List<string> AlertKeywords { get; set; } = [];
}
