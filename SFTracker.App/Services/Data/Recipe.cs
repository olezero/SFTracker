using System.Text.Json.Serialization;

namespace SFTracker.Services.Data;

public class Recipe {
	[JsonPropertyName("Name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("Machine")]
	public string Machine { get; set; } = string.Empty;

	[JsonPropertyName("BatchTime")]
	public string BatchTime { get; set; } = string.Empty;

	[JsonPropertyName("Tier")]
	public string? Tier { get; set; }

	[JsonPropertyName("Alternate")]
	public bool? Alternate { get; set; }

	[JsonPropertyName("MinPower")]
	public string? MinPower { get; set; }

	[JsonPropertyName("AveragePower")]
	public string? AveragePower { get; set; }

	[JsonPropertyName("Ficsmas")]
	public bool? Ficsmas { get; set; }

	[JsonPropertyName("Parts")]
	public List<RecipePart> Parts { get; set; } = new();
}
